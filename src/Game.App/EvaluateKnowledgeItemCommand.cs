#nullable enable
using System;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Evaluates a ready item in the base knowledge-evaluation queue into an archived insight plus
/// bonus Knowledge Points. Items become ready as base time passes (see AdvanceBaseTimeCommand).
/// </summary>
public sealed class EvaluateKnowledgeItemCommand
{
    public EvaluateKnowledgeItemResult Execute(GameState game, string itemId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return EvaluateKnowledgeItemResult.Rejected("No item selected.");
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return EvaluateKnowledgeItemResult.Rejected("Knowledge can only be evaluated during base preparation.");
        }

        var item = game.Base.EvaluationQueue.FindItem(itemId);
        if (item == null)
        {
            return EvaluateKnowledgeItemResult.Rejected($"Unknown evaluation item '{itemId}'.");
        }

        if (item.IsEvaluated)
        {
            return EvaluateKnowledgeItemResult.Rejected($"{item.Name} has already been evaluated.");
        }

        if (!item.IsReady)
        {
            return EvaluateKnowledgeItemResult.Rejected($"{item.Name} is still being evaluated.");
        }

        item.MarkEvaluated();
        game.Base.AddKnowledgePoints(item.KnowledgeReward);
        var entry = $"Erkenntnis gesichert: {item.Name} (+{item.KnowledgeReward} Wissen). {item.InsightText}";
        game.Base.AddArchiveEntry(new ArchiveEntryState(
            entry,
            ArchiveEntryKind.Erkenntnis,
            "Auswertung",
            game.World.WorldDay,
            ArchiveReliability.Bestaetigt));

        return EvaluateKnowledgeItemResult.Evaluated(item.Id, item.Name, item.KnowledgeReward, game.Base.KnowledgePoints, item.InsightText, entry);
    }
}
}
