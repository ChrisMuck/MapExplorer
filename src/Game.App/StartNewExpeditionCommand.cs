#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class StartNewExpeditionCommand
{
    // Base resource budgets available for a departure loadout. Balancing placeholders.
    public const int RationBudget = 40;
    public const int MedicineBudget = 4;

    private readonly KnowledgeService knowledgeService = new KnowledgeService();

    public StartNewExpeditionResult Execute(GameState game)
    {
        return Execute(game, null);
    }

    public StartNewExpeditionResult Execute(GameState game, IReadOnlyList<string>? selectedMemberIds)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return StartNewExpeditionResult.Rejected("Current expedition is still active.");
        }

        if (!game.Base.CanStartNextExpedition(game.World.WorldDay))
        {
            var waitDays = Math.Max(0, game.Base.NextExpeditionAvailableWorldDay - game.World.WorldDay);
            return StartNewExpeditionResult.Rejected($"Next expedition is not ready. Wait {waitDays} day(s).");
        }

        var nextNumber = game.Expedition.ExpeditionNumber + 1;
        if (!TryBuildMembers(game, selectedMemberIds, nextNumber, out var members, out var memberError))
        {
            return StartNewExpeditionResult.Rejected(memberError!);
        }

        var supplyBonus = game.Base.ConsumePendingSupplyBonus();
        var expedition = new ExpeditionState(
            nextNumber,
            game.Base.Location,
            members,
            expeditionDay: 1,
            movementPoints: 4,
            maxMovementPoints: 4,
            supplies: 20 + supplyBonus,
            medicine: 3,
            morale: 70,
            capacity: 20);

        game.SetExpedition(expedition);
        knowledgeService.RevealFromExpedition(game.World.Map, game.Knowledge, game.Base.Location);

        var archiveEntry = supplyBonus > 0
            ? $"Expedition {nextNumber} prepared at the base on world day {game.World.WorldDay}. Supply preparation bonus: +{supplyBonus}."
            : $"Expedition {nextNumber} prepared at the base on world day {game.World.WorldDay}.";
        game.Base.AddArchiveEntry(archiveEntry);
        game.Base.MarkExpeditionDepartureArchivePoint();

        return StartNewExpeditionResult.Started(nextNumber, game.Base.Location, archiveEntry);
    }

    /// <summary>
    /// Departure with an explicit loadout: chosen roster members, generic units drawn from the base
    /// stock, and rations/medicine within the base budgets. Readiness is derived in Core; an
    /// overloaded loadout is rejected before it can leave.
    /// </summary>
    public StartNewExpeditionResult Execute(
        GameState game,
        IReadOnlyList<string>? selectedMemberIds,
        IReadOnlyList<string>? selectedUnitIds,
        int rations,
        int medicine)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return StartNewExpeditionResult.Rejected("Current expedition is still active.");
        }

        if (!game.Base.CanStartNextExpedition(game.World.WorldDay))
        {
            var waitDays = Math.Max(0, game.Base.NextExpeditionAvailableWorldDay - game.World.WorldDay);
            return StartNewExpeditionResult.Rejected($"Next expedition is not ready. Wait {waitDays} day(s).");
        }

        var nextNumber = game.Expedition.ExpeditionNumber + 1;
        if (!TryBuildMembers(game, selectedMemberIds, nextNumber, out var members, out var memberError))
        {
            return StartNewExpeditionResult.Rejected(memberError!);
        }

        var memberList = members.ToList();
        if (memberList.Count == 0)
        {
            return StartNewExpeditionResult.Rejected("Select at least one expedition member.");
        }

        var supplyBonus = game.Base.PendingSupplyBonus;
        var clampedRations = Clamp(rations, 0, RationBudget + supplyBonus);
        var clampedMedicine = Clamp(medicine, 0, MedicineBudget);

        var selectedUnits = new List<BaseUnitState>();
        if (selectedUnitIds != null)
        {
            foreach (var unitId in selectedUnitIds)
            {
                var unit = game.Base.UnitStock.FindUnit(unitId);
                if (unit == null)
                {
                    return StartNewExpeditionResult.Rejected($"Unknown unit '{unitId}'.");
                }

                if (selectedUnits.Any(u => u.Id == unit.Id))
                {
                    return StartNewExpeditionResult.Rejected($"Unit '{unitId}' selected twice.");
                }

                selectedUnits.Add(unit);
            }
        }

        var readiness = ExpeditionReadiness.Compute(memberList.Count, selectedUnits, clampedRations, clampedMedicine);
        if (readiness.Overload)
        {
            return StartNewExpeditionResult.Rejected(
                $"Loadout is overloaded ({readiness.Load}/{readiness.CarryCapacity}). Reduce rations/medicine or add porters.");
        }

        // Committing the loadout consumes the base supply bonus.
        game.Base.ConsumePendingSupplyBonus();

        var expedition = new ExpeditionState(
            nextNumber,
            game.Base.Location,
            memberList,
            expeditionDay: 1,
            movementPoints: readiness.SlowMarch ? 3 : 4,
            maxMovementPoints: readiness.SlowMarch ? 3 : 4,
            supplies: clampedRations,
            medicine: clampedMedicine,
            morale: 70,
            capacity: readiness.CarryCapacity);

        game.SetExpedition(expedition);
        knowledgeService.RevealFromExpedition(game.World.Map, game.Knowledge, game.Base.Location);

        var archiveEntry =
            $"Expedition {nextNumber} departed with {memberList.Count} member(s), {selectedUnits.Count(u => u.Kind == BaseUnitKind.Porter)} porter(s), " +
            $"{selectedUnits.Count(u => u.Kind == BaseUnitKind.Soldier)} soldier(s), {clampedRations} rations and {clampedMedicine} medicine on world day {game.World.WorldDay}.";
        game.Base.AddArchiveEntry(archiveEntry);
        game.Base.MarkExpeditionDepartureArchivePoint();

        return StartNewExpeditionResult.Started(nextNumber, game.Base.Location, archiveEntry);
    }

    private static bool TryBuildMembers(
        GameState game,
        IReadOnlyList<string>? selectedMemberIds,
        int nextNumber,
        out IEnumerable<ExpeditionMemberState> members,
        out string? error)
    {
        error = null;
        if (selectedMemberIds != null && selectedMemberIds.Count > 0)
        {
            var built = new List<ExpeditionMemberState>();
            foreach (var id in selectedMemberIds)
            {
                var rosterMember = game.Roster.FindMember(id);
                if (rosterMember == null)
                {
                    members = Array.Empty<ExpeditionMemberState>();
                    error = $"Unknown roster member '{id}'.";
                    return false;
                }

                if (!rosterMember.IsAvailable)
                {
                    members = Array.Empty<ExpeditionMemberState>();
                    error = $"{rosterMember.Name} is not available for the expedition.";
                    return false;
                }

                built.Add(rosterMember.ToExpeditionMember());
            }

            members = built;
            return true;
        }

        members = CreateNextMembers(game.Expedition, game.Base.LastExpeditionOutcome, nextNumber);
        return true;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private static IEnumerable<ExpeditionMemberState> CreateNextMembers(ExpeditionState previous, ExpeditionStatus? outcome, int expeditionNumber)
    {
        if (outcome == ExpeditionStatus.Returned)
        {
            var returningMembers = previous.Members
                .Where(member => member.Status != ExpeditionMemberStatus.Dead && member.Status != ExpeditionMemberStatus.Missing)
                .Select(member => new ExpeditionMemberState(member.Id, member.Name, member.Role))
                .ToList();

            if (returningMembers.Count > 0)
            {
                return returningMembers;
            }
        }

        return new[]
        {
            new ExpeditionMemberState($"scout-{expeditionNumber}-1", "Mira", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState($"scout-{expeditionNumber}-2", "Tovin", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState($"guard-{expeditionNumber}-1", "Bram", ExpeditionMemberRole.Guard),
            new ExpeditionMemberState($"guard-{expeditionNumber}-2", "Ilyra", ExpeditionMemberRole.Guard),
            new ExpeditionMemberState($"carrier-{expeditionNumber}-1", "Nessa", ExpeditionMemberRole.Carrier),
            new ExpeditionMemberState($"carrier-{expeditionNumber}-2", "Oren", ExpeditionMemberRole.Carrier),
            new ExpeditionMemberState($"medic-{expeditionNumber}-1", "Sela", ExpeditionMemberRole.Medic),
            new ExpeditionMemberState($"scholar-{expeditionNumber}-1", "Rook", ExpeditionMemberRole.Scholar)
        };
    }
}
}
