using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class BaseState
{
    private readonly List<string> archiveEntries = new();
    private readonly List<LostExpeditionRecord> lostExpeditions = new();
    private int expeditionArchiveStartIndex;

    public BaseState(HexCoord location)
    {
        Location = location;
    }

    public HexCoord Location { get; }

    public ExpeditionStatus? LastExpeditionOutcome { get; private set; }

    public int NextExpeditionAvailableWorldDay { get; private set; } = 1;

    public int KnowledgePoints { get; private set; }

    public int PendingSupplyBonus { get; private set; }

    public IReadOnlyList<string> ArchiveEntries
    {
        get { return archiveEntries; }
    }

    public IReadOnlyList<LostExpeditionRecord> LostExpeditions
    {
        get { return lostExpeditions; }
    }

    public void AddArchiveEntry(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            throw new ArgumentException("Archive entry must not be empty.", nameof(entry));
        }

        archiveEntries.Add(entry);
    }

    public void AddLostExpeditionRecord(LostExpeditionRecord record)
    {
        lostExpeditions.Add(record ?? throw new ArgumentNullException(nameof(record)));
    }

    public void AddKnowledgePoints(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Knowledge points must not be negative.");
        }

        KnowledgePoints += amount;
    }

    public bool SpendKnowledgePoints(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Knowledge point cost must not be negative.");
        }

        if (amount > KnowledgePoints)
        {
            return false;
        }

        KnowledgePoints -= amount;
        return true;
    }

    public void AddPendingSupplyBonus(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Supply bonus must not be negative.");
        }

        PendingSupplyBonus += amount;
    }

    public int ConsumePendingSupplyBonus()
    {
        var bonus = PendingSupplyBonus;
        PendingSupplyBonus = 0;
        return bonus;
    }

    public void MarkExpeditionDepartureArchivePoint()
    {
        expeditionArchiveStartIndex = archiveEntries.Count;
    }

    public void SecureCurrentExpeditionArchiveEntries()
    {
        expeditionArchiveStartIndex = archiveEntries.Count;
    }

    public void DiscardCurrentExpeditionArchiveEntries()
    {
        if (archiveEntries.Count <= expeditionArchiveStartIndex)
        {
            return;
        }

        archiveEntries.RemoveRange(expeditionArchiveStartIndex, archiveEntries.Count - expeditionArchiveStartIndex);
    }

    public void ScheduleNextExpedition(ExpeditionStatus outcome, int currentWorldDay, int delayDays)
    {
        if (outcome != ExpeditionStatus.Returned && outcome != ExpeditionStatus.Lost)
        {
            throw new ArgumentException("Only completed expedition outcomes can schedule a new expedition.", nameof(outcome));
        }

        if (currentWorldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentWorldDay), currentWorldDay, "World day must be at least 1.");
        }

        if (delayDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delayDays), delayDays, "Delay must not be negative.");
        }

        LastExpeditionOutcome = outcome;
        NextExpeditionAvailableWorldDay = currentWorldDay + delayDays;
    }

    public bool CanStartNextExpedition(int currentWorldDay)
    {
        return LastExpeditionOutcome.HasValue && currentWorldDay >= NextExpeditionAvailableWorldDay;
    }
}
}
