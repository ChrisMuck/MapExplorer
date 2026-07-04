#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class ExpeditionState
{
    private readonly List<ExpeditionMemberState> members;
    private readonly List<ScoutMissionState> scoutMissions = new();

    public ExpeditionState(
        int expeditionNumber,
        HexCoord position,
        IEnumerable<ExpeditionMemberState> members,
        int expeditionDay = 1,
        int movementPoints = 4,
        int maxMovementPoints = 4,
        int supplies = 20,
        int medicine = 3,
        int morale = 70,
        int capacity = 20,
        ExpeditionStatus status = ExpeditionStatus.Active)
    {
        if (expeditionNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expeditionNumber), expeditionNumber, "Expedition number must be at least 1.");
        }

        if (expeditionDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expeditionDay), expeditionDay, "Expedition day must be at least 1.");
        }

        if (movementPoints < 0 || maxMovementPoints < 0 || supplies < 0 || medicine < 0 || morale < 0 || capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(movementPoints), "Expedition resources must not be negative.");
        }

        if (movementPoints > maxMovementPoints)
        {
            throw new ArgumentOutOfRangeException(nameof(movementPoints), movementPoints, "Movement points must not exceed max movement points.");
        }

        ExpeditionNumber = expeditionNumber;
        Position = position;
        ExpeditionDay = expeditionDay;
        MovementPoints = movementPoints;
        MaxMovementPoints = maxMovementPoints;
        Supplies = supplies;
        Medicine = medicine;
        Morale = morale;
        Capacity = capacity;
        Status = status;
        this.members = new List<ExpeditionMemberState>(members ?? throw new ArgumentNullException(nameof(members)));
    }

    public int ExpeditionNumber { get; }

    public HexCoord Position { get; private set; }

    public int ExpeditionDay { get; private set; }

    public int MovementPoints { get; private set; }

    public int MaxMovementPoints { get; }

    public int Supplies { get; private set; }

    public int Medicine { get; private set; }

    public int Morale { get; private set; }

    public int Capacity { get; private set; }

    public ExpeditionStatus Status { get; private set; }

    public IReadOnlyList<ExpeditionMemberState> Members
    {
        get { return members; }
    }

    public IReadOnlyList<ScoutMissionState> ScoutMissions
    {
        get { return scoutMissions; }
    }

    public ExpeditionMemberState? FindMember(string memberId)
    {
        return members.FirstOrDefault(member => member.Id == memberId);
    }

    public void AddScoutMission(ScoutMissionState mission)
    {
        scoutMissions.Add(mission ?? throw new ArgumentNullException(nameof(mission)));
    }

    public void SetPosition(HexCoord position)
    {
        Position = position;
    }

    public void SpendMovementPoints(int cost)
    {
        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "Movement cost must not be negative.");
        }

        if (cost > MovementPoints)
        {
            throw new InvalidOperationException("Not enough movement points.");
        }

        MovementPoints -= cost;
    }

    public void AdvanceExpeditionDay()
    {
        ExpeditionDay += 1;
        MovementPoints = MaxMovementPoints;
    }

    public void ConsumeSupplies(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Supply consumption must not be negative.");
        }

        Supplies = Math.Max(0, Supplies - amount);
    }
}
}
