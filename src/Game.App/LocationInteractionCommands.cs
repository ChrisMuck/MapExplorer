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

        return LocationInteractionQueryResult.Found(
            interactionService.BuildInteraction(location, game.Expedition, LocationInteractionSupport.LinkedFactions(game, location)));
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

internal static class LocationInteractionSupport
{
    /// <summary>Resolves the faction states a location is linked to, for social risk (§9.4B).</summary>
    public static IReadOnlyList<FactionState> LinkedFactions(GameState game, SpecialLocationState location)
    {
        var factions = new List<FactionState>();
        foreach (var factionId in location.FactionIds)
        {
            var faction = game.FindFaction(factionId);
            if (faction != null)
            {
                factions.Add(faction);
            }
        }

        return factions;
    }
}

public sealed class ResolveLocationActionCommand
{
    private readonly LocationInteractionService interactionService;

    public ResolveLocationActionCommand(LocationInteractionService interactionService)
    {
        this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    }

    public LocationActionResult Execute(
        GameState game,
        string locationId,
        string actionId,
        LocationOutcomeTier? forcedTier = null,
        LocationRecoveryOutcome? forcedRecovery = null)
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

        var interaction = interactionService.BuildInteraction(location, game.Expedition, LocationInteractionSupport.LinkedFactions(game, location));
        var option = interaction.FindOption(actionId);
        if (option == null)
        {
            return LocationActionResult.Rejected("This action is not available for this location.");
        }

        if (!option.IsAvailable)
        {
            return LocationActionResult.Rejected(option.LockedReason ?? "This action is locked.");
        }

        // Consumed-action key must reflect the state the action ran in, before any effect changes it (§7.6).
        var repeatKey = LocationInteractionService.RepeatKey(option.Action, location);

        if (option.Action.StartsProject)
        {
            location.StartProject(option.Action.Id, option.Action.ProjectDurationDays);
            location.MarkActionResolved(repeatKey);
            game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: project started at {location.Name}: {option.Action.Label}.");
            return LocationActionResult.Resolved(location, option.Action, null, null, new[] { $"Project started: {option.Action.Label}." });
        }

        var resolution = interactionService.ResolveOutcome(option.Action, option.RiskBand, forcedTier);
        if (resolution == null)
        {
            return LocationActionResult.Rejected("This action has no outcome table yet.");
        }

        // Recovery Check (§9.9): a member-ending effect gets a softer roll before it is applied.
        LocationRecoveryOutcome? recovery = null;
        if (resolution.Effects.Any(effect => IsMemberEndingEffect(effect.Kind)))
        {
            recovery = interactionService.RollRecovery(HasMedic(game.Expedition), forcedRecovery);
        }

        var texts = ApplyEffects(game, location, resolution.Effects, recovery);
        location.MarkActionResolved(repeatKey);
        return LocationActionResult.Resolved(location, option.Action, resolution.Tier, resolution.Label, texts);
    }

    private static bool IsMemberEndingEffect(LocationEffectKind kind)
    {
        return kind == LocationEffectKind.InjureMember;
    }

    private static bool HasMedic(ExpeditionState expedition)
    {
        return expedition.Members.Any(member =>
            member.Role == ExpeditionMemberRole.Medic &&
            member.Status != ExpeditionMemberStatus.Missing &&
            member.Status != ExpeditionMemberStatus.Dead);
    }

    internal static IReadOnlyList<string> ApplyEffects(
        GameState game,
        SpecialLocationState location,
        IReadOnlyList<LocationEffectDefinition> effects,
        LocationRecoveryOutcome? recovery = null)
    {
        var texts = new List<string>();
        foreach (var effect in effects)
        {
            if (IsMemberEndingEffect(effect.Kind) && recovery == LocationRecoveryOutcome.Preserved)
            {
                texts.Add("Ein Ungluecklicher konnte im letzten Moment gerettet werden.");
                continue;
            }

            ApplyEffect(game, location, effect);
            texts.Add(effect.Text);
        }

        return texts;
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
            case LocationEffectKind.InjureMember:
                InjureMember(game);
                break;
            case LocationEffectKind.ChangeFactionTrust:
                game.FindFaction(effect.FactionId ?? "")?.Adjust(trustDelta: effect.Amount);
                break;
            case LocationEffectKind.ChangeFactionAnger:
                game.FindFaction(effect.FactionId ?? "")?.Adjust(angerDelta: effect.Amount);
                break;
            case LocationEffectKind.ChangeFactionFear:
                game.FindFaction(effect.FactionId ?? "")?.Adjust(fearDelta: effect.Amount);
                break;
            default:
                throw new InvalidOperationException($"Unsupported location effect kind {effect.Kind}.");
        }
    }

    private static void InjureMember(GameState game)
    {
        // MVP: injure the first still-active member. "selection" is honored loosely for now.
        foreach (var member in game.Expedition.Members)
        {
            if (member.Status != ExpeditionMemberStatus.Injured &&
                member.Status != ExpeditionMemberStatus.Missing &&
                member.Status != ExpeditionMemberStatus.Dead)
            {
                member.SetStatus(ExpeditionMemberStatus.Injured);
                return;
            }
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
            return LocationActionResult.Resolved(location, action, null, null, new[] { $"Project progress: {location.ActiveProject.Progress}/{location.ActiveProject.RequiredProgress}." });
        }

        var texts = ResolveLocationActionCommand.ApplyEffects(game, location, action.ProjectCompletionEffects);
        location.ClearProject();
        return LocationActionResult.Resolved(location, action, null, null, texts);
    }
}
}
