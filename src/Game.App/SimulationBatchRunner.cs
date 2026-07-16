#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Headless regression runner for deterministic generated worlds and explicit development
/// scenarios. It owns no game rules: every sample is a normal <see cref="SimulationSession"/>.
/// </summary>
public sealed class SimulationBatchRunner
{
    public const int RequiredSoftConnectionCount = 2;
    private static readonly HashSet<string> SafeDecisionTags = new(StringComparer.Ordinal)
    {
        "leave", "withdraw", "avoid", "mark", "ignore", "defer", "return-later"
    };

    public SimulationBatchReport Run(GameDataCatalog catalog, SimulationBatchRequest request)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var issues = new List<SimulationBatchIssue>();
        AuditContent(catalog, issues);
        var generated = new List<SimulationBatchWorldResult>();
        var scenarios = new List<SimulationBatchScenarioResult>();

        for (var offset = 0; offset < request.SeedCount; offset++)
        {
            var seed = checked(request.FirstSeed + (uint)offset);
            try
            {
                var session = SimulationSession.CreateGenerated(catalog, new WorldGenerationRequest
                {
                    Seed = seed,
                    Width = request.Width,
                    Height = request.Height,
                    FactionCount = request.FactionCount
                });
                for (var day = 0; day < request.DaysToAdvance; day++)
                {
                    var dayResult = session.EndDay();
                    if (!dayResult.Success)
                    {
                        issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.WorldAdvanceFailed, seed, null, dayResult.Error ?? "End day failed."));
                        break;
                    }
                }

