#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class GetLocationInteractionCommand
{
    private readonly LocationInteractionService interactionService;

    public GetLocationInteractionCommand(LocationInteractionService interactionService)
    {
        this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    }

    public LocationInteractionQueryResult Execute(GameState game, string locationId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var location = FindLocation(game, locationId);
        if (location == null)
        {
            return LocationInteractionQueryResult.Rejected("Location was not found.");
        }

        if (!IsKnownEnough(game, location))
        {
            return LocationInteractionQueryResult.Rejected("This location is not confirmed knowledge yet.");
        }

        return LocationInteractionQueryResult.Found(interactionService.BuildInteraction(location, game.Expedition));
    }

    private static bool IsKnownEnough(GameState game, SpecialLocationState location)
    {
        return location.Anchor.Coords.Any(coord => game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Confirmed);
    }

    private static SpecialLocationState? FindLocation(GameState game, string locationId)
    {
        foreach (var location in game.World.Locations)
        {
            if (location.Id == locationId)
            {
                return location;
            }
        }

        return null;
    }
}

public sealed class ResolveLocationActionCommand
{
    private readonly LocationInteractionService interactionService;

    public ResolveLocationActionCommand(LocationInteractionService interactionService)
    {
        this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    }

    public LocationActionResult Execute(GameState game, string locationId, string actionId, string? forcedOutcomeId = null)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var location = FindLocation(game, locationId);
        if (location == null)
        {
            return LocationActionResult.Rejected("Location was not found.");
        }

        var interaction = interactionService.BuildInteraction(location, game.Expedition);
        var option = interaction.FindOption(actionId);
        if (option == null)
        {
            return LocationActionResult.Rejected("This action is not available for this location.");
        }

        if (!option.IsAvailable)
        {
            return LocationActionResult.Rejected(option.LockedReason ?? "This action is locked.");
        }

        if (option.Action.StartsProject)
        {
            location.StartProject(option.Action.Id, option.Action.ProjectDurationDays);
            game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: project started at {location.Name}: {option.Action.Label}.");
            return LocationActionResult.Resolved(location, option.Action, null, new[] { $"Project started: {option.Action.Label}." });
        }

        var outcome = SelectOutcome(option.Action, forcedOutcomeId);
        if (outcome == null)
        {
            return LocationActionResult.Rejected("This action has no outcome table yet.");
        }

        var texts = ApplyEffects(game, location, outcome.Effects);
        return LocationActionResult.Resolved(location, option.Action, outcome, texts);
    }

    internal static IReadOnlyList<string> ApplyEffects(GameState game, SpecialLocationState location, IReadOnlyList<LocationEffectDefinition> effects)
    {
        var texts = new List<string>();
        foreach (var effect in effects)
        {
            ApplyEffect(game, location, effect);
            texts.Add(effect.Text);
        }

        return texts;
    }

    private static LocationOutcomeDefinition? SelectOutcome(LocationActionDefinition action, string? forcedOutcomeId)
    {
        if (!string.IsNullOrWhiteSpace(forcedOutcomeId))
        {
            foreach (var outcome in action.Outcomes)
            {
                if (outcome.Id == forcedOutcomeId)
                {
                    return outcome;
                }
            }
        }

        return action.Outcomes.Count == 0 ? null : action.Outcomes[0];
    }

    private static void ApplyEffect(GameState game, SpecialLocationState location, LocationEffectDefinition effect)
    {
        switch (effect.Kind)
        {
            case LocationEffectKind.ChangeLocationState:
                if (effect.StateChannel == null || effect.StateId == null)
                {
                    throw new InvalidOperationException("ChangeLocationState effect needs a state channel and state id.");
                }

                location.SetState(effect.StateChannel, effect.StateId);
                break;
            case LocationEffectKind.AddUnsecuredKnowledge:
                game.Expedition.AddUnsecuredKnowledge(Math.Max(0, effect.Amount));
                break;
            case LocationEffectKind.ConsumeSupplies:
                game.Expedition.ConsumeSupplies(Math.Max(0, effect.Amount));
                break;
            case LocationEffectKind.ChangeMorale:
                game.Expedition.AdjustMorale(effect.Amount);
                break;
            case LocationEffectKind.AddFactionMemory:
                if (effect.FactionId != null && effect.Memory != null)
                {
                    var faction = game.FindFaction(effect.FactionId);
                    faction?.AddMemory(effect.Memory);
                }

                break;
            case LocationEffectKind.AddArchiveEntry:
                game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: {effect.Text}");
                break;
            case LocationEffectKind.OpenRoute:
                OpenRoute(game, location);
                break;
            default:
                throw new InvalidOperationException($"Unsupported location effect kind {effect.Kind}.");
        }
    }

    private static void OpenRoute(GameState game, SpecialLocationState location)
    {
        if (location.Anchor.Kind != LocationAnchorKind.Edge || location.Anchor.Coords.Count != 2)
        {
            return;
        }

        game.World.AddPath(new WorldPathState(
            $"route-opened-{location.Id}",
            WorldPathKind.Road,
            location.Anchor.Coords));
    }

    private static SpecialLocationState? FindLocation(GameState game, string locationId)
    {
        foreach (var location in game.World.Locations)
        {
            if (location.Id == locationId)
            {
                return location;
            }
        }

        return null;
    }
}

public sealed class AdvanceLocationProjectCommand
{
    private readonly LocationInteractionDefinitionSet definitions;

    public AdvanceLocationProjectCommand(LocationInteractionDefinitionSet definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    public LocationActionResult Execute(GameState game, string locationId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var location = game.World.Locations.FirstOrDefault(item => item.Id == locationId);
        if (location == null)
        {
            return LocationActionResult.Rejected("Location was not found.");
        }

        if (location.ActiveProject == null)
        {
            return LocationActionResult.Rejected("No project is active at this location.");
        }

        if (!definitions.Actions.TryGetValue(location.ActiveProject.ActionId, out var action))
        {
            return LocationActionResult.Rejected("The active project action is not registered.");
        }

        location.AdvanceProject();
        if (!location.ActiveProject.IsComplete)
        {
            return LocationActionResult.Resolved(location, action, null, new[] { $"Project progress: {location.ActiveProject.Progress}/{location.ActiveProject.RequiredProgress}." });
        }

        var texts = ResolveLocationActionCommand.ApplyEffects(game, location, action.ProjectCompletionEffects);
        location.ClearProject();
        return LocationActionResult.Resolved(location, action, null, texts);
    }
}
}
