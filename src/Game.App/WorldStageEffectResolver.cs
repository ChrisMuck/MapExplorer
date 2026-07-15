#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Applies an authored consequence effect to concrete runtime objects. Definitions contain only
/// neutral target roles; generated location relations select any actual faction at runtime.
/// </summary>
public sealed class WorldStageEffectResolver
{
    private readonly CrossSystemDataBundle? content;
    private readonly CrossSystemAuthoringBundle? authoring;

    public WorldStageEffectResolver(CrossSystemDataBundle? content, CrossSystemAuthoringBundle? authoring)
    {
        this.content = content;
        this.authoring = authoring;
    }

    public int Apply(GameState game, ScheduledConsequenceState process, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (process == null) throw new ArgumentNullException(nameof(process));
        if (effect == null) throw new ArgumentNullException(nameof(effect));

        if (effect.SuppressIfSituationResponseTags.Count > 0 && game.World.Situations.Any(situation =>
                situation.SourceProcessId == process.Id && situation.Status == WorldSituationStatus.Resolved &&
                situation.ResolutionActionTag != null && effect.SuppressIfSituationResponseTags.Contains(situation.ResolutionActionTag, StringComparer.Ordinal)))
        {
            return Trace(game, process, effect, stageTraceId, process.Id, "was prevented by an earlier situation response");
        }

        switch (effect.Kind)
        {
            case WorldStageEffectKind.AddEvidence:
                return AddEvidence(game, location, effect, stageTraceId, process);
            case WorldStageEffectKind.ChangeLocationState:
                if (location == null) return 0;
                location.SetState(effect.StateChannel ?? throw new InvalidOperationException("Location-state effect lacks a channel."), effect.StateId ?? throw new InvalidOperationException("Location-state effect lacks a state."));
                return Trace(game, process, effect, stageTraceId, location.Id, "changed a location state");
            case WorldStageEffectKind.SetLocationFlag:
                if (location == null || effect.ReferenceId == null) return 0;
                location.SetFlag(effect.ReferenceId);
                return Trace(game, process, effect, stageTraceId, location.Id, "set a location flag");
            case WorldStageEffectKind.SetTilePassability:
                return SetTilePassability(game, process, location, effect, stageTraceId);
            case WorldStageEffectKind.RaiseWorldTrigger:
                return RaiseTrigger(game, process, location, effect, stageTraceId);
            case WorldStageEffectKind.CreateSituation:
                return CreateSituation(game, process, location, effect, stageTraceId);
            case WorldStageEffectKind.EscalateRelatedFactionAwareness:
                return EscalateRelatedAwareness(game, process, location, effect, stageTraceId);
            case WorldStageEffectKind.CreateConnection:
                return CreateConnection(game, process, location, effect, stageTraceId);
            default:
                throw new InvalidOperationException($"Unsupported world-stage effect '{effect.Kind}'.");
        }
    }

    private int AddEvidence(GameState game, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId, ScheduledConsequenceState process)
    {
        if (effect.ReferenceId == null) return 0;
        var definition = content?.Evidence.Find(effect.ReferenceId);
        var text = definition?.WorldEventText ?? "Eine vergangene Handlung verändert die Umgebung.";
        game.Knowledge.AddEvidence(new EvidenceState(
            game.World.RuntimeIds.Allocate("evidence-world"), effect.ReferenceId, EvidenceSourceKind.WorldEvent,
            EvidenceKnowledgeState.Reported, text, subjectLocationId: location?.Id));
        return Trace(game, process, effect, stageTraceId, effect.ReferenceId, "created player evidence");
    }

    private static int SetTilePassability(GameState game, ScheduledConsequenceState process, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId)
    {
        var coord = location?.Coord;
        if (!coord.HasValue || !game.World.Map.TryGetTile(coord.Value, out var tile) || tile == null || !effect.IsBlocked.HasValue) return 0;
        game.World.Map.SetTile(tile.WithTerrain(tile.Terrain, isBlocked: effect.IsBlocked.Value));
        return Trace(game, process, effect, stageTraceId, coord.Value.ToString(), "changed tile passability");
    }

    private static int RaiseTrigger(GameState game, ScheduledConsequenceState process, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId)
    {
        if (effect.ReferenceId == null) return 0;
        game.World.QueueWorldTrigger(new WorldTriggerState(
            game.World.RuntimeIds.Allocate("world-trigger"), effect.ReferenceId, game.World.WorldDay,
            effect.ActionTags, location?.Id, location?.Coord, stageTraceId));
        return Trace(game, process, effect, stageTraceId, effect.ReferenceId, "raised a follow-up trigger");
    }

    private int CreateSituation(GameState game, ScheduledConsequenceState process, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId)
    {
        if (effect.ReferenceId == null || authoring == null || !authoring.Situations.ContainsKey(effect.ReferenceId)) return 0;
        var factionId = location?.FactionRelations.Count == 1 ? location.FactionRelations[0].FactionId : null;
        var situation = new WorldSituationState(
            game.World.RuntimeIds.Allocate("situation"), effect.ReferenceId, game.World.WorldDay,
            process.Id, location?.Id, effect.DueDays > 0 ? game.World.WorldDay + effect.DueDays : null, factionId: factionId);
        situation.Activate();
        if (!game.World.TryAddSituation(situation)) return 0;
        return Trace(game, process, effect, stageTraceId, situation.Id, "activated a situation");
    }

    private static int EscalateRelatedAwareness(GameState game, ScheduledConsequenceState process, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId)
    {
        if (location == null) return 0;
        var changed = 0;
        foreach (var relation in location.FactionRelations)
        {
            game.World.EscalateFactionAwareness(relation.FactionId, $"location-region:{location.Id}");
            changed += Trace(game, process, effect, stageTraceId, relation.FactionId, "escalated generated faction awareness");
        }
        return changed;
    }

    private static int CreateConnection(GameState game, ScheduledConsequenceState process, SpecialLocationState? location, WorldStageEffectDefinition effect, string stageTraceId)
    {
        if (location == null || effect.ReferenceId == null || effect.ConnectionKind == null) return 0;
        var connection = new WorldConnectionState(game.World.RuntimeIds.Allocate("world-connection"), location.Id, effect.ReferenceId, effect.ConnectionKind, game.World.WorldDay, effect.ActionTags);
        if (!game.World.AddConnection(connection)) return 0;
        return Trace(game, process, effect, stageTraceId, connection.Id, "created a logical world connection");
    }

    private static int Trace(GameState game, ScheduledConsequenceState process, WorldStageEffectDefinition effect, string stageTraceId, string subjectId, string description)
    {
        game.World.RecordTrace(SimulationTraceKind.ConsequenceStageApplied,
            $"World process '{process.Id}' effect '{effect.Id}' {description}.",
            new[] { stageTraceId }, new[] { process.Id, effect.Id, subjectId });
        return 1;
    }
}
}
