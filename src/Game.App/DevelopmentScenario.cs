#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core;
using Newtonsoft.Json;

namespace Game.App
{

/// <summary>Development-only scenario input. It is intentionally separate from authored game content and saves.</summary>
public sealed class DevelopmentScenario
{
    public string Id { get; set; } = string.Empty;
    public uint Seed { get; set; } = 1;
    public string WorldKind { get; set; } = "generated";
    public int Width { get; set; } = 24;
    public int Height { get; set; } = 18;
    public int FactionCount { get; set; } = 3;
    public DevelopmentScenarioMetadata Metadata { get; set; } = new();
    /// <summary>Explicit development fixture; it never participates in normal generation or save data.</summary>
    public DevelopmentScenarioInitialState? InitialState { get; set; }
    public List<DevelopmentScenarioCommand> Commands { get; set; } = new();
    public List<DevelopmentScenarioAssertion> Assertions { get; set; } = new();
}

public sealed class DevelopmentScenarioMetadata
{
    public string Purpose { get; set; } = string.Empty;
    public List<string> SelectedTeamMemberIds { get; set; } = new();
    public List<string> StartingKnowledge { get; set; } = new();
    public string ExpectedWorldProcess { get; set; } = string.Empty;
    public string OutstandingDecisionPoint { get; set; } = string.Empty;
}

public sealed class DevelopmentScenarioCommand
{
    public string Kind { get; set; } = string.Empty;
    public List<string> ScoutMemberIds { get; set; } = new();
    public string? Direction { get; set; }
    public int DurationDays { get; set; } = 1;
    public string? Focus { get; set; }
    public string? Behavior { get; set; }
    public string? LocationId { get; set; }
    public string? ActionId { get; set; }
    public string? OutcomeTier { get; set; }
    public string? SituationDefinitionId { get; set; }
    public string? ResponseActionTag { get; set; }
    public int? DestinationQ { get; set; }
    public int? DestinationR { get; set; }
}

public sealed class DevelopmentScenarioAssertion
{
    public string Kind { get; set; } = string.Empty;
    public string? Argument { get; set; }
    public string? LocationId { get; set; }
    public string? ActionId { get; set; }
    public string? StateChannel { get; set; }
    public string? StateId { get; set; }
    public string? SituationDefinitionId { get; set; }
    public string? DeliveryChannel { get; set; }
    public string? FactionId { get; set; }
    public int? Q { get; set; }
    public int? R { get; set; }
}

public sealed class DevelopmentScenarioInitialState
{
    public int ExpeditionQ { get; set; }
    public int ExpeditionR { get; set; }
    public int Supplies { get; set; } = 12;
    public int Medicine { get; set; } = 3;
    /// <summary>Only these fixture locations begin as player-confirmed; all others must be rediscovered through play.</summary>
    public List<string> ConfirmedLocationIds { get; set; } = new();
    public List<DevelopmentScenarioMember> Members { get; set; } = new();
    public List<DevelopmentScenarioFaction> Factions { get; set; } = new();
    public List<DevelopmentScenarioLocation> Locations { get; set; } = new();
    public List<DevelopmentScenarioWorldTrigger> WorldTriggers { get; set; } = new();
}

public sealed class DevelopmentScenarioWorldTrigger
{
    public string TriggerId { get; set; } = string.Empty;
    public string? SourceLocationId { get; set; }
    public List<string> ActionTags { get; set; } = new();
}

public sealed class DevelopmentScenarioMember
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int StarLevel { get; set; }
}

public sealed class DevelopmentScenarioFaction
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ReactionProfileId { get; set; } = "neutral-cautious";
    public string SignatureProfileId { get; set; } = "unassigned";
}

public sealed class DevelopmentScenarioLocation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Ruin";
    public int Q { get; set; }
    public int R { get; set; }
    public string AnchorKind { get; set; } = "Point";
    public int? AnchorOtherQ { get; set; }
    public int? AnchorOtherR { get; set; }
    public string? ArchetypeId { get; set; }
    public string? VariantId { get; set; }
    /// <summary>Optional shared presentation profile for this development fixture location.</summary>
    public string? ContentProfileId { get; set; }
    public List<string> ModifierIds { get; set; } = new();
    public string InteractionStateId { get; set; } = LocationStateIds.Interaction.Untouched;
    public string OperationalStateId { get; set; } = LocationStateIds.Operational.None;
    public string PresenceStateId { get; set; } = LocationStateIds.Presence.Unknown;
    public List<string> ContextTags { get; set; } = new();
    public List<string> EvidenceSeedIds { get; set; } = new();
    public List<DevelopmentScenarioLocationRelation> FactionRelations { get; set; } = new();
}

