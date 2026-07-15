#nullable enable
using System;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Turns an authored scenario finding pool into one concrete unsecured field finding. The service
/// never gives Knowledge Points: it only records what the expedition has to bring home.
/// </summary>
public sealed class LocationFindingAcquisitionService
{
    private readonly CrossSystemAuthoringBundle authoring;
    private readonly LocationScenarioActionResolver scenarioResolver;

    public LocationFindingAcquisitionService(CrossSystemAuthoringBundle authoring)
    {
        this.authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
        scenarioResolver = new LocationScenarioActionResolver(authoring);
    }

    public FieldFindingState? TryAcquire(GameState game, SpecialLocationState location)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (location == null) throw new ArgumentNullException(nameof(location));

        var profile = scenarioResolver.FindProfile(location);
        if (profile == null || profile.FindingPoolIds.Count == 0) return null;
        scenarioResolver.ValidateRuntimeState(location);

        var candidates = profile.FindingPoolIds
            .Where(authoring.Findings.ContainsKey)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0) return null;

        var offset = new WorldDeterministicRandomSource(game.World).NextInt(candidates.Count);
        for (var index = 0; index < candidates.Count; index++)
        {
            var finding = TryAcquire(game, location, candidates[(offset + index) % candidates.Count]);
            if (finding != null) return finding;
        }

        return null;
    }

    /// <summary>
    /// Records one explicitly authored finding from a location outcome. The definition controls
    /// repeat policy; this method only creates unsecured field state and never grants points.
    /// </summary>
    public FieldFindingState? TryAcquire(GameState game, SpecialLocationState location, string findingDefinitionId)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (location == null) throw new ArgumentNullException(nameof(location));
        if (string.IsNullOrWhiteSpace(findingDefinitionId)) throw new ArgumentException("Finding definition ID must not be empty.", nameof(findingDefinitionId));
        if (!authoring.Findings.TryGetValue(findingDefinitionId, out var definition)) return null;
        if (!game.World.TryRegisterFinding(definition.Id, definition.RepeatPolicy, location.Id)) return null;

        var finding = new FieldFindingState(
            game.World.RuntimeIds.Allocate("finding"),
            definition.Id,
            location.Id,
            game.World.WorldDay);
        game.Expedition.AddFieldFinding(finding);
        game.World.RecordTrace(
            SimulationTraceKind.KnowledgeObserved,
            $"Field finding '{definition.Id}' acquired at '{location.Id}'.",
            subjectIds: new[] { finding.Id, location.Id });
        return finding;
    }
}
}
