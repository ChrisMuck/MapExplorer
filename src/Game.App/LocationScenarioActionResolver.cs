#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Converts static Scenario Profile authoring into an action base for one concrete location.
/// Only KnowledgeState context tags unlock conditional actions; generated WorldState tags are
/// intentionally not consulted here, so this resolver cannot reveal hidden context to the player.
/// </summary>
public sealed class LocationScenarioActionResolver
{
    private readonly CrossSystemAuthoringBundle authoring;
    private readonly LocationArchetypeInteractionFlowRegistry archetypeFlows;

    public LocationScenarioActionResolver(
        CrossSystemAuthoringBundle authoring,
        LocationArchetypeInteractionFlowRegistry? archetypeFlows = null)
    {
        this.authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
        this.archetypeFlows = archetypeFlows ?? LocationArchetypeInteractionFlowRegistry.CreateInitialSlice();
    }

    public IReadOnlyList<string>? ResolveBaseActionIds(SpecialLocationState location, KnowledgeState knowledge)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));
        if (string.IsNullOrWhiteSpace(location.ArchetypeId) || string.IsNullOrWhiteSpace(location.VariantId)) return null;

        var profile = FindProfile(location);
        if (profile == null) return null;
        ValidateRuntimeState(location, profile);

        return archetypeFlows.Get(profile.ArchetypeId).ResolveActionIds(profile, location, knowledge);
    }

    public LocationScenarioProfileDefinition? FindProfile(SpecialLocationState location)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        if (string.IsNullOrWhiteSpace(location.ArchetypeId) || string.IsNullOrWhiteSpace(location.VariantId)) return null;

        return authoring.ScenarioProfiles.Values
            .Where(candidate => candidate.ArchetypeId == location.ArchetypeId && candidate.VariantId == location.VariantId)
            .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>Rejects a generated runtime instance that does not conform to its scenario state schema.</summary>
    public void ValidateRuntimeState(SpecialLocationState location)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        var profile = FindProfile(location);
        if (profile != null) ValidateRuntimeState(location, profile);
    }

    private void ValidateRuntimeState(SpecialLocationState location, LocationScenarioProfileDefinition scenario)
    {
        if (!authoring.StateProfiles.TryGetValue(scenario.StateProfileId, out var stateProfile))
        {
            throw new InvalidOperationException($"Scenario profile '{scenario.Id}' references missing state profile '{scenario.StateProfileId}'.");
        }

        ValidateChannel(location, scenario, stateProfile, LocationStateChannels.Interaction, location.InteractionStateId);
        ValidateChannel(location, scenario, stateProfile, LocationStateChannels.Operational, location.OperationalStateId);
        ValidateChannel(location, scenario, stateProfile, LocationStateChannels.Presence, location.PresenceStateId);
    }

    private static void ValidateChannel(
        SpecialLocationState location,
        LocationScenarioProfileDefinition scenario,
        LocationStateProfileDefinition stateProfile,
        string channelId,
        string actualValue)
    {
        if (!stateProfile.Channels.TryGetValue(channelId, out var channel)) return;
        if (channel.Values.Contains(actualValue, StringComparer.Ordinal)) return;

        throw new InvalidOperationException(
            $"Location '{location.Id}' has {channelId} state '{actualValue}', which is invalid for scenario '{scenario.Id}' and state profile '{stateProfile.Id}'.");
    }

}
}