public sealed class DevelopmentScenarioLocationRelation
{
    public string FactionId { get; set; } = string.Empty;
    public string Kind { get; set; } = "Claimed";
    public List<string> ContextTags { get; set; } = new();
}

public static class DevelopmentScenarioLoader
{
    public static DevelopmentScenario LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new FileNotFoundException("Development scenario was not found.", path);
        var scenario = JsonConvert.DeserializeObject<DevelopmentScenario>(File.ReadAllText(path)) ?? throw new LocationDataException("Development scenario is empty.");
        if (string.IsNullOrWhiteSpace(scenario.Id)) throw new LocationDataException("Development scenario needs an id.");
        if (scenario.Seed < 1 || scenario.Width < 3 || scenario.Height < 3 || scenario.FactionCount < 0) throw new LocationDataException("Development scenario has invalid world-generation values.");
        ValidateInitialState(scenario);
        return scenario;
    }

    private static void ValidateInitialState(DevelopmentScenario scenario)
    {
        var initial = scenario.InitialState;
        if (initial == null) return;
        if (initial.Supplies < 0 || initial.Medicine < 0) throw new LocationDataException("Development scenario fixture has invalid expedition resources.");
        if (initial.Members.Count == 0) throw new LocationDataException("Development scenario fixture needs expedition members.");
        if (initial.Locations.Count == 0) throw new LocationDataException("Development scenario fixture needs locations.");
        if (string.IsNullOrWhiteSpace(scenario.Metadata.Purpose) || string.IsNullOrWhiteSpace(scenario.Metadata.OutstandingDecisionPoint))
            throw new LocationDataException("Development scenario needs purpose and outstanding decision metadata.");
        var memberIds = initial.Members.Select(member => member.Id).ToHashSet(StringComparer.Ordinal);
        if (scenario.Metadata.SelectedTeamMemberIds.Count == 0 || scenario.Metadata.SelectedTeamMemberIds.Any(id => !memberIds.Contains(id)))
            throw new LocationDataException("Development scenario selected team must reference fixture members.");
    }
}

