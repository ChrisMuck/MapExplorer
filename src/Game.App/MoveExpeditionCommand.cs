using System;
using Game.Core;

namespace Game.App
{

public sealed class MoveExpeditionCommand
{
    private readonly MovementCostService movementCostService;

    public MoveExpeditionCommand(MovementCostService movementCostService)
    {
        this.movementCostService = movementCostService ?? throw new ArgumentNullException(nameof(movementCostService));
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
        game.Knowledge.SetTileKnowledge(destination, KnowledgeLevel.Confirmed);

        foreach (var neighbor in destination.Neighbors())
        {
            if (game.World.Map.Contains(neighbor) && game.Knowledge.GetTileKnowledge(neighbor) == KnowledgeLevel.Unknown)
            {
                game.Knowledge.SetTileKnowledge(neighbor, KnowledgeLevel.Reported);
            }
        }

        return MoveExpeditionResult.Moved(from, destination, cost.Cost);
    }
}
}
