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
    private readonly CrossSystemAuthoringBundle? authoring;
    private readonly FactionReactionResolver factionReactionResolver;
    private readonly FactionTerritorialPolicyResolver territorialPolicyResolver;

    public WorldPhaseService(CrossSystemDataBundle? content = null, CrossSystemAuthoringBundle? authoring = null)
    {
        this.content = content;
        this.authoring = authoring;
        factionReactionResolver = new FactionReactionResolver(content);
        territorialPolicyResolver = new FactionTerritorialPolicyResolver(content);
    }

    public IReadOnlyList<string> Resolve(GameState game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        var messages = new List<string>();

        foreach (var trigger in game.World.WorldTriggers)
        {
            if (trigger.IsResolved) continue;
            var triggerTrace = game.World.RecordTrace(
                SimulationTraceKind.WorldTrigger,
                $"World trigger '{trigger.TriggerId}' entered the world phase.",
                trigger.CausedByTraceId == null ? null : new[] { trigger.CausedByTraceId },
                trigger.SourceLocationId == null ? null : new[] { trigger.SourceLocationId, trigger.Id });
            var definition = content?.FindTrigger(trigger.TriggerId);
            if (definition?.ConsequenceId != null)
            {
                var consequence = content!.FindConsequence(definition.ConsequenceId);
                if (consequence != null)
                {
                    var branch = consequence.SelectBranch(new WorldDeterministicRandomSource(game.World));
                    var stages = branch.Stages
                        .OrderBy(stage => stage.DelayDays)
                        .ThenBy(stage => stage.Id, StringComparer.Ordinal)
                        .Select(stage => new ScheduledConsequenceStageState(
                            stage.Id,
                            trigger.RaisedWorldDay + stage.DelayDays,
                            new[] { stage.Id }))
                        .ToList();
                    var process = new ScheduledConsequenceState(
                        game.World.RuntimeIds.Allocate("world-process"),
                        consequence.Id,
                        branch.Id,
                        trigger.SourceLocationId ?? "world",
                        trigger.Id,
                        affectedContextIds: null,
                        stages);
                    game.World.ScheduleConsequence(process);
                    game.World.RecordTrace(
                        SimulationTraceKind.WorldProcessScheduled,
                        $"Consequence '{consequence.Id}' fixed branch '{branch.Id}' and scheduled {stages.Count} stage(s).",
                        new[] { triggerTrace.TraceId },
                        new[] { process.Id, consequence.Id });
                }
            }
            factionReactionResolver.Resolve(game, trigger);
            territorialPolicyResolver.Resolve(game, trigger);
            trigger.MarkResolved();
            messages.Add($"World trigger resolved: {trigger.TriggerId}.");
        }

        foreach (var consequence in game.World.ScheduledConsequences)
        {
            while (consequence.IsDue(game.World.WorldDay))
            {
                var scheduledStage = consequence.CurrentStage;
                if (scheduledStage == null) break;
                var location = game.World.Locations.FirstOrDefault(item => item.Id == consequence.SourceLocationId);
                var definition = content?.FindConsequence(consequence.DefinitionId);
                var stage = definition?.FindStage(consequence.ResolvedBranchId, scheduledStage.StageId);
                var appliedStage = consequence.TryApplyDueStage(game.World.WorldDay);
                if (appliedStage == null) break;

                var stageTrace = game.World.RecordTrace(
                    SimulationTraceKind.ConsequenceStageApplied,
                    $"World process '{consequence.Id}' applied stage '{appliedStage.StageId}'.",
                    consequence.SourceTriggerId == null ? null : game.World.Traces
                        .Where(trace => trace.SubjectIds.Contains(consequence.SourceTriggerId, StringComparer.Ordinal))
                        .Select(trace => trace.TraceId)
                        .Take(1),
                    new[] { consequence.Id, appliedStage.StageId, consequence.SourceLocationId });

                if (stage?.EvidenceId != null)
                {
                    var text = content?.Evidence.Find(stage.EvidenceId)?.WorldEventText ?? stage.EventBody ?? "A past expedition action has begun to change the surrounding world.";
                    game.Knowledge.AddEvidence(new EvidenceState(
                        game.World.RuntimeIds.Allocate("evidence-world"),
                        stage.EvidenceId,
                        EvidenceSourceKind.WorldEvent,
                        EvidenceKnowledgeState.Reported,
                        text,
                        subjectLocationId: location?.Id));
                    game.World.RecordTrace(
                        SimulationTraceKind.KnowledgeObserved,
                        $"World process stage '{appliedStage.StageId}' created evidence '{stage.EvidenceId}'.",
                        new[] { stageTrace.TraceId },
                        new[] { stage.EvidenceId, consequence.Id });
                }

                foreach (var situationDefinitionId in stage?.SituationDefinitionIds ?? Array.Empty<string>())
                {
                    if (authoring == null || !authoring.Situations.ContainsKey(situationDefinitionId))
                    {
                        continue;
                    }

                    var situation = new WorldSituationState(
                        game.World.RuntimeIds.Allocate("situation"),
                        situationDefinitionId,
                        game.World.WorldDay,
                        sourceProcessId: consequence.Id,
                        sourceLocationId: location?.Id);
                    situation.Activate();
                    game.World.AddSituation(situation);
                    game.World.RecordTrace(
                        SimulationTraceKind.SituationChanged,
                        $"World process stage '{appliedStage.StageId}' activated situation '{situationDefinitionId}'.",
                        new[] { stageTrace.TraceId },
                        new[] { situation.Id, situationDefinitionId, consequence.Id });
                }

                var title = stage?.EventTitle ?? "World changed";
                var body = stage?.EventBody ?? "A past expedition action has begun to change the surrounding world.";
                game.Events.Enqueue(new EventState(
                    game.World.RuntimeIds.Allocate("world-consequence"),
                    EventKind.WorldConsequence,
                    title,
                    "World",
                    body,
                    new[] { new EventOptionState("acknowledge", "Record observation", "The expedition records the change.", EventOptionEffectKind.Archive) },
                    location?.Coord));
                messages.Add($"Scheduled consequence stage applied: {consequence.DefinitionId}/{appliedStage.StageId}.");
            }
        }

        return messages;
    }
}
}
