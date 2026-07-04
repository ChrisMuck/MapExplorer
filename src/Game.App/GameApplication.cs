#nullable enable
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public sealed class GameApplication
{
    public HexMapBounds DefaultPrototypeBounds { get; } = new(40, 30);
    private readonly MovementCostService movementCostService = new MovementCostService();
    private readonly EndDayCommand endDayCommand = new EndDayCommand();
    private readonly AddMapMarkerCommand addMapMarkerCommand = new AddMapMarkerCommand();
    private readonly AddMapNoteCommand addMapNoteCommand = new AddMapNoteCommand();
    private readonly SendScoutMissionCommand sendScoutMissionCommand = new SendScoutMissionCommand();

    public GameState CreateTutorialGame()
    {
        return TutorialGameFactory.Create();
    }

    public MoveExpeditionResult MoveExpedition(GameState game, HexCoord destination)
    {
        return new MoveExpeditionCommand(movementCostService).Execute(game, destination);
    }

    public EndDayResult EndDay(GameState game)
    {
        return endDayCommand.Execute(game);
    }

    public MapAnnotationResult AddMapMarker(GameState game, HexCoord coord, PlayerMapMarkerKind kind, string label, string? factionId = null)
    {
        return addMapMarkerCommand.Execute(game, coord, kind, label, factionId);
    }

    public MapAnnotationResult AddMapNote(GameState game, HexCoord coord, string text)
    {
        return addMapNoteCommand.Execute(game, coord, text);
    }

    public SendScoutMissionResult SendScoutMission(
        GameState game,
        IReadOnlyList<string> scoutMemberIds,
        HexDirection direction,
        int durationDays,
        ScoutMissionFocus focus,
        ScoutMissionBehavior behavior)
    {
        return sendScoutMissionCommand.Execute(game, scoutMemberIds, direction, durationDays, focus, behavior);
    }
}
}
