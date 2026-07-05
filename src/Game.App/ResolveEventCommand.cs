#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class ResolveEventCommand
{
    private readonly AddMapMarkerCommand addMapMarkerCommand;

    public ResolveEventCommand()
        : this(new AddMapMarkerCommand())
    {
    }

    public ResolveEventCommand(AddMapMarkerCommand addMapMarkerCommand)
    {
        this.addMapMarkerCommand = addMapMarkerCommand ?? throw new ArgumentNullException(nameof(addMapMarkerCommand));
    }

    public ResolveEventResult Execute(GameState game, string eventId, string optionId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var eventState = game.Events.Find(eventId);
        if (eventState == null)
        {
            return ResolveEventResult.Rejected("Unknown event.");
        }

        if (eventState.IsResolved)
        {
            return ResolveEventResult.Rejected("Event is already resolved.");
        }

        var option = eventState.FindOption(optionId);
        if (option == null)
        {
            return ResolveEventResult.Rejected("Unknown event option.");
        }

        var effectMessage = ApplyEffect(game, eventState, option);
        eventState.Resolve(option.Id);
        return ResolveEventResult.Resolved(eventState, option, effectMessage);
    }

    private string ApplyEffect(GameState game, EventState eventState, EventOptionState option)
    {
        switch (option.EffectKind)
        {
            case EventOptionEffectKind.Archive:
                game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: {option.ResultText}");
                return option.ResultText;
            case EventOptionEffectKind.AddWarningMarker:
                if (eventState.Coord.HasValue)
                {
                    var result = addMapMarkerCommand.Execute(
                        game,
                        eventState.Coord.Value,
                        PlayerMapMarkerKind.Warning,
                        eventState.Title);
                    if (!result.Success)
                    {
                        return result.Error ?? "Marker could not be added.";
                    }
                }

                return option.ResultText;
            default:
                return option.ResultText;
        }
    }
}
}
