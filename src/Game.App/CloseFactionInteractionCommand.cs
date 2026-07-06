#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class CloseFactionInteractionCommand
{
    public FactionInteractionResult Execute(GameState game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.ActiveFactionInteraction == null)
        {
            return FactionInteractionResult.Rejected("No active faction interaction.");
        }

        game.ClearActiveFactionInteraction();
        return FactionInteractionResult.Closed("Faction contact closed.");
    }
}
}
