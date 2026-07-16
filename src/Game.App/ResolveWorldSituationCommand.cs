#nullable enable
using System;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>Records a player's response to a visible world warning through the application layer.</summary>
public sealed class ResolveWorldSituationCommand
{
    public const string PromiseReturnActionTag = "promise-return";
    private readonly CrossSystemAuthoringBundle authoring;

    public ResolveWorldSituationCommand(CrossSystemAuthoringBundle authoring)
    {
        this.authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
    }

    public bool Execute(GameState game, string situationId, string responseActionTag)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (string.IsNullOrWhiteSpace(situationId) || string.IsNullOrWhiteSpace(responseActionTag)) return false;
        var situation = game.World.Situations.FirstOrDefault(item => item.Id == situationId.Trim());
        if (situation == null || (situation.Status != WorldSituationStatus.Active && situation.Status != WorldSituationStatus.Promised)) return false;
        if (!authoring.Situations.TryGetValue(situation.DefinitionId, out var definition) || !definition.ResponseActionTags.Contains(responseActionTag.Trim(), StringComparer.Ordinal)) return false;

        if (responseActionTag.Trim() == PromiseReturnActionTag) situation.Promise(responseActionTag);
        else situation.Resolve(responseActionTag);
        game.World.RecordTrace(SimulationTraceKind.SituationChanged,
            $"Situation '{situation.Id}' resolved with response '{responseActionTag.Trim()}'.",
            subjectIds: new[] { situation.Id, situation.DefinitionId, responseActionTag.Trim() });
        return true;
    }
}
}
