#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class AdvanceBaseTimeCommand
{
    public AdvanceBaseTimeResult Execute(GameState game, int days = 1)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Days must be at least one.");
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return AdvanceBaseTimeResult.Rejected("Base time can only advance after an expedition has ended.");
        }

        game.World.AdvanceDays(days);
        return AdvanceBaseTimeResult.Advanced(game.World.WorldDay, game.Base.CanStartNextExpedition(game.World.WorldDay));
    }
}
}
