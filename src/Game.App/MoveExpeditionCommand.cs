using System;
using Game.Core;

namespace Game.App
{

public sealed class MoveExpeditionCommand
{
    private const int FirstFactionContactKnowledge = 10;

    private readonly MovementCostService movementCostService;
    private readonly KnowledgeService knowledgeService;

    public MoveExpeditionCommand(MovementCostService movementCostService)
        : this(movementCostService, new KnowledgeService())
    {
    }

    public MoveExpeditionCommand(MovementCostService movementCostService, KnowledgeService knowledgeService)
    {
        this.movementCostService = movementCostService ?? throw new ArgumentNullException(nameof(movementCostService));
        this.knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
    }

    public MoveExpeditionResult Execute(GameState game, HexCoord destination)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return MoveExpeditionResult.Rejected("Expedition is not active.");
        }

        var from = game.Expedition.Position;
        if (from == destination)
        {
            return MoveExpeditionResult.Rejected("Expedition is already at the destination.");
        }

        if (from.DistanceTo(destination) != 1)
        {
            return MoveExpeditionResult.Rejected("Destination is not adjacent.");
        }

        if (!game.World.Map.TryGetTile(destination, out var destinationTile) || destinationTile == null)
        {
            return MoveExpeditionResult.Rejected("Destination is outside the map.");
        }

        var cost = movementCostService.GetEntryCost(destinationTile);
        if (!cost.CanEnter)
        {
            return MoveExpeditionResult.Rejected(cost.Reason ?? "Destination cannot be entered.");
        }

        if (cost.Cost > game.Expedition.MovementPoints)
        {
            return MoveExpeditionResult.Rejected("Not enough movement points.");
        }

        var destinationWasConfirmed = game.Knowledge.GetTileKnowledge(destination) == KnowledgeLevel.Confirmed;

        game.Expedition.SpendMovementPoints(cost.Cost);
        game.Expedition.SetPosition(destination);
        knowledgeService.RevealFromExpedition(game.World.Map, game.Knowledge, destination);
        if (!destinationWasConfirmed && game.Knowledge.ClaimKnowledgeSource($"confirmed-hex:{destination.Q}:{destination.R}"))
        {
            game.Expedition.AddUnsecuredKnowledge(1);
        }

        ApplyFactionEntry(game, destinationTile, destination);

        return MoveExpeditionResult.Moved(from, destination, cost.Cost);
    }

    private static void ApplyFactionEntry(GameState game, HexTileState destinationTile, HexCoord destination)
    {
        if (string.IsNullOrWhiteSpace(destinationTile.OwnerId))
        {
            return;
        }

        var faction = game.FindFaction(destinationTile.OwnerId);
        if (faction == null)
        {
            return;
        }

        ApplyFactionKnowledge(game, faction);

        if (!faction.IsWarningZone(destination))
        {
            return;
        }

        var memoryKey = $"warning-zone-entered:{destination.Q}:{destination.R}";
        if (faction.HasMemory(memoryKey))
        {
            return;
        }

        faction.AddMemory(memoryKey);
        if (faction.ContactStatus == FactionContactStatus.Unknown)
        {
            faction.SetContactStatus(FactionContactStatus.Rumored);
        }

        faction.Adjust(angerDelta: 6, fearDelta: 3);
        game.Events.Enqueue(CreateFactionWarningEvent(game, faction, destination));
    }

    private static void ApplyFactionKnowledge(GameState game, FactionState faction)
    {
        if (game.Knowledge.ClaimKnowledgeSource($"faction-contact:{faction.Id}"))
        {
            game.Expedition.AddUnsecuredKnowledge(FirstFactionContactKnowledge);
            faction.AddMemory($"expedition-entered-territory:{game.World.WorldDay}");
        }

        if (faction.ContactStatus == FactionContactStatus.Unknown)
        {
            faction.SetContactStatus(FactionContactStatus.Rumored);
        }
    }

    private static EventState CreateFactionWarningEvent(GameState game, FactionState faction, HexCoord coord)
    {
        return new EventState(
            $"event-{game.Events.Events.Count + 1}",
            EventKind.WarningSign,
            $"{faction.Name} Warning",
            faction.Name,
            "The expedition crossed a line marked by old stones and carved posts. This is not a clean border on a map, but someone likely expects it to be respected.",
            new[]
            {
                new EventOptionState("mark", "Mark warning", "A faction warning marker was added to the map.", EventOptionEffectKind.AddWarningMarker),
                new EventOptionState("archive", "Archive observation", $"The expedition recorded a suspected {faction.Name} warning zone.", EventOptionEffectKind.Archive),
                new EventOptionState("continue", "Continue carefully", "The expedition continues, aware that the crossing may be remembered.", EventOptionEffectKind.None)
            },
            coord);
    }
}
}
