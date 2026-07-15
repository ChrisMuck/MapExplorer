#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Code-owned decision flow for one primary location archetype. Content controls the concrete
/// action IDs and gates, while the flow preserves the archetype as the semantic owner of its path.
/// </summary>
public interface ILocationArchetypeInteractionFlow
{
    string ArchetypeId { get; }

    IReadOnlyList<string> ResolveActionIds(
        LocationScenarioProfileDefinition profile,
        SpecialLocationState location,
        KnowledgeState knowledge);
}

/// <summary>Registry boundary that selects an interaction flow from the authored primary archetype.</summary>
public sealed class LocationArchetypeInteractionFlowRegistry
{
    private readonly IReadOnlyDictionary<string, ILocationArchetypeInteractionFlow> flows;

    public LocationArchetypeInteractionFlowRegistry(IEnumerable<ILocationArchetypeInteractionFlow> flows)
    {
        if (flows == null) throw new ArgumentNullException(nameof(flows));
        this.flows = flows.ToDictionary(flow => flow.ArchetypeId, StringComparer.Ordinal);
    }

    public ILocationArchetypeInteractionFlow Get(string archetypeId)
    {
        if (string.IsNullOrWhiteSpace(archetypeId)) throw new ArgumentException("Archetype ID must not be empty.", nameof(archetypeId));
        if (flows.TryGetValue(archetypeId, out var flow)) return flow;
        throw new InvalidOperationException($"No interaction flow is registered for archetype '{archetypeId}'.");
    }

    public static LocationArchetypeInteractionFlowRegistry CreateInitialSlice()
    {
        return new LocationArchetypeInteractionFlowRegistry(new ILocationArchetypeInteractionFlow[]
        {
            new RouteObstacleInteractionFlow(),
            new InvestigationSiteInteractionFlow(),
            new TerritorialMarkerInteractionFlow(),
            new ContainmentSiteInteractionFlow(),
            new ContactSiteInteractionFlow(),
            new HazardSiteInteractionFlow(),
            new NaturalPhenomenonInteractionFlow()
        });
    }
}

public abstract class ScenarioProfileArchetypeInteractionFlow : ILocationArchetypeInteractionFlow
{
    public abstract string ArchetypeId { get; }

    public IReadOnlyList<string> ResolveActionIds(
        LocationScenarioProfileDefinition profile,
        SpecialLocationState location,
        KnowledgeState knowledge)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (location == null) throw new ArgumentNullException(nameof(location));
        if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));
        if (!string.Equals(profile.ArchetypeId, ArchetypeId, StringComparison.Ordinal) ||
            !string.Equals(location.ArchetypeId, ArchetypeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Archetype flow '{ArchetypeId}' cannot resolve profile '{profile.Id}' for location '{location.Id}'.");
        }

        var actionIds = new List<string>();
        AddDistinct(actionIds, profile.ActionSet.SharedActionIds);
        AddDistinct(actionIds, profile.ActionSet.InitialAdditionalActionIds.Take(profile.ActionSet.MaximumVisibleAdditionalActions));

        foreach (var rule in profile.ActionSet.ContextActionRules)
        {
            var allRequiredTags = rule.KnownContextTags.Concat(rule.HiddenContextTags).Distinct(StringComparer.Ordinal).ToList();
            if (allRequiredTags.Count == 0 || !allRequiredTags.Any(tag => knowledge.KnowsLocationContextTag(location.Id, tag))) continue;
            AddDistinct(actionIds, rule.ActionIds);
        }

        foreach (var rule in profile.ActionSet.StateActionRules)
        {
            if (rule.Matches(location)) AddDistinct(actionIds, rule.ActionIds);
        }

        return actionIds;
    }

    private static void AddDistinct(ICollection<string> target, IEnumerable<string> actionIds)
    {
        foreach (var actionId in actionIds)
        {
            if (!target.Contains(actionId)) target.Add(actionId);
        }
    }
}

public sealed class RouteObstacleInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "route-obstacle";
}

public sealed class InvestigationSiteInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "investigation-site";
}

public sealed class TerritorialMarkerInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "territorial-marker";
}

public sealed class ContainmentSiteInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "containment-site";
}

public sealed class ContactSiteInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "contact-site";
}

public sealed class HazardSiteInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "hazard-site";
}

public sealed class NaturalPhenomenonInteractionFlow : ScenarioProfileArchetypeInteractionFlow
{
    public override string ArchetypeId => "natural-phenomenon";
}
}
