using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class BaseState
{
    private readonly List<ArchiveEntryState> archive = new();
    private readonly List<LostExpeditionRecord> lostExpeditions = new();
    private int expeditionArchiveStartIndex;

    public BaseState(HexCoord location, BaseUpgradesState? upgrades = null, EvaluationQueueState? evaluationQueue = null, BaseUnitStockState? unitStock = null)
    {
        Location = location;
        Upgrades = upgrades ?? new BaseUpgradesState();
        EvaluationQueue = evaluationQueue ?? new EvaluationQueueState();
        UnitStock = unitStock ?? new BaseUnitStockState();
    }

    public HexCoord Location { get; }

    public BaseUpgradesState Upgrades { get; }

    public EvaluationQueueState EvaluationQueue { get; }

    public BaseUnitStockState UnitStock { get; }

    public ExpeditionStatus? LastExpeditionOutcome { get; private set; }

    public int NextExpeditionAvailableWorldDay { get; private set; } = 1;

    public int KnowledgePoints { get; private set; }

    public int PendingSupplyBonus { get; private set; }

    public bool HasRequestedEngineer { get; private set; }

    /// <summary>Typed archive entries; the source of truth for the base archive.</summary>
    public IReadOnlyList<ArchiveEntryState> Archive
    {
        get { return archive; }
    }

    /// <summary>Back-compat flat view of the archive as strings.</summary>
    public IReadOnlyList<string> ArchiveEntries
    {
        get { return archive.Select(entry => entry.Text).ToList(); }
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

        archive.Add(ArchiveEntryState.Note(entry));
    }

    public void AddArchiveEntry(ArchiveEntryState entry)
    {
        archive.Add(entry ?? throw new ArgumentNullException(nameof(entry)));
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

    public void MarkEngineerRequested()
    {
        HasRequestedEngineer = true;
    }

    public void MarkExpeditionDepartureArchivePoint()
    {
        expeditionArchiveStartIndex = archive.Count;
    }

    public void SecureCurrentExpeditionArchiveEntries()
    {
        expeditionArchiveStartIndex = archive.Count;
    }

    public void DiscardCurrentExpeditionArchiveEntries()
    {
        if (archive.Count <= expeditionArchiveStartIndex)
        {
            return;
        }

        archive.RemoveRange(expeditionArchiveStartIndex, archive.Count - expeditionArchiveStartIndex);
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
        // A campaign may be in its initial base-preparation state before Expedition 1 exists.
        // Later departures still require the normal returned/lost outcome and its scheduled date.
        return currentWorldDay >= NextExpeditionAvailableWorldDay &&
            (!LastExpeditionOutcome.HasValue ||
             LastExpeditionOutcome == ExpeditionStatus.Returned ||
             LastExpeditionOutcome == ExpeditionStatus.Lost);
    }
}
}
