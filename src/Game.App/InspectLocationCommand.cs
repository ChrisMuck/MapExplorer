#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class InspectLocationCommand
{
    public InspectLocationResult Execute(GameState game, HexCoord coord)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Knowledge.GetTileKnowledge(coord) != KnowledgeLevel.Confirmed)
        {
            return InspectLocationResult.Rejected("Only confirmed locations can be inspected.");
        }

        var location = FindLocation(game, coord);
        if (location == null)
        {
            return InspectLocationResult.Rejected("There is no special location on this field.");
        }

        var wasInspected = location.IsInspected;
        location.Inspect(game.World.WorldDay);
        var message = BuildMessage(game, location);
        string? archiveEntry = null;
        if (!wasInspected)
        {
            archiveEntry = $"Day {game.World.WorldDay}: {location.Name} inspected. {message}";
            game.Base.AddArchiveEntry(archiveEntry);
            AddLocationLeverage(game, location);
            game.Events.Enqueue(CreateLocationEvent(game, location, message));
            if (location.Kind != LocationKind.BaseCamp)
            {
                game.Expedition.AddUnsecuredKnowledge(8);
            }
        }

        return InspectLocationResult.Inspected(location, message, archiveEntry);
    }

    private static void AddLocationLeverage(GameState game, SpecialLocationState location)
    {
        foreach (var leverage in FactionInteractionDefinitions.LeverageForLocation(location))
        {
            if (game.LeverageItems.Add(leverage.ItemId))
            {
                game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: recovered {leverage.DisplayName} as negotiation leverage.");
            }
        }
    }

    private static SpecialLocationState? FindLocation(GameState game, HexCoord coord)
    {
        foreach (var location in game.World.Locations)
        {
            if (location.Coord == coord)
            {
                return location;
            }
        }

        return null;
    }

    private static string BuildMessage(GameState game, SpecialLocationState location)
    {
        switch (location.Kind)
        {
            case LocationKind.BrokenRavine:
                return HasEngineer(game)
                    ? "An engineer can plan a safe crossing, but the route still needs supplies and time."
                    : "The ravine is unstable. Without an engineer, crossing it safely is not possible yet.";
            case LocationKind.AbandonedCamp:
                return "Old tracks and weather-damaged notes suggest another expedition passed here before.";
            case LocationKind.MarkedGrave:
                return "The grave marker is deliberate and recent enough to be a warning, not only a memorial.";
            case LocationKind.Mine:
                return "The mine looks abandoned, but fresh tool marks suggest it may still matter.";
            case LocationKind.Watchtower:
                return "The watchtower gives a useful view line and hints at guarded territory nearby.";
            case LocationKind.Settlement:
                return "The settlement can become a contact point once trust and language are established.";
            case LocationKind.BaseCamp:
                return "The base camp is secure and can receive archived expedition knowledge.";
            default:
                return "The place is now recorded in the expedition archive.";
        }
    }

    private static EventState CreateLocationEvent(GameState game, SpecialLocationState location, string message)
    {
        var eventId = $"event-{game.Events.Events.Count + 1}";
        switch (location.Kind)
        {
            case LocationKind.AbandonedCamp:
                return new EventState(
                    eventId,
                    EventKind.LocationDiscovery,
                    "Abandoned Camp",
                    "Expedition",
                    message,
                    new[]
                    {
                        new EventOptionState("archive", "Archive the traces", "The abandoned camp has been archived as old expedition evidence.", EventOptionEffectKind.Archive),
                        new EventOptionState("mark", "Mark as warning", "A warning marker was placed at the abandoned camp.", EventOptionEffectKind.AddWarningMarker)
                    },
                    location.Coord);
            case LocationKind.MarkedGrave:
                return new EventState(
                    eventId,
                    EventKind.WarningSign,
                    "Marked Grave",
                    "Expedition",
                    message,
                    new[]
                    {
                        new EventOptionState("archive", "Record the warning", "The marked grave has been archived as a possible territorial warning.", EventOptionEffectKind.Archive),
                        new EventOptionState("mark", "Mark danger", "A warning marker was placed at the marked grave.", EventOptionEffectKind.AddWarningMarker)
                    },
                    location.Coord);
            case LocationKind.BrokenRavine:
                return new EventState(
                    eventId,
                    EventKind.LocationDiscovery,
                    "Broken Ravine",
                    "Expedition",
                    message,
                    new[]
                    {
                        new EventOptionState("archive", "Add to archive", "The broken ravine has been archived as a route obstacle.", EventOptionEffectKind.Archive),
                        new EventOptionState("defer", "Leave for later", "The ravine remains noted but unresolved.", EventOptionEffectKind.None)
                    },
                    location.Coord);
            default:
                return new EventState(
                    eventId,
                    EventKind.LocationDiscovery,
                    location.Name,
                    "Expedition",
                    message,
                    new[]
                    {
                        new EventOptionState("acknowledge", "Acknowledge", "The discovery was acknowledged.", EventOptionEffectKind.None)
                    },
                    location.Coord);
        }
    }

    private static bool HasEngineer(GameState game)
    {
        foreach (var member in game.Expedition.Members)
        {
            if (member.Role == ExpeditionMemberRole.Engineer && member.Status != ExpeditionMemberStatus.Missing)
            {
                return true;
            }
        }

        return false;
    }
}
}
