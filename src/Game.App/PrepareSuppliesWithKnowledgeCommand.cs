#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class PrepareSuppliesWithKnowledgeCommand
{
    public const int KnowledgeCost = 10;
    public const int SupplyBonus = 20;

    public PrepareSuppliesWithKnowledgeResult Execute(GameState game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return PrepareSuppliesWithKnowledgeResult.Rejected("Knowledge can only be spent during base preparation.");
        }

        if (!game.Base.SpendKnowledgePoints(KnowledgeCost))
        {
            return PrepareSuppliesWithKnowledgeResult.Rejected($"Not enough Knowledge Points. Need {KnowledgeCost}.");
        }

        game.Base.AddPendingSupplyBonus(SupplyBonus);
        var archiveEntry = $"Base preparation: spent {KnowledgeCost} Knowledge for +{SupplyBonus} supplies on the next expedition.";
        game.Base.AddArchiveEntry(archiveEntry);

        return PrepareSuppliesWithKnowledgeResult.Prepared(
            KnowledgeCost,
            SupplyBonus,
            game.Base.KnowledgePoints,
            game.Base.PendingSupplyBonus,
            archiveEntry);
    }
}
}
