#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class ResolveEventCommand
{
    private readonly AddMapMarkerCommand addMapMarkerCommand;
    private readonly OpenFactionInteractionCommand openFactionInteractionCommand;

    public ResolveEventCommand()
        : this(new AddMapMarkerCommand(), new OpenFactionInteractionCommand())
    {
    }

    public ResolveEventCommand(AddMapMarkerCommand addMapMarkerCommand, OpenFactionInteractionCommand openFactionInteractionCommand)
    {
        this.addMapMarkerCommand = addMapMarkerCommand ?? throw new ArgumentNullException(nameof(addMapMarkerCommand));
        this.openFactionInteractionCommand = openFactionInteractionCommand ?? throw new ArgumentNullException(nameof(openFactionInteractionCommand));
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
            case EventOptionEffectKind.OpenFactionInteraction:
                if (string.IsNullOrWhiteSpace(eventState.FactionId) || !eventState.Coord.HasValue)
                {
                    return "No faction contact is available here.";
                }

                var interactionResult = openFactionInteractionCommand.Execute(game, eventState.FactionId, eventState.Coord.Value);
                return interactionResult.Success
                    ? interactionResult.Message ?? option.ResultText
                    : interactionResult.Error ?? "Faction contact could not be opened.";
            default:
                return option.ResultText;
        }
    }
}
}
