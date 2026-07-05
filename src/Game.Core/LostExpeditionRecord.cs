#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class LostExpeditionRecord
{
    private readonly List<string> possibleRecoveryClueIds = new();
    private readonly List<string> recoveredArchiveEntryIds = new();

    public LostExpeditionRecord(
        string expeditionId,
        int expeditionNumber,
        int lastKnownWorldDay,
        HexCoord lastKnownPosition,
        int estimatedLostKnowledge,
        LostExpeditionStatus status = LostExpeditionStatus.Missing,
        IEnumerable<string>? possibleRecoveryClueIds = null,
        IEnumerable<string>? recoveredArchiveEntryIds = null)
    {
        if (string.IsNullOrWhiteSpace(expeditionId))
        {
            throw new ArgumentException("Expedition id must not be empty.", nameof(expeditionId));
        }

        if (expeditionNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expeditionNumber), expeditionNumber, "Expedition number must be at least 1.");
        }

        if (lastKnownWorldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lastKnownWorldDay), lastKnownWorldDay, "World day must be at least 1.");
        }

        if (estimatedLostKnowledge < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedLostKnowledge), estimatedLostKnowledge, "Lost knowledge must not be negative.");
        }

        ExpeditionId = expeditionId;
        ExpeditionNumber = expeditionNumber;
        LastKnownWorldDay = lastKnownWorldDay;
        LastKnownPosition = lastKnownPosition;
        EstimatedLostKnowledge = estimatedLostKnowledge;
        Status = status;
        this.possibleRecoveryClueIds.AddRange(possibleRecoveryClueIds ?? Enumerable.Empty<string>());
        this.recoveredArchiveEntryIds.AddRange(recoveredArchiveEntryIds ?? Enumerable.Empty<string>());
    }

    public string ExpeditionId { get; }

    public int ExpeditionNumber { get; }

    public int LastKnownWorldDay { get; }

    public HexCoord LastKnownPosition { get; }

    public int EstimatedLostKnowledge { get; }

    public int RecoveredKnowledge { get; private set; }

    public LostExpeditionStatus Status { get; private set; }

    public IReadOnlyList<string> PossibleRecoveryClueIds
    {
        get { return possibleRecoveryClueIds; }
    }

    public IReadOnlyList<string> RecoveredArchiveEntryIds
    {
        get { return recoveredArchiveEntryIds; }
    }

    public void AddRecoveryClue(string clueId)
    {
        if (string.IsNullOrWhiteSpace(clueId) || possibleRecoveryClueIds.Contains(clueId))
        {
            return;
        }

        possibleRecoveryClueIds.Add(clueId);
    }

    public void RecordRecoveredKnowledge(int amount, string archiveEntryId)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Recovered knowledge must not be negative.");
        }

        RecoveredKnowledge += amount;
        if (!string.IsNullOrWhiteSpace(archiveEntryId) && !recoveredArchiveEntryIds.Contains(archiveEntryId))
        {
            recoveredArchiveEntryIds.Add(archiveEntryId);
        }

        Status = RecoveredKnowledge >= EstimatedLostKnowledge
            ? LostExpeditionStatus.FullyResolved
            : LostExpeditionStatus.PartiallyRecovered;
    }
}
}
