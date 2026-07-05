#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class StartNewExpeditionCommand
{
    private readonly KnowledgeService knowledgeService = new KnowledgeService();

    public StartNewExpeditionResult Execute(GameState game)
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
        var members = CreateNextMembers(game.Expedition, game.Base.LastExpeditionOutcome, nextNumber);
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
