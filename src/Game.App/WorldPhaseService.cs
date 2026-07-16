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
    public const int MaximumConcurrentWorldProcesses = 8;
    private readonly CrossSystemDataBundle? content;
    private readonly CrossSystemAuthoringBundle? authoring;
    private readonly FactionReactionResolver factionReactionResolver;
    private readonly FactionTerritorialPolicyResolver territorialPolicyResolver;
    private readonly WorldStageEffectResolver stageEffectResolver;
    private readonly FactionObservationResolver factionObservationResolver;

    public WorldPhaseService(CrossSystemDataBundle? content = null, CrossSystemAuthoringBundle? authoring = null)
    {
        this.content = content;
        this.authoring = authoring;
        factionReactionResolver = new FactionReactionResolver(content);
        territorialPolicyResolver = new FactionTerritorialPolicyResolver(content);
        stageEffectResolver = new WorldStageEffectResolver(content, authoring);
        factionObservationResolver = new FactionObservationResolver(content);
    }

    public IReadOnlyList<string> Resolve(GameState game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        var messages = new List<string>();

        foreach (var trigger in game.World.WorldTriggers.OrderBy(item => item.RaisedWorldDay).ThenBy(item => item.Id, StringComparer.Ordinal))
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
                    if (game.World.ScheduledConsequences.Count(item => !item.IsCompleted) >= MaximumConcurrentWorldProcesses)
                    {
                        messages.Add($"World trigger deferred: {trigger.TriggerId}; process capacity reached.");
                        continue;
                    }

                    var branch = consequence.SelectBranch(new WorldDeterministicRandomSource(game.World));
                    var stages = branch.Stages
                        .OrderBy(stage => stage.DelayDays)
                        .ThenBy(stage => stage.Id, StringComparer.Ordinal)
                        .Select(stage => new ScheduledConsequenceStageState(
                            stage.Id,
                            trigger.RaisedWorldDay + stage.DelayDays,
                            EffectiveEffects(stage).Select(effect => effect.Id)))
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
            factionObservationResolver.Resolve(game, trigger);
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

                foreach (var effect in stage == null ? Array.Empty<WorldStageEffectDefinition>() : EffectiveEffects(stage))
                {
                    stageEffectResolver.Apply(game, consequence, location, effect, stageTrace.TraceId);
                }

                if (stage?.EventTitle != null && stage.EventBody != null)
                {
                    game.Events.Enqueue(new EventState(
                        game.World.RuntimeIds.Allocate("world-consequence"),
                        EventKind.WorldConsequence,
                        stage.EventTitle,
                        "World",
                        stage.EventBody,
                        new[] { new EventOptionState("acknowledge", "Record observation", "The expedition records the change.", EventOptionEffectKind.Archive) },
                        location?.Coord));
                }
                messages.Add($"Scheduled consequence stage applied: {consequence.DefinitionId}/{appliedStage.StageId}.");
            }
        }

        foreach (var situation in game.World.Situations.Where(item =>
                     (item.Status == WorldSituationStatus.Active || item.Status == WorldSituationStatus.Promised) &&
                     item.DueWorldDay.HasValue && game.World.WorldDay >= item.DueWorldDay.Value))
        {
            var wasPromised = situation.Status == WorldSituationStatus.Promised;
            situation.Expire(game.World.WorldDay);
            var location = situation.SourceLocationId == null
                ? null
                : game.World.Locations.FirstOrDefault(item => item.Id == situation.SourceLocationId);
            game.World.RecordTrace(
                SimulationTraceKind.SituationChanged,
                $"Situation '{situation.Id}' expired unanswered on world day {game.World.WorldDay}.",
                subjectIds: new[] { situation.Id, situation.DefinitionId });
            if (wasPromised && situation.FactionId != null && authoring?.Situations.TryGetValue(situation.DefinitionId, out var definition) == true)
            {
                var faction = game.FindFaction(situation.FactionId);
                faction?.Adjust(trustDelta: definition.BrokenPromiseTrustDelta);
                if (definition.BrokenPromiseMemoryId != null) faction?.AddMemory(definition.BrokenPromiseMemoryId);
            }
            game.Events.Enqueue(new EventState(
                game.World.RuntimeIds.Allocate("situation-expired"),
                EventKind.WorldConsequence,
                "Eine Gelegenheit ist verstrichen",
                "World",
                wasPromised
                    ? "Eine zugesagte Rueckmeldung oder Hilfe ist nicht rechtzeitig erfolgt."
                    : "Eine bekannte Anfrage oder Warnung wurde nicht rechtzeitig beantwortet.",
                new[] { new EventOptionState("acknowledge", "Zur Kenntnis nehmen", "Der Ablauf wird im Bericht festgehalten.", EventOptionEffectKind.Archive) },
                location?.Coord,
                situation.FactionId));
            messages.Add($"World situation expired: {situation.DefinitionId}.");
        }

        return messages;
    }

    private static IReadOnlyList<WorldStageEffectDefinition> EffectiveEffects(ConsequenceStageDefinition stage)
    {
        var effects = stage.Effects.ToList();
        if (stage.EvidenceId != null && !effects.Any(effect => effect.Kind == WorldStageEffectKind.AddEvidence && effect.ReferenceId == stage.EvidenceId))
        {
            effects.Add(new WorldStageEffectDefinition($"legacy-evidence:{stage.Id}", WorldStageEffectKind.AddEvidence, stage.EvidenceId));
        }
        foreach (var situationId in stage.SituationDefinitionIds)
        {
            if (!effects.Any(effect => effect.Kind == WorldStageEffectKind.CreateSituation && effect.ReferenceId == situationId))
            {
                effects.Add(new WorldStageEffectDefinition($"legacy-situation:{stage.Id}:{situationId}", WorldStageEffectKind.CreateSituation, situationId));
            }
        }
        if (effects.Count == 0)
        {
            effects.Add(new WorldStageEffectDefinition($"stage:{stage.Id}", WorldStageEffectKind.CreateConnection));
        }
        return effects;
    }
}
}