public sealed class DevelopmentScenarioExecutor
{
    public DevelopmentScenarioRunResult Execute(GameDataCatalog catalog, DevelopmentScenario scenario)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        return DevelopmentScenarioPlayback.Create(catalog, scenario).RunToCompletion();
    }

    internal static bool ExecuteCommand(SimulationSession session, DevelopmentScenarioCommand command, out string error)
    {
        error = string.Empty;
        switch (command.Kind?.Trim().ToLowerInvariant())
        {
            case "end-day":
                var day = session.EndDay();
                error = day.Error ?? string.Empty;
                return day.Success;
            case "send-directional-scout":
                if (!Enum.TryParse<ScoutDirection>(command.Direction, true, out var direction) || !Enum.TryParse<ScoutMissionFocus>(command.Focus, true, out var focus) || !Enum.TryParse<ScoutMissionBehavior>(command.Behavior, true, out var behavior))
                {
                    error = "direction, focus or behavior is invalid.";
                    return false;
                }
                var scout = session.SendDirectionalScout(command.ScoutMemberIds, direction, command.DurationDays, focus, behavior);
                error = scout.Error ?? string.Empty;
                return scout.Success;
            case "scout-location-surroundings":
                var local = session.ScoutLocationSurroundings(command.LocationId ?? string.Empty, command.ScoutMemberIds);
                error = local.Error ?? string.Empty;
                return local.Success;
            case "inspect-location":
                var locationToInspect = session.Game.World.Locations.FirstOrDefault(item => item.Id == command.LocationId);
                if (locationToInspect == null)
                {
                    error = "location was not found.";
                    return false;
                }
                var inspection = session.InspectLocation(locationToInspect.Coord);
                error = inspection.Error ?? string.Empty;
                return inspection.Success;
            case "move-expedition":
                if (!command.DestinationQ.HasValue || !command.DestinationR.HasValue)
                {
                    error = "destination Q and R are required.";
                    session.RecordScenarioCommandRejection("move-expedition", error);
                    return false;
                }
                var movement = session.MoveExpedition(new HexCoord(command.DestinationQ.Value, command.DestinationR.Value));
                error = movement.Error ?? string.Empty;
                return movement.Success;
            case "resolve-location-action":
                LocationOutcomeTier? forcedTier = null;
                if (!string.IsNullOrWhiteSpace(command.OutcomeTier))
                {
                    if (!Enum.TryParse<LocationOutcomeTier>(command.OutcomeTier, true, out var parsedTier))
                    {
                        error = "outcome tier is invalid.";
                        session.RecordScenarioCommandRejection("resolve-location-action", error);
                        return false;
                    }
                    forcedTier = parsedTier;
                }
                var action = !forcedTier.HasValue
                    ? session.ResolveLocationAction(command.LocationId ?? string.Empty, command.ActionId ?? string.Empty)
                    : session.ResolveLocationAction(command.LocationId ?? string.Empty, command.ActionId ?? string.Empty, forcedTier.Value);
                error = action.Error ?? string.Empty;
                return action.Success;
            case "advance-location-project":
                var project = session.AdvanceLocationProject(command.LocationId ?? string.Empty);
                error = project.Error ?? string.Empty;
                return project.Success;
            case "resolve-world-situation":
                var resolved = session.ResolveWorldSituation(command.SituationDefinitionId ?? string.Empty, command.ResponseActionTag ?? string.Empty);
                error = resolved ? string.Empty : "No matching active situation accepted the response.";
                return resolved;
            default:
                error = "unsupported command.";
                session.RecordScenarioCommandRejection(command.Kind ?? string.Empty, error);
                return false;
        }
    }

    internal static bool EvaluateAssertion(SimulationSession session, DevelopmentScenarioAssertion assertion, out string error)
    {
        error = string.Empty;
        var argument = assertion.Argument;
        switch (assertion.Kind?.Trim().ToLowerInvariant())
        {
            case "option-visible":
            case "option-available":
            case "option-locked":
                var interaction = session.GetLocationInteraction(assertion.LocationId ?? string.Empty);
                var option = interaction.Interaction?.FindOption(assertion.ActionId ?? string.Empty);
                if (option == null)
                {
                    error = $"option '{assertion.ActionId}' is not player-visible at location '{assertion.LocationId}'.";
                    return false;
                }
                if (string.Equals(assertion.Kind, "option-visible", StringComparison.OrdinalIgnoreCase)) return true;
                var expectedAvailable = string.Equals(assertion.Kind, "option-available", StringComparison.OrdinalIgnoreCase);
                if (option.IsAvailable == expectedAvailable) return true;
                error = expectedAvailable
                    ? $"option '{assertion.ActionId}' is locked: {option.LockedReason}"
                    : $"option '{assertion.ActionId}' is available but was expected to be locked.";
                return false;
            case "has-location-state":
                var location = session.Game.World.Locations.FirstOrDefault(item => item.Id == assertion.LocationId);
                if (location == null)
                {
                    error = $"location '{assertion.LocationId}' was not found.";
                    return false;
                }
                var actualState = assertion.StateChannel?.Trim().ToLowerInvariant() switch
                {
                    "interaction" => location.InteractionStateId,
                    "operational" => location.OperationalStateId,
                    "presence" => location.PresenceStateId,
                    _ => string.Empty
                };
                if (actualState == assertion.StateId) return true;
                error = $"location '{location.Id}' has {assertion.StateChannel} state '{actualState}', expected '{assertion.StateId}'.";
                return false;
            case "has-known-location-state":
                var known = session.Game.Knowledge.KnownLocationConditions.FirstOrDefault(item => item.LocationId == assertion.LocationId);
                var knownState = assertion.StateChannel?.Trim().ToLowerInvariant() switch
                {
                    "interaction" => known?.InteractionStateId,
                    "operational" => known?.OperationalStateId,
                    "presence" => known?.PresenceStateId,
                    _ => null
                };
                if (knownState == assertion.StateId) return true;
                error = $"player knowledge has {assertion.StateChannel} state '{knownState ?? "none"}' for '{assertion.LocationId}', expected '{assertion.StateId}'.";
                return false;
            case "has-situation-delivery":
                if (session.Game.World.Situations.Any(item =>
                    item.DefinitionId == assertion.SituationDefinitionId &&
                    string.Equals(item.DeliveryChannel, assertion.DeliveryChannel, StringComparison.OrdinalIgnoreCase))) return true;
                error = $"no situation '{assertion.SituationDefinitionId}' used delivery channel '{assertion.DeliveryChannel}'.";
                return false;
            case "has-situation-status":
                if (Enum.TryParse<WorldSituationStatus>(assertion.StateId, true, out var expectedStatus) &&
                    session.Game.World.Situations.Any(item => item.DefinitionId == assertion.SituationDefinitionId && item.Status == expectedStatus)) return true;
                error = $"no situation '{assertion.SituationDefinitionId}' has status '{assertion.StateId}'.";
                return false;
            case "has-tile-blocked-state":
                if (!assertion.Q.HasValue || !assertion.R.HasValue || !bool.TryParse(argument, out var expectedBlocked))
                {
                    error = "tile blocked assertion needs q, r and a boolean argument.";
                    return false;
                }
                var tile = session.Game.World.Map.GetTile(new HexCoord(assertion.Q.Value, assertion.R.Value));
                if (tile != null && tile.IsBlocked == expectedBlocked) return true;
                error = $"tile ({assertion.Q},{assertion.R}) blocked state did not equal '{expectedBlocked}'.";
                return false;
            case "has-faction-memory":
                if (!string.IsNullOrWhiteSpace(argument) && session.Game.FindFaction(assertion.FactionId ?? string.Empty)?.HasMemory(argument) == true) return true;
                error = $"faction '{assertion.FactionId}' does not remember '{argument}'.";
                return false;
            case "has-scout-mission-status":
                if (Enum.TryParse<ScoutMissionStatus>(assertion.StateId, true, out var missionStatus) &&
                    session.Game.Expedition.ScoutMissions.Any(item => item.Status == missionStatus)) return true;
                error = $"no scout mission has status '{assertion.StateId}'.";
                return false;
            case "has-member-status":
                if (Enum.TryParse<ExpeditionMemberStatus>(assertion.StateId, true, out var memberStatus) &&
                    session.Game.Expedition.FindMember(assertion.Argument ?? string.Empty)?.Status == memberStatus) return true;
                error = $"member '{assertion.Argument}' does not have status '{assertion.StateId}'.";
                return false;
            case "has-report-reliability-at-most":
                if (int.TryParse(argument, out var maximumReliability) &&
                    session.Game.Knowledge.ScoutReports.Any(report => report.Reliability <= maximumReliability)) return true;
                error = $"no scout report has reliability at most '{argument}'.";
                return false;
            case "has-soft-connections":
                if (session.Game.World.Connections.Count >= 2) return true;
                error = "generated world has fewer than two soft connections.";
                return false;
            case "has-directional-lead":
                if (session.Game.Knowledge.ScoutReports.SelectMany(report => report.Leads).Any(lead => lead.Scope == ScoutLeadScope.Directional)) return true;
                error = "no directional lead was recorded.";
                return false;
            case "no-exact-scout-coordinates":
                if (session.Game.Knowledge.ScoutReports.All(report => !report.HasExactCoordinates)) return true;
                error = "a scout report exposed exact coordinates.";
                return false;
            case "has-causality-trace":
                if (session.Game.World.Traces.Count > 0) return true;
                error = "session recorded no simulation trace.";
                return false;
            case "has-evidence":
                if (!string.IsNullOrWhiteSpace(argument) && session.Game.Knowledge.Evidence.Any(item => item.DefinitionId == argument.Trim())) return true;
                error = $"player knowledge does not contain evidence '{argument}'.";
                return false;
            case "has-no-evidence":
                if (session.Game.Knowledge.Evidence.Count == 0) return true;
                error = "player knowledge unexpectedly contains evidence.";
                return false;
            case "has-world-trigger":
                if (!string.IsNullOrWhiteSpace(argument) && session.Game.World.WorldTriggers.Any(item => item.TriggerId == argument.Trim())) return true;
                error = $"world truth does not contain trigger '{argument}'.";
                return false;
            case "no-world-trigger":
                if (!string.IsNullOrWhiteSpace(argument) && session.Game.World.WorldTriggers.All(item => item.TriggerId != argument.Trim())) return true;
                error = $"world truth contains forbidden trigger '{argument}'.";
                return false;
            case "has-resolved-situation-response":
                if (!string.IsNullOrWhiteSpace(argument) && session.Game.World.Situations.Any(item => item.Status == WorldSituationStatus.Resolved && item.ResolutionActionTag == argument.Trim())) return true;
                error = $"no world situation was resolved with '{argument}'.";
                return false;
            case "has-trace-kind":
                if (!string.IsNullOrWhiteSpace(argument) && session.Game.World.Traces.Any(item => string.Equals(item.Kind.ToString(), argument.Trim(), StringComparison.OrdinalIgnoreCase))) return true;
                error = $"causality trace does not contain kind '{argument}'.";
                return false;
            default:
                error = "unsupported assertion.";
                return false;
        }
    }

    internal static SimulationSession CreateSession(GameDataCatalog catalog, DevelopmentScenario scenario)
    {
        if (scenario.InitialState != null)
        {
            return SimulationSession.CreateFixture(catalog, CreateFixtureGame(scenario, scenario.InitialState), scenario.Seed);
        }

        return string.Equals(scenario.WorldKind, "tutorial", StringComparison.OrdinalIgnoreCase)
            ? SimulationSession.CreateTutorial(catalog, scenario.Seed)
            : SimulationSession.CreateGenerated(catalog, new WorldGenerationRequest { Seed = scenario.Seed, Width = scenario.Width, Height = scenario.Height, FactionCount = scenario.FactionCount });
    }

    private static GameState CreateFixtureGame(DevelopmentScenario scenario, DevelopmentScenarioInitialState initial)
    {
        var bounds = new HexMapBounds(scenario.Width, scenario.Height);
        HexCoord Coord(int q, int r, string label)
        {
            var coord = new HexCoord(q, r);
            if (!bounds.Contains(coord)) throw new LocationDataException($"Development scenario fixture {label} lies outside the map.");
            return coord;
        }

        var factions = initial.Factions.Select(item => new FactionState(item.Id, item.Name, reactionProfileId: item.ReactionProfileId, signatureProfileId: item.SignatureProfileId)).ToList();
        var factionIds = factions.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var locations = initial.Locations.Select(item => CreateFixtureLocation(item, factionIds, Coord)).ToList();
        var members = initial.Members.Select(item =>
        {
            if (!Enum.TryParse<ExpeditionMemberRole>(item.Role, true, out var role)) throw new LocationDataException($"Development scenario member '{item.Id}' has an invalid role.");
            return new ExpeditionMemberState(item.Id, item.Name, role, starLevel: item.StarLevel);
        }).ToList();
        var expeditionCoord = Coord(initial.ExpeditionQ, initial.ExpeditionR, "expedition");
        var knowledge = new KnowledgeState();
        knowledge.SetTileKnowledge(expeditionCoord, KnowledgeLevel.Confirmed);
        var confirmedLocationIds = new HashSet<string>(initial.ConfirmedLocationIds ?? new List<string>(), StringComparer.Ordinal);
        foreach (var location in locations.Where(location => confirmedLocationIds.Contains(location.Id)))
        {
            knowledge.SetTileKnowledge(location.Coord, KnowledgeLevel.Confirmed);
        }

        var game = new GameState(
            new WorldState(HexMapState.CreateFilled(bounds, TerrainType.Grassland), locations: locations, random: new DeterministicRandomState(scenario.Seed)),
            knowledge,
            new PlayerNotesState(),
            new ExpeditionState(1, expeditionCoord, members, supplies: initial.Supplies, medicine: initial.Medicine),
            new BaseState(expeditionCoord),
            factions: factions);
        foreach (var trigger in initial.WorldTriggers)
        {
            var sourceLocation = string.IsNullOrWhiteSpace(trigger.SourceLocationId)
                ? null
                : locations.FirstOrDefault(location => location.Id == trigger.SourceLocationId)
                    ?? throw new LocationDataException($"Development scenario trigger references unknown location '{trigger.SourceLocationId}'.");
            game.World.QueueWorldTrigger(new WorldTriggerState(
                game.World.RuntimeIds.Allocate("world-trigger"), trigger.TriggerId, game.World.WorldDay,
                trigger.ActionTags, sourceLocation?.Id, sourceLocation?.Coord));
        }
        return game;
    }

    private static SpecialLocationState CreateFixtureLocation(DevelopmentScenarioLocation item, ISet<string> factionIds, Func<int, int, string, HexCoord> coord)
    {
        if (!Enum.TryParse<LocationKind>(item.Kind, true, out var kind)) throw new LocationDataException($"Development scenario location '{item.Id}' has an invalid kind.");
        var position = coord(item.Q, item.R, $"location '{item.Id}'");
        if (!Enum.TryParse<LocationAnchorKind>(item.AnchorKind, true, out var anchorKind)) throw new LocationDataException($"Development scenario location '{item.Id}' has an invalid anchor kind.");
        var anchor = anchorKind switch
        {
            LocationAnchorKind.Point => LocationAnchor.Point(position),
            LocationAnchorKind.Edge => LocationAnchor.Edge(coord(item.AnchorOtherQ ?? item.Q - 1, item.AnchorOtherR ?? item.R, $"edge anchor for '{item.Id}'"), position),
            LocationAnchorKind.Area => LocationAnchor.Area(new[] { position }),
            _ => throw new LocationDataException($"Development scenario location '{item.Id}' has an unsupported anchor kind.")
        };
        var relations = item.FactionRelations.Select(relation =>
        {
            if (!factionIds.Contains(relation.FactionId)) throw new LocationDataException($"Development scenario relation on '{item.Id}' references an unknown faction.");
            if (!Enum.TryParse<LocationFactionRelationKind>(relation.Kind, true, out var relationKind)) throw new LocationDataException($"Development scenario relation on '{item.Id}' has an invalid kind.");
            return new LocationFactionRelationState(relation.FactionId, relationKind, relation.ContextTags);
        });
        return new SpecialLocationState(item.Id, kind, position, item.Name, anchor, item.ArchetypeId, item.VariantId,
            item.ModifierIds, contentProfileId: item.ContentProfileId, interactionStateId: item.InteractionStateId,
            operationalStateId: item.OperationalStateId, presenceStateId: item.PresenceStateId, factionRelations: relations,
            contextTags: item.ContextTags, evidenceSeedIds: item.EvidenceSeedIds);
    }
}

