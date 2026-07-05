#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class RecoverLostExpeditionCommand
{
    public RecoverLostExpeditionResult Execute(GameState game, HexCoord coord)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return RecoverLostExpeditionResult.Rejected("Expedition is not active.");
        }

        if (game.Expedition.Position != coord)
        {
            return RecoverLostExpeditionResult.Rejected("The expedition must be on the recovery field.");
        }

        var record = FindRecoverableRecord(game, coord);
        if (record == null)
        {
            return RecoverLostExpeditionResult.Rejected("No recoverable lost expedition trace on this field.");
        }

        var remainingKnowledge = record.EstimatedLostKnowledge - record.RecoveredKnowledge;
        if (remainingKnowledge <= 0)
        {
            return RecoverLostExpeditionResult.Rejected("This lost expedition trace has already been recovered.");
        }

        var recoveredKnowledge = Math.Min(remainingKnowledge, Math.Max(1, (record.EstimatedLostKnowledge + 1) / 2));
        var archiveEntryId = $"lost-expedition-recovery-{record.ExpeditionNumber}-{game.World.WorldDay}-{record.RecoveredArchiveEntryIds.Count + 1}";
        record.RecordRecoveredKnowledge(recoveredKnowledge, archiveEntryId);
        game.Expedition.AddUnsecuredKnowledge(recoveredKnowledge);

        var archiveEntry = $"Day {game.World.WorldDay}: Recovery lead for Expedition {record.ExpeditionNumber} investigated at {coord}. Recovered field knowledge: {recoveredKnowledge}.";
        game.Base.AddArchiveEntry(archiveEntry);

        return RecoverLostExpeditionResult.Recovered(record, recoveredKnowledge, archiveEntry);
    }

    private static LostExpeditionRecord? FindRecoverableRecord(GameState game, HexCoord coord)
    {
        foreach (var record in game.Base.LostExpeditions)
        {
            if (record.LastKnownPosition == coord && record.Status != LostExpeditionStatus.FullyResolved)
            {
                return record;
            }
        }

        return null;
    }
}
}
