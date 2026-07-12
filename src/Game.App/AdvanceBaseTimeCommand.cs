#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class AdvanceBaseTimeCommand
{
    private readonly WorldPhaseService worldPhaseService = new WorldPhaseService();
    // Authored, deterministic world reactions surfaced while base time passes. The world keeps
    // moving during preparation; the entry is chosen by world day so a rebuild is reproducible.
    private static readonly string[] WorldReactions =
    {
        "Coastal People report smoke rising in the interior.",
        "Border Wardens renewed the warning markers near the ravine.",
        "Travellers say the abandoned camp was disturbed again.",
        "The gray river ran high; a ford near the crossing washed out.",
        "The Hidden Ones' tracks were seen and then lost in the northwest."
    };

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
        game.Base.EvaluationQueue.AdvanceDays(days);
        worldPhaseService.Resolve(game);

        var reaction = WorldReactions[game.World.WorldDay % WorldReactions.Length];
        var reactionEntry = $"World day {game.World.WorldDay}: {reaction}";
        game.Base.AddArchiveEntry(reactionEntry);

        return AdvanceBaseTimeResult.Advanced(
            game.World.WorldDay,
            game.Base.CanStartNextExpedition(game.World.WorldDay),
            reactionEntry);
    }
}
}
