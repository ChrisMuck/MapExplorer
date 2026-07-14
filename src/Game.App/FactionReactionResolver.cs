#nullable enable
using System;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Resolves a neutral world trigger through the concrete generated location context. It never
/// invents a faction link: locations without a generated relation produce no faction reaction.
/// </summary>
public sealed class FactionReactionResolver
{
    private readonly CrossSystemDataBundle? content;

    public FactionReactionResolver(CrossSystemDataBundle? content)
    {
        this.content = content;
    }

    public int Resolve(GameState game, WorldTriggerState trigger)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        if (content == null || string.IsNullOrWhiteSpace(trigger.SourceLocationId)) return 0;

        var location = game.World.Locations.FirstOrDefault(item => item.Id == trigger.SourceLocationId);
        if (location == null) return 0;

        var reactions = 0;
        foreach (var relation in location.FactionRelations)
        {
            var faction = game.FindFaction(relation.FactionId);
            if (faction == null) continue;

            var awareness = game.World.FactionAwareness
                .FirstOrDefault(item => item.FactionId == faction.Id && item.RegionId == $"location-region:{location.Id}")?.Level
                ?? FactionAwarenessLevel.Unaware;
            var profile = content.FactionProfiles.Find(faction.ReactionProfileId);
            var rule = content.FactionReactionRules
                .Where(item => item.TriggerId == trigger.TriggerId && item.Matches(faction, profile, relation, awareness, trigger))
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (rule == null) continue;

            var memoryId = rule.MemoryId ?? $"reaction:{rule.Id}:{trigger.Id}";
            if (faction.HasMemory(memoryId)) continue;

            faction.Adjust(rule.TrustDelta, rule.AngerDelta, rule.FearDelta);
            faction.AddMemory(memoryId);
            game.World.RecordTrace(
                SimulationTraceKind.FactionReaction,
                $"Faction '{faction.Id}' applied reaction rule '{rule.Id}' to trigger '{trigger.TriggerId}'.",
                game.World.Traces
                    .Where(trace => trace.SubjectIds.Contains(trigger.Id, StringComparer.Ordinal))
                    .Select(trace => trace.TraceId)
                    .Take(1),
                new[] { faction.Id, trigger.Id, rule.Id, location.Id });
            if (rule.RevealsFaction && faction.ContactStatus == FactionContactStatus.Unknown)
            {
                faction.SetContactStatus(FactionContactStatus.Rumored);
            }

            if (rule.EventTitle != null && rule.EventBody != null)
            {
                game.Events.Enqueue(new EventState(
                    game.World.RuntimeIds.Allocate("faction-reaction"),
                    EventKind.FactionReaction,
                    rule.EventTitle,
                    rule.RevealsFaction ? faction.Name : "Unbekannte Beobachter",
                    rule.EventBody,
                    new[] { new EventOptionState("acknowledge", "Zur Kenntnis nehmen", "Die Expedition hält die Reaktion fest.", EventOptionEffectKind.Archive) },
                    location.Coord,
                    rule.RevealsFaction ? faction.Id : null));
            }

            reactions++;
        }

        return reactions;
    }
}
}
