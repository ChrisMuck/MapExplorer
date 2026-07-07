#nullable enable
using System;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Builds a persistent base upgrade during the base phase. Costs Knowledge Points and requires any
/// prerequisite upgrades to be built first. Built upgrades stay built across expeditions and expose
/// effects other systems read (e.g. cheaper healing).
/// </summary>
public sealed class StartUpgradeCommand
{
    public StartUpgradeResult Execute(GameState game, string upgradeId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (string.IsNullOrWhiteSpace(upgradeId))
        {
            return StartUpgradeResult.Rejected("No upgrade selected.");
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return StartUpgradeResult.Rejected("The base can only be upgraded during base preparation.");
        }

        var upgrade = game.Base.Upgrades.FindUpgrade(upgradeId);
        if (upgrade == null)
        {
            return StartUpgradeResult.Rejected($"Unknown upgrade '{upgradeId}'.");
        }

        if (upgrade.IsBuilt)
        {
            return StartUpgradeResult.Rejected($"{upgrade.Name} is already built.");
        }

        if (!game.Base.Upgrades.PrerequisitesMet(upgrade))
        {
            return StartUpgradeResult.Rejected($"{upgrade.Name} still needs its prerequisites.");
        }

        if (!game.Base.SpendKnowledgePoints(upgrade.Cost))
        {
            return StartUpgradeResult.Rejected($"Not enough Knowledge Points. Need {upgrade.Cost}.");
        }

        upgrade.MarkBuilt();
        var entry = $"Base upgrade built: {upgrade.Name} ({upgrade.Cost} Knowledge).";
        game.Base.AddArchiveEntry(entry);

        return StartUpgradeResult.Built(upgrade.Id, upgrade.Name, upgrade.Cost, game.Base.KnowledgePoints, entry);
    }
}
}
