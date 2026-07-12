#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>Processes persistent world work after time advances; presentation remains in EventQueueState.</summary>
public sealed class WorldPhaseService
{
    public IReadOnlyList<string> Resolve(GameState game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        var messages = new List<string>();

        foreach (var trigger in game.World.WorldTriggers)
        {
            if (trigger.IsResolved) continue;
            trigger.MarkResolved();
            messages.Add($"World trigger resolved: {trigger.TriggerId}.");
        }

        foreach (var consequence in game.World.ScheduledConsequences)
        {
            if (!consequence.IsDue(game.World.WorldDay)) continue;
            consequence.MarkApplied();
            var location = game.World.Locations.FirstOrDefault(item => item.Id == consequence.SourceLocationId);
            game.Events.Enqueue(new EventState(
                $"world-consequence-{game.Events.Events.Count + 1}",
                EventKind.WorldConsequence,
                "World changed",
                "World",
                "A past expedition action has begun to change the surrounding world.",
                new[] { new EventOptionState("acknowledge", "Record observation", "The expedition records the change.", EventOptionEffectKind.Archive) },
                location?.Coord));
            messages.Add($"Scheduled consequence applied: {consequence.DefinitionId}.");
        }

        return messages;
    }
}
}