                var world = session.Game.World;
                var worldResult = new SimulationBatchWorldResult(
                    seed,
                    world.Locations.Count,
                    world.Connections.Count,
                    world.ScheduledConsequences.Count(item => !item.IsCompleted),
                    world.Situations.Count(item => item.Status == WorldSituationStatus.Active || item.Status == WorldSituationStatus.Dormant),
                    world.Traces.Count(item => item.Kind == SimulationTraceKind.FactionObservation),
                    world.Traces.Count(item => item.Kind == SimulationTraceKind.FactionReaction));
                generated.Add(worldResult);
                AuditRuntime(catalog, session, seed, null, issues);
            }
            catch (Exception error)
            {
                issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.GenerationFailure, seed, null, error.Message));
            }
        }

        foreach (var scenario in request.Scenarios)
        {
            try
            {
                var playback = DevelopmentScenarioPlayback.Create(catalog, scenario);
                while (playback.HasNextCommand)
                {
                    var command = scenario.Commands[playback.NextCommandIndex];
                    var location = FindCommandLocation(playback.Session, command);
                    var before = location == null ? null : LocationStateSignature(location);
                    var step = playback.ExecuteNext();
                    if (step.Success && location != null && before != LocationStateSignature(location))
                    {
                        AuditStateChangingFollowUp(catalog, playback.Session, scenario, command, location, issues);
                    }
                }

                var result = playback.CompleteForInspection();
                var options = CountKnownOptions(result.Session);
                scenarios.Add(new SimulationBatchScenarioResult(
                    scenario.Id,
                    scenario.Seed,
                    result.Success,
                    result.Failures.ToList(),
                    result.Session.Game.World.ScheduledConsequences.Count(item => !item.IsCompleted),
                    result.Session.Game.World.Situations.Count(item => item.Status == WorldSituationStatus.Active || item.Status == WorldSituationStatus.Dormant),
                    options.Total,
                    options.Available,
                    result.Session.Game.World.Traces.Count(item => item.Kind == SimulationTraceKind.FactionObservation),
                    result.Session.Game.World.Traces.Count(item => item.Kind == SimulationTraceKind.FactionReaction)));
                if (!result.Success)
                {
                    issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.UnreachableScenarioPath, scenario.Seed, scenario.Id, result.FailureSummary));
                }

                AuditRuntime(catalog, result.Session, scenario.Seed, scenario.Id, issues);
                AuditKnownLocationChoices(catalog, result.Session, scenario.Seed, scenario.Id, issues);
            }
            catch (Exception error)
            {
                issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.ScenarioFailure, scenario.Seed, scenario.Id, error.Message));
            }
        }

        return new SimulationBatchReport(catalog.ContentVersion, request, generated, scenarios, issues);
    }

    private static void AuditContent(GameDataCatalog catalog, ICollection<SimulationBatchIssue> issues)
    {
        try
        {
            CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, catalog.Authoring);
        }
        catch (Exception error)
        {
            issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.InvalidContent, null, null, error.Message));
        }

        foreach (var consequence in catalog.CrossSystem.Consequences.Values)
        {
            foreach (var branch in consequence.Branches)
            {
                var stages = branch.Stages.OrderBy(stage => stage.DelayDays).ThenBy(stage => stage.Id, StringComparer.Ordinal).ToList();
                for (var index = 0; index < stages.Count; index++)
                {
                    if (stages[index].Severity != WorldConsequenceSeverity.Serious) continue;
                    var hasAnswerableWarning = stages.Take(index)
                        .SelectMany(stage => stage.Effects)
                        .Where(effect => effect.Kind == WorldStageEffectKind.CreateSituation && effect.ReferenceId != null)
                        .Select(effect => effect.ReferenceId!)
                        .Any(id => catalog.Authoring.Situations.TryGetValue(id, out var situation) && situation.ResponseActionTags.Count > 0);
                    if (!hasAnswerableWarning)
                    {
                        issues.Add(new SimulationBatchIssue(
                            SimulationBatchIssueKind.MissingWarningPath,
                            null,
                            consequence.Id,
                            $"Serious branch '{branch.Id}' stage '{stages[index].Id}' has no earlier answerable warning situation."));
                    }
                }
            }
        }

        foreach (var profile in catalog.Authoring.ScenarioProfiles.Values)
        {
            var sharedActions = profile.ActionSet.SharedActionIds
                .Where(catalog.Locations.Definitions.Actions.ContainsKey)
                .Select(id => catalog.Locations.Definitions.Actions[id])
                .ToList();
            if (!sharedActions.Any(IsSafeDecision))
            {
                issues.Add(new SimulationBatchIssue(
                    SimulationBatchIssueKind.MissingLeaveOrDeferChoice,
                    null,
                    profile.Id,
                    $"Archetype profile '{profile.ArchetypeId}' has no shared leave, mark, defer or return-later action."));
            }

            var gatedActionIds = profile.ActionSet.InitialAdditionalActionIds
                .Concat(profile.ActionSet.ContextActionRules.SelectMany(rule => rule.ActionIds))
                .Concat(profile.ActionSet.StateActionRules.SelectMany(rule => rule.ActionIds))
                .Distinct(StringComparer.Ordinal);
            var hasSpecialistGate = gatedActionIds
                .Where(catalog.Locations.Definitions.Actions.ContainsKey)
                .Select(id => catalog.Locations.Definitions.Actions[id])
                .Any(action => action.HardRequirements.Any(requirement => requirement.Kind == LocationRequirementKind.RolePresent));
            if (hasSpecialistGate && !sharedActions.Any(IsSafeDecision))
            {
                issues.Add(new SimulationBatchIssue(
                    SimulationBatchIssueKind.SpecialistGateDeadEnd,
                    null,
                    profile.Id,
                    $"Archetype profile '{profile.ArchetypeId}' can expose a specialist gate without a shared safe decision."));
            }
        }
    }

    private static void AuditStateChangingFollowUp(
        GameDataCatalog catalog,
        SimulationSession session,
        DevelopmentScenario scenario,
        DevelopmentScenarioCommand command,
        SpecialLocationState location,
        ICollection<SimulationBatchIssue> issues)
    {
        var interaction = session.GetLocationInteraction(location.Id).Interaction;
        if (interaction == null) return;
        var available = interaction.Options.Where(option => option.IsAvailable).ToList();
        if (available.Count == 0)
        {
            issues.Add(new SimulationBatchIssue(
                SimulationBatchIssueKind.MissingStateChangeFollowUp,
                scenario.Seed,
                scenario.Id,
                $"Action '{command.ActionId}' changed location '{location.Id}' but left no available follow-up option."));
        }
        else if (!available.Any(option => catalog.Locations.Definitions.Actions.TryGetValue(option.Action.Id, out var action) && IsSafeDecision(action)))
        {
            issues.Add(new SimulationBatchIssue(
                SimulationBatchIssueKind.MissingLeaveOrDeferChoice,
                scenario.Seed,
                scenario.Id,
                $"Action '{command.ActionId}' changed location '{location.Id}' but the resulting choice has no available leave, mark, defer or return-later path."));
        }
    }

    private static void AuditKnownLocationChoices(
        GameDataCatalog catalog,
        SimulationSession session,
        uint seed,
        string scenarioId,
        ICollection<SimulationBatchIssue> issues)
    {
        foreach (var location in session.Game.World.Locations)
        {
            var interaction = session.GetLocationInteraction(location.Id).Interaction;
            if (interaction == null || interaction.Options.Count == 0) continue;
            var available = interaction.Options.Where(option => option.IsAvailable).ToList();
            if (available.Count == 0)
            {
                issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.DeadEndKnownLocationOptions, seed, scenarioId, $"All player-visible options at '{location.Id}' are locked."));
            }

            var hasLockedSpecialistChoice = interaction.Options.Any(option =>
                !option.IsAvailable &&
                catalog.Locations.Definitions.Actions.TryGetValue(option.Action.Id, out var action) &&
                action.HardRequirements.Any(requirement => requirement.Kind == LocationRequirementKind.RolePresent));
            var hasSafeChoice = available.Any(option =>
                catalog.Locations.Definitions.Actions.TryGetValue(option.Action.Id, out var action) && IsSafeDecision(action));
            if (hasLockedSpecialistChoice && !hasSafeChoice)
            {
                issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.SpecialistGateDeadEnd, seed, scenarioId, $"Specialist-gated choices at '{location.Id}' have no available safe alternative."));
            }
        }
    }

    private static SpecialLocationState? FindCommandLocation(SimulationSession session, DevelopmentScenarioCommand command) =>
        string.Equals(command.Kind, "resolve-location-action", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(command.Kind, "advance-location-project", StringComparison.OrdinalIgnoreCase)
            ? session.Game.World.Locations.FirstOrDefault(location => location.Id == command.LocationId)
            : null;

    private static string LocationStateSignature(SpecialLocationState location) =>
        $"{location.InteractionStateId}|{location.OperationalStateId}|{location.PresenceStateId}";

    private static bool IsSafeDecision(LocationActionDefinition action) =>
        action.ActionTags.Any(SafeDecisionTags.Contains);

    private static void AuditRuntime(
        GameDataCatalog catalog,
        SimulationSession session,
        uint seed,
        string? scenarioId,
        ICollection<SimulationBatchIssue> issues)
    {
        var world = session.Game.World;
        if (scenarioId == null && world.Connections.Count < RequiredSoftConnectionCount)
        {
            issues.Add(new SimulationBatchIssue(
                SimulationBatchIssueKind.MissingSoftConnections,
                seed,
                null,
                $"Generated world has {world.Connections.Count} soft connection(s); expected at least {RequiredSoftConnectionCount}."));
        }

        var activeProcesses = world.ScheduledConsequences.Count(item => !item.IsCompleted);
        if (activeProcesses > WorldPhaseService.MaximumConcurrentWorldProcesses)
        {
            issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.ExcessiveConcurrentProcesses, seed, scenarioId, $"{activeProcesses} active processes exceed the cap of {WorldPhaseService.MaximumConcurrentWorldProcesses}."));
        }

        foreach (var situation in world.Situations.Where(item => item.Status == WorldSituationStatus.Active || item.Status == WorldSituationStatus.Dormant))
        {
            if (!catalog.Authoring.Situations.TryGetValue(situation.DefinitionId, out var definition) || definition.ResponseActionTags.Count == 0)
            {
                issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.UnanswerableSituation, seed, scenarioId, $"Situation '{situation.DefinitionId}' has no authored response path."));
            }
        }

        var observations = world.Traces.Count(item => item.Kind == SimulationTraceKind.FactionObservation);
        var reactions = world.Traces.Count(item => item.Kind == SimulationTraceKind.FactionReaction);
        var deliveredFactionSituation = world.Situations.Any(item =>
            string.Equals(item.SourceKind, "faction", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(item.DeliveryChannel));
        if (observations > 0 && reactions == 0 && !deliveredFactionSituation)
        {
            issues.Add(new SimulationBatchIssue(SimulationBatchIssueKind.NoOpFactionObservation, seed, scenarioId, "Faction observation occurred but produced no reaction trace; inspect the context and reaction rules."));
        }
    }

    private static (int Total, int Available) CountKnownOptions(SimulationSession session)
    {
        var total = 0;
        var available = 0;
        foreach (var location in session.Game.World.Locations)
        {
            var interaction = session.GetLocationInteraction(location.Id);
            if (!interaction.Success || interaction.Interaction == null) continue;
            total += interaction.Interaction.Options.Count;
            available += interaction.Interaction.Options.Count(option => option.IsAvailable);
        }

        return (total, available);
    }
}

