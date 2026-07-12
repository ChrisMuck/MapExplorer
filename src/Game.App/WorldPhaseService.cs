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
    private readonly CrossSystemDataBundle? content;
    private readonly FactionReactionResolver factionReactionResolver;

    public WorldPhaseService(CrossSystemDataBundle? content = null)
    {
        this.content = content;
        factionReactionResolver = new FactionReactionResolver(content);
    }

    public IReadOnlyList<string> Resolve(GameState game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        var messages = new List<string>();

        foreach (var trigger in game.World.WorldTriggers)
        {
            if (trigger.IsResolved) continue;
            var definition = content?.FindTrigger(trigger.TriggerId);
            if (definition?.ConsequenceId != null)
            {
                var consequence = content!.FindConsequence(definition.ConsequenceId);
                if (consequence != null)
                {
                    foreach (var stage in consequence.Stages)
                    {
                        game.World.ScheduleConsequence(new ScheduledConsequenceState(
                            $"scheduled-consequence-{game.World.ScheduledConsequences.Count + 1}",
                            consequence.Id,
                            trigger.SourceLocationId ?? "world",
                            trigger.RaisedWorldDay + stage.DelayDays,
                            new[] { stage.Id }));
                    }
                }
            }
            factionReactionResolver.Resolve(game, trigger);
            trigger.MarkResolved();
            messages.Add($"World trigger resolved: {trigger.TriggerId}.");
        }

        foreach (var consequence in game.World.ScheduledConsequences)
        {
            if (!consequence.IsDue(game.World.WorldDay)) continue;
            consequence.MarkApplied();
            var location = game.World.Locations.FirstOrDefault(item => item.Id == consequence.SourceLocationId);
            var definition = content?.FindConsequence(consequence.DefinitionId);
            var stage = definition != null
                ? consequence.ResolvedEffectIds.Select(definition.FindStage).FirstOrDefault(item => item != null)
                : null;

            if (stage?.EvidenceId != null)
            {
                var text = content?.Evidence.Find(stage.EvidenceId)?.WorldEventText ?? stage.EventBody ?? "A past expedition action has begun to change the surrounding world.";
                game.Knowledge.AddEvidence(new EvidenceState(
                    $"evidence-world-{game.Knowledge.Evidence.Count + 1}",
                    stage.EvidenceId,
                    EvidenceSourceKind.WorldEvent,
                    EvidenceKnowledgeState.Reported,
                    text,
                    subjectLocationId: location?.Id));
            }

            var title = stage?.EventTitle ?? "World changed";
            var body = stage?.EventBody ?? "A past expedition action has begun to change the surrounding world.";
            game.Events.Enqueue(new EventState(
                $"world-consequence-{game.Events.Events.Count + 1}",
                EventKind.WorldConsequence,
                title,
                "World",
                body,
                new[] { new EventOptionState("acknowledge", "Record observation", "The expedition records the change.", EventOptionEffectKind.Archive) },
                location?.Coord));
            messages.Add($"Scheduled consequence applied: {consequence.DefinitionId}.");
        }

        return messages;
    }
}
}
