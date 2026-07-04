using System;
using Game.Core;

namespace Game.App
{

public sealed class MoveExpeditionCommand
{
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

        game.Expedition.SpendMovementPoints(cost.Cost);
        game.Expedition.SetPosition(destination);
        knowledgeService.RevealFromExpedition(game.World.Map, game.Knowledge, destination);

        return MoveExpeditionResult.Moved(from, destination, cost.Cost);
    }
}
}