public sealed class SimulationBatchRequest
{
    public SimulationBatchRequest(uint firstSeed, int seedCount, int width = 24, int height = 18, int factionCount = 3, int daysToAdvance = 1, IEnumerable<DevelopmentScenario>? scenarios = null)
    {
        if (firstSeed == 0) throw new ArgumentOutOfRangeException(nameof(firstSeed));
        if (seedCount < 1) throw new ArgumentOutOfRangeException(nameof(seedCount));
        if ((ulong)firstSeed + (ulong)seedCount - 1UL > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(seedCount), "The requested seed range exceeds UInt32.");
        if (width < 3) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 3) throw new ArgumentOutOfRangeException(nameof(height));
        if (factionCount < 0) throw new ArgumentOutOfRangeException(nameof(factionCount));
        if (daysToAdvance < 0) throw new ArgumentOutOfRangeException(nameof(daysToAdvance));
        FirstSeed = firstSeed;
        SeedCount = seedCount;
        Width = width;
        Height = height;
        FactionCount = factionCount;
        DaysToAdvance = daysToAdvance;
        Scenarios = (scenarios ?? Enumerable.Empty<DevelopmentScenario>()).ToList();
    }

    public uint FirstSeed { get; }
    public int SeedCount { get; }
    public int Width { get; }
    public int Height { get; }
    public int FactionCount { get; }
    public int DaysToAdvance { get; }
    public IReadOnlyList<DevelopmentScenario> Scenarios { get; }
}

