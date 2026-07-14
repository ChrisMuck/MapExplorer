#nullable enable
using System;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Evaluates a neutral location action through the territorial policy of the faction that the
/// generator attached to that concrete location. The policy never creates a faction link itself.
/// </summary>
public sealed class FactionTerritorialPolicyResolver
{
    public const string LocationActionCompletedTriggerId = "location-action-completed";

    private readonly CrossSystemDataBundle? content;

    public FactionTerritorialPolicyResolver(CrossSystemDataBundle? content)
    {
        this.content = content;
    }

    public int Resolve(GameState game, WorldTriggerState trigger)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        if (content == null || string.IsNullOrWhiteSpace(trigger.SourceLocationId) || trigger.ActionTags.Count == 0) return 0;

        var location = game.World.Locations.FirstOrDefault(item => item.Id == trigger.SourceLocationId);
        if (location == null) return 0;

        var reactions = 0;
        foreach (var relation in location.FactionRelations)
        {
            var faction = game.FindFaction(relation.FactionId);
            if (faction == null) continue;

            var awareness = AwarenessAtLocation(game, faction.Id, location.Id);
            if (!CanObserve(relation, awareness) || HasSpecificReaction(faction, relation, awareness, trigger)) continue;

            var profile = content.FactionProfiles.Find(faction.ReactionProfileId);
            if (profile == null) continue;

            var response = FindResponse(profile, trigger.ActionTags);
            if (response == null || !response.HasEffect) continue;

            var memoryId = $"territorial-policy:{trigger.Id}:{faction.Id}";
            if (faction.HasMemory(memoryId)) continue;

            faction.AddMemory(memoryId);
            faction.Adjust(response.TrustDelta, response.AngerDelta, response.FearDelta);
            game.World.RecordTrace(
                SimulationTraceKind.FactionReaction,
                $"Faction '{faction.Id}' applied territorial policy to trigger '{trigger.TriggerId}'.",
                game.World.Traces
                    .Where(trace => trace.SubjectIds.Contains(trigger.Id, StringComparer.Ordinal))
                    .Select(trace => trace.TraceId)
                    .Take(1),
                new[] { faction.Id, trigger.Id, location.Id });
            for (var i = 0; i < response.AwarenessIncrease; i++)
            {
                game.World.EscalateFactionAwareness(faction.Id, $"location-region:{location.Id}");
            }

            if (response.RevealsFaction && faction.ContactStatus == FactionContactStatus.Unknown)
            {
                faction.SetContactStatus(FactionContactStatus.Rumored);
            }

            if (response.EventTitle != null && response.EventBody != null)
            {
                game.Events.Enqueue(new EventState(
                    game.World.RuntimeIds.Allocate("faction-policy"),
                    EventKind.FactionReaction,
                    response.EventTitle,
                    response.RevealsFaction ? faction.Name : "Unbekannte Beobachter",
                    response.EventBody,
                    new[] { new EventOptionState("acknowledge", "Zur Kenntnis nehmen", "Die Expedition hält die Reaktion fest.", EventOptionEffectKind.Archive) },
                    location.Coord,
                    response.RevealsFaction ? faction.Id : null));
            }

            reactions++;
        }

        return reactions;
    }

    private bool HasSpecificReaction(
        FactionState faction,
        LocationFactionRelationState relation,
        FactionAwarenessLevel awareness,
        WorldTriggerState trigger)
    {
        var profile = content!.FactionProfiles.Find(faction.ReactionProfileId);
        return content.FactionReactionRules.Any(rule =>
            rule.TriggerId == trigger.TriggerId && rule.Matches(faction, profile, relation, awareness, trigger));
    }

    private static FactionTerritorialPolicyResponseDefinition? FindResponse(
        FactionProfileDefinition profile,
        System.Collections.Generic.IReadOnlyList<string> actionTags)
    {
        var hasRestrictedTag = actionTags.Any(tag =>
            profile.TabooActionTags.Contains(tag, StringComparer.Ordinal) ||
            profile.TerritorialPolicy.RestrictedActionTags.Contains(tag, StringComparer.Ordinal));
        if (hasRestrictedTag) return profile.TerritorialPolicy.RestrictedResponse;

        return actionTags.Any(tag => profile.TerritorialPolicy.WarningActionTags.Contains(tag, StringComparer.Ordinal))
            ? profile.TerritorialPolicy.WarningResponse
            : null;
    }

    private static bool CanObserve(LocationFactionRelationState relation, FactionAwarenessLevel awareness)
    {
        return relation.Kind == LocationFactionRelationKind.Watched
            || relation.Kind == LocationFactionRelationKind.Guarded
            || awareness >= FactionAwarenessLevel.Suspicious;
    }

    private static FactionAwarenessLevel AwarenessAtLocation(GameState game, string factionId, string locationId)
    {
        return game.World.FactionAwareness
            .FirstOrDefault(item => item.FactionId == factionId && item.RegionId == $"location-region:{locationId}")?.Level
            ?? FactionAwarenessLevel.Unaware;
    }
}
}
