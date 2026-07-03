using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public sealed class GameApplication
{
    public HexMapBounds DefaultPrototypeBounds { get; } = new(40, 30);
    private readonly MovementCostService movementCostService = new MovementCostService();

    public GameState CreateTutorialGame()
    {
        return TutorialGameFactory.Create();
    }

    public MoveExpeditionResult MoveExpedition(GameState game, HexCoord destination)
    {
        return new MoveExpeditionCommand(movementCostService).Execute(game, destination);
    }
}
}