public enum SimulationBatchIssueKind
{
    InvalidContent,
    GenerationFailure,
    WorldAdvanceFailed,
    MissingSoftConnections,
    ExcessiveConcurrentProcesses,
    UnanswerableSituation,
    MissingWarningPath,
    NoOpFactionObservation,
    DeadEndKnownLocationOptions,
    MissingStateChangeFollowUp,
    MissingLeaveOrDeferChoice,
    SpecialistGateDeadEnd,
    UnreachableScenarioPath,
    ScenarioFailure
}

public sealed class SimulationBatchIssue
{
    public SimulationBatchIssue(SimulationBatchIssueKind kind, uint? seed, string? scenarioId, string message)
    {
        Kind = kind;
        Seed = seed;
        ScenarioId = scenarioId;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public SimulationBatchIssueKind Kind { get; }
    public uint? Seed { get; }
    public string? ScenarioId { get; }
    public string Message { get; }
}

public sealed class SimulationBatchWorldResult
{
    public SimulationBatchWorldResult(uint seed, int locationCount, int softConnectionCount, int activeProcessCount, int openSituationCount, int factionObservationCount, int factionReactionCount)
    {
        Seed = seed;
        LocationCount = locationCount;
        SoftConnectionCount = softConnectionCount;
        ActiveProcessCount = activeProcessCount;
        OpenSituationCount = openSituationCount;
        FactionObservationCount = factionObservationCount;
        FactionReactionCount = factionReactionCount;
    }

    public uint Seed { get; }
    public int LocationCount { get; }
    public int SoftConnectionCount { get; }
    public int ActiveProcessCount { get; }
    public int OpenSituationCount { get; }
    public int FactionObservationCount { get; }
    public int FactionReactionCount { get; }
}

public sealed class SimulationBatchScenarioResult
{
    public SimulationBatchScenarioResult(string scenarioId, uint seed, bool success, IReadOnlyList<string> failures, int activeProcessCount, int openSituationCount, int visibleOptionCount, int availableOptionCount, int factionObservationCount, int factionReactionCount)
    {
        ScenarioId = scenarioId;
        Seed = seed;
        Success = success;
        Failures = failures;
        ActiveProcessCount = activeProcessCount;
        OpenSituationCount = openSituationCount;
        VisibleOptionCount = visibleOptionCount;
        AvailableOptionCount = availableOptionCount;
        FactionObservationCount = factionObservationCount;
        FactionReactionCount = factionReactionCount;
    }

    public string ScenarioId { get; }
    public uint Seed { get; }
    public bool Success { get; }
    public IReadOnlyList<string> Failures { get; }
    public int ActiveProcessCount { get; }
    public int OpenSituationCount { get; }
    public int VisibleOptionCount { get; }
    public int AvailableOptionCount { get; }
    public int FactionObservationCount { get; }
    public int FactionReactionCount { get; }
}

public sealed class SimulationBatchReport
{
    public SimulationBatchReport(int contentVersion, SimulationBatchRequest request, IReadOnlyList<SimulationBatchWorldResult> generatedWorlds, IReadOnlyList<SimulationBatchScenarioResult> scenarios, IReadOnlyList<SimulationBatchIssue> issues)
    {
        ContentVersion = contentVersion;
        FirstSeed = request.FirstSeed;
        SeedCount = request.SeedCount;
        DaysToAdvance = request.DaysToAdvance;
        GeneratedWorlds = generatedWorlds;
        Scenarios = scenarios;
        Issues = issues;
    }

    public int ContentVersion { get; }
    public uint FirstSeed { get; }
    public int SeedCount { get; }
    public int DaysToAdvance { get; }
    public IReadOnlyList<SimulationBatchWorldResult> GeneratedWorlds { get; }
    public IReadOnlyList<SimulationBatchScenarioResult> Scenarios { get; }
    public IReadOnlyList<SimulationBatchIssue> Issues { get; }
    public bool Success => Issues.Count == 0;
    public int TotalGeneratedLocations => GeneratedWorlds.Sum(item => item.LocationCount);
    public int TotalSoftConnections => GeneratedWorlds.Sum(item => item.SoftConnectionCount);
}
}
