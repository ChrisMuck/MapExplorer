#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class EndDayCommand
{
    private readonly int suppliesPerDay;
    private readonly ScoutMissionResolutionService scoutMissionResolutionService;
    private readonly FailExpeditionCommand failExpeditionCommand = new FailExpeditionCommand();
    private readonly WorldPhaseService worldPhaseService;

    public EndDayCommand(
        int suppliesPerDay = 2,
        ScoutMissionResolutionService? scoutMissionResolutionService = null,
        WorldPhaseService? worldPhaseService = null)
    {
        if (suppliesPerDay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(suppliesPerDay), suppliesPerDay, "Supply consumption must not be negative.");
        }

        this.suppliesPerDay = suppliesPerDay;
        this.scoutMissionResolutionService = scoutMissionResolutionService ?? new ScoutMissionResolutionService();
        this.worldPhaseService = worldPhaseService ?? new WorldPhaseService();
    }

    public EndDayResult Execute(GameState game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return EndDayResult.Rejected("Expedition is not active.");
        }

        var isAtBase = game.Expedition.Position == game.Base.Location;
        var consumed = isAtBase ? 0 : Math.Min(game.Expedition.Supplies, suppliesPerDay);
        if (!isAtBase)
        {
            game.Expedition.ConsumeSupplies(suppliesPerDay);
        }

        game.Expedition.AdvanceExpeditionDay();
        game.World.AdvanceDays(1);
        worldPhaseService.Resolve(game);
        if (!isAtBase && suppliesPerDay > 0 && game.Expedition.Supplies == 0)
        {
            failExpeditionCommand.Execute(game, "Supplies exhausted before the expedition could return.");
            return EndDayResult.Advanced(game.World.WorldDay, game.Expedition.ExpeditionDay, consumed, expeditionLost: true);
        }

        var scoutResolutions = scoutMissionResolutionService.ResolveDueMissions(game);
        foreach (var resolution in scoutResolutions)
        {
            game.Events.Enqueue(CreateScoutEvent(game, resolution));
        }

        return EndDayResult.Advanced(game.World.WorldDay, game.Expedition.ExpeditionDay, consumed, scoutResolutions);
    }

    private static EventState CreateScoutEvent(GameState game, ScoutMissionResolutionResult resolution)
    {
        var eventId = $"event-{game.Events.Events.Count + 1}";
        var coord = resolution.Report != null && resolution.Report.RelatedCoords.Count > 0
            ? resolution.Report.RelatedCoords[0]
            : (HexCoord?)null;

        switch (resolution.Status)
        {
            case ScoutMissionStatus.Returned:
                return new EventState(
                    eventId,
                    EventKind.ScoutReport,
                    "Scout Report Returned",
                    "Scouts",
                    resolution.Report != null ? resolution.Report.Body : "A scout returned with a report.",
                    new[]
                    {
                        new EventOptionState("read", "Read report", "The scout report is ready in the reports panel.", EventOptionEffectKind.None),
                        new EventOptionState("archive", "Archive summary", "A scout report summary was archived.", EventOptionEffectKind.Archive)
                    },
                    coord);
            case ScoutMissionStatus.ReturnedInjured:
                return new EventState(
                    eventId,
                    EventKind.ScoutReport,
                    "Scout Returned Injured",
                    "Scouts",
                    resolution.Report != null ? resolution.Report.Body : "A scout returned injured.",
                    new[]
                    {
                        new EventOptionState("treat", "Treat and read report", "The injured scout's report was reviewed.", EventOptionEffectKind.None),
                        new EventOptionState("archive", "Archive injury note", "An injury note from the scout mission was archived.", EventOptionEffectKind.Archive)
                    },
                    coord);
            case ScoutMissionStatus.Overdue:
                return new EventState(
                    eventId,
                    EventKind.ScoutOverdue,
                    "Scout Overdue",
                    "Scouts",
                    "A scout has missed the expected return day. The route may be harder or more dangerous than reported.",
                    new[]
                    {
                        new EventOptionState("wait", "Wait one more day", "The expedition will wait and watch for the overdue scout.", EventOptionEffectKind.None),
                        new EventOptionState("archive", "Record risk", "The overdue scout was recorded as a route risk.", EventOptionEffectKind.Archive)
                    });
            case ScoutMissionStatus.Missing:
                return new EventState(
                    eventId,
                    EventKind.ScoutOverdue,
                    "Scout Missing",
                    "Scouts",
                    "A scout did not return. Their last route is now a dangerous unknown.",
                    new[]
                    {
                        new EventOptionState("archive", "Archive loss", "A missing scout has been added to the expedition archive.", EventOptionEffectKind.Archive),
                        new EventOptionState("acknowledge", "Acknowledge", "The expedition acknowledges the loss and continues.", EventOptionEffectKind.None)
                    });
            default:
                return new EventState(
                    eventId,
                    EventKind.ScoutReport,
                    "Scout Update",
                    "Scouts",
                    "The scout mission status changed.",
                    new[]
                    {
                        new EventOptionState("acknowledge", "Acknowledge", "The scout update was acknowledged.", EventOptionEffectKind.None)
                    },
                    coord);
        }
    }
}
}