public sealed class DevelopmentScenarioRunResult
{
    public DevelopmentScenarioRunResult(DevelopmentScenario scenario, SimulationSession session, IEnumerable<string> failures)
    {
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        ScenarioId = scenario.Id;
        Session = session;
        Failures = (failures ?? throw new ArgumentNullException(nameof(failures))).ToList();
    }

    public DevelopmentScenario Scenario { get; }
    public string ScenarioId { get; }
    public SimulationSession Session { get; }
    public IReadOnlyList<string> Failures { get; }
    public bool Success => Failures.Count == 0;
    public string FailureSummary => $"Scenario '{ScenarioId}' failed (seed {Session.Seed}, content {Session.ContentVersion}) at {string.Join("; ", Failures)}. Trace: {string.Join(" -> ", Session.Game.World.Traces.Select(trace => trace.TraceId))}";
    public DevelopmentScenarioRunRecord CreateRunRecord() => new(Scenario, Session.CreateRunRecord());
}

/// <summary>
/// Serializable development artifact containing both the immutable scenario input and the observed
/// run result. It is intentionally not part of campaign save data.
/// </summary>
public sealed class DevelopmentScenarioRunRecord
{
    public DevelopmentScenarioRunRecord(DevelopmentScenario scenario, SimulationRunRecord simulation)
    {
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
    }

    public DevelopmentScenario Scenario { get; set; }
    public SimulationRunRecord Simulation { get; set; }

    public DevelopmentScenarioRunResult Replay(GameDataCatalog catalog)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (catalog.ContentVersion != Simulation.ContentVersion)
        {
            throw new InvalidOperationException($"Run record expects content version {Simulation.ContentVersion}, but catalog provides {catalog.ContentVersion}.");
        }

        return new DevelopmentScenarioExecutor().Execute(catalog, Scenario);
    }
}
}
