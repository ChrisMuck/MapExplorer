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

        LocationOutcomeResolution? resolution = null;
        if (!option.Action.StartsProject)
        {
            resolution = interactionService.ResolveOutcome(option.Action, option.RiskBand, forcedTier);
            if (resolution == null)
            {
                return LocationActionResult.Rejected("This action has no outcome table yet.");
            }
        }

        var costError = FirstUnpayableCost(option.Action, game.Expedition);
        if (costError != null)
        {
            return LocationActionResult.Rejected(costError);
        }

        var costTexts = SpendCosts(game.Expedition, option.Action);

        if (option.Action.StartsProject)
        {
            location.StartProject(option.Action.Id, option.Action.ProjectDurationDays);
            location.MarkActionResolved(repeatKey);
            game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: project started at {location.Name}: {option.Action.Label}.");
            var projectTexts = new List<string>(costTexts) { $"Project started: {option.Action.Label}." };
            return LocationActionResult.Resolved(location, option.Action, null, null, projectTexts);
        }

        // Recovery Check (§9.9): a member-ending effect gets a softer roll before it is applied.
        LocationRecoveryOutcome? recovery = null;
        if (resolution == null)
        {
            return LocationActionResult.Rejected("This action has no outcome table yet.");
        }

        if (resolution.Effects.Any(effect => IsMemberEndingEffect(effect.Kind)))
        {
            recovery = interactionService.RollRecovery(HasMedic(game.Expedition), forcedRecovery);
        }

        var triggerCountBeforeEffects = game.World.WorldTriggers.Count;
        var effectTexts = ApplyEffects(game, location, resolution.Effects, recovery, out var expeditionMoved, option.Action.ActionTags);
        QueueGenericActionTriggerIfNeeded(game, location, option.Action, triggerCountBeforeEffects);
        var texts = new List<string>(costTexts);
        texts.AddRange(effectTexts);
        location.MarkActionResolved(repeatKey);
        return LocationActionResult.Resolved(location, option.Action, resolution.Tier, resolution.Label, texts, expeditionMoved);
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

    private static string? FirstUnpayableCost(LocationActionDefinition action, ExpeditionState expedition)
    {
        foreach (var cost in action.Costs)
        {
            if (cost.Amount <= 0)
            {
                continue;
            }

            switch (cost.Kind)
            {
                case LocationCostKind.MovementPoints:
                    if (expedition.MovementPoints < cost.Amount)
                    {
                        return "Not enough movement points.";
                    }

                    break;
                case LocationCostKind.Supplies:
                    if (expedition.Supplies < cost.Amount)
                    {
                        return "Not enough supplies.";
                    }

                    break;
                case LocationCostKind.Medicine:
                    if (expedition.Medicine < cost.Amount)
                    {
                        return "Not enough medicine.";
                    }

                    break;
                case LocationCostKind.Morale:
                    if (expedition.Morale < cost.Amount)
                    {
                        return "Not enough morale.";
                    }

                    break;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> SpendCosts(ExpeditionState expedition, LocationActionDefinition action)
    {
        var texts = new List<string>();
        foreach (var cost in action.Costs)
        {
            if (cost.Amount <= 0)
            {
                continue;
            }

            switch (cost.Kind)
            {
                case LocationCostKind.MovementPoints:
                    expedition.SpendMovementPoints(cost.Amount);
                    texts.Add(CostText(cost.Amount, "Bewegungspunkt", "Bewegungspunkte"));
                    break;
                case LocationCostKind.Supplies:
                    expedition.ConsumeSupplies(cost.Amount);
                    texts.Add(CostText(cost.Amount, "Vorrat", "Vorraete"));
                    break;
                case LocationCostKind.Medicine:
                    expedition.ConsumeMedicine(cost.Amount);
                    texts.Add(CostText(cost.Amount, "Medizin", "Medizin"));
                    break;
                case LocationCostKind.Morale:
                    expedition.AdjustMorale(-cost.Amount);
                    texts.Add(CostText(cost.Amount, "Moral", "Moral"));
                    break;
            }
        }

        return texts;
    }

    private static string CostText(int amount, string singular, string plural)
    {
        return amount == 1
            ? $"Kosten bezahlt: 1 {singular}."
            : $"Kosten bezahlt: {amount} {plural}.";
    }

    internal static void QueueGenericActionTriggerIfNeeded(
        GameState game,
        SpecialLocationState location,
        LocationActionDefinition action,
        int triggerCountBeforeAction)
    {
        if (action.ActionTags.Count == 0 || game.World.WorldTriggers.Count > triggerCountBeforeAction)
        {
            return;
        }

        game.World.QueueWorldTrigger(new WorldTriggerState(
            $"world-trigger-{game.World.WorldTriggers.Count + 1}",
            FactionTerritorialPolicyResolver.LocationActionCompletedTriggerId,
            game.World.WorldDay,
            action.ActionTags,
            location.Id,
            location.Coord));
    }

    internal static IReadOnlyList<string> ApplyEffects(
        GameState game,
        SpecialLocationState location,
        IReadOnlyList<LocationEffectDefinition> effects,
        LocationRecoveryOutcome? recovery,
        out bool expeditionMoved,
        IReadOnlyList<string>? actionTags = null)
    {
        var texts = new List<string>();
        expeditionMoved = false;
        foreach (var effect in effects)
        {
            if (IsMemberEndingEffect(effect.Kind) && recovery == LocationRecoveryOutcome.Preserved)
            {
                texts.Add("Ein Ungluecklicher konnte im letzten Moment gerettet werden.");
                continue;
            }

            expeditionMoved |= ApplyEffect(game, location, effect, actionTags);
            texts.Add(effect.Text);
        }

        return texts;
    }

    private static bool ApplyEffect(
        GameState game,
        SpecialLocationState location,
        LocationEffectDefinition effect,
        IReadOnlyList<string>? actionTags)
    {
        switch (effect.Kind)
        {
            case LocationEffectKind.ChangeLocationState:
                if (effect.StateChannel == null || effect.StateId == null)
                {
                    throw new InvalidOperationException("ChangeLocationState effect needs a state channel and state id.");
                }

                location.SetState(effect.StateChannel, effect.StateId);
                return false;
            case LocationEffectKind.AddUnsecuredKnowledge:
                game.Expedition.AddUnsecuredKnowledge(Math.Max(0, effect.Amount));
                return false;
            case LocationEffectKind.ConsumeSupplies:
                game.Expedition.ConsumeSupplies(Math.Max(0, effect.Amount));
                return false;
            case LocationEffectKind.ChangeMorale:
                game.Expedition.AdjustMorale(effect.Amount);
                return false;
            case LocationEffectKind.AddFactionMemory:
                if (effect.FactionId != null && effect.Memory != null)
                {
                    var faction = game.FindFaction(effect.FactionId);
                    faction?.AddMemory(effect.Memory);
                }

                return false;
            case LocationEffectKind.AddArchiveEntry:
                game.Base.AddArchiveEntry($"Day {game.World.WorldDay}: {effect.Text}");
                return false;
            case LocationEffectKind.OpenRoute:
                OpenRoute(game, location);
                return false;
            case LocationEffectKind.MoveExpeditionAcrossEdge:
                return MoveExpeditionAcrossEdge(game, location);
            case LocationEffectKind.InjureMember:
                InjureMember(game);
                return false;
            case LocationEffectKind.ChangeFactionTrust:
                game.FindFaction(effect.FactionId ?? "")?.Adjust(trustDelta: effect.Amount);
                return false;
            case LocationEffectKind.ChangeFactionAnger:
                game.FindFaction(effect.FactionId ?? "")?.Adjust(angerDelta: effect.Amount);
                return false;
            case LocationEffectKind.ChangeFactionFear:
                game.FindFaction(effect.FactionId ?? "")?.Adjust(fearDelta: effect.Amount);
                return false;
            case LocationEffectKind.AddEvidence:
                game.Knowledge.AddEvidence(new EvidenceState(
                    $"evidence-{game.Knowledge.Evidence.Count + 1}",
                    effect.ReferenceId ?? effect.Id,
                    EvidenceSourceKind.LocationInspection,
                    EvidenceKnowledgeState.Reported,
                    effect.Text,
                    subjectLocationId: location.Id));
                return false;
            case LocationEffectKind.RaiseWorldTrigger:
                game.World.QueueWorldTrigger(new WorldTriggerState(
                    $"world-trigger-{game.World.WorldTriggers.Count + 1}",
                    effect.ReferenceId ?? effect.Id,
                    game.World.WorldDay,
                    actionTags: actionTags,
                    sourceLocationId: location.Id,
                    sourceCoord: location.Coord));
                return false;
            case LocationEffectKind.ScheduleConsequence:
                var delayDays = Math.Max(0, effect.DelayDays);
                game.World.ScheduleConsequence(new ScheduledConsequenceState(
                    $"scheduled-consequence-{game.World.ScheduledConsequences.Count + 1}",
                    effect.ReferenceId ?? effect.Id,
                    location.Id,
                    game.World.WorldDay + delayDays,
                    new[] { effect.Id }));
                return false;
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

    private static bool MoveExpeditionAcrossEdge(GameState game, SpecialLocationState location)
    {
        if (location.Anchor.Kind != LocationAnchorKind.Edge || location.Anchor.Coords.Count != 2)
        {
            return false;
        }

        var from = game.Expedition.Position;
        var a = location.Anchor.Coords[0];
        var b = location.Anchor.Coords[1];
        HexCoord destination;
        if (from == a)
        {
            destination = b;
        }
        else if (from == b)
        {
            destination = a;
        }
        else
        {
            return false;
        }

        if (!game.World.Map.TryGetTile(destination, out var destinationTile) || destinationTile == null)
        {
            return false;
        }

        var destinationWasConfirmed = game.Knowledge.GetTileKnowledge(destination) == KnowledgeLevel.Confirmed;
        game.Expedition.SetPosition(destination);
        new KnowledgeService().RevealFromExpedition(game.World.Map, game.Knowledge, destination);
        if (!destinationWasConfirmed && game.Knowledge.ClaimKnowledgeSource($"confirmed-hex:{destination.Q}:{destination.R}"))
        {
            game.Expedition.AddUnsecuredKnowledge(1);
        }

        return true;
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

        var triggerCountBeforeEffects = game.World.WorldTriggers.Count;
        var texts = ResolveLocationActionCommand.ApplyEffects(game, location, action.ProjectCompletionEffects, null, out var expeditionMoved, action.ActionTags);
        ResolveLocationActionCommand.QueueGenericActionTriggerIfNeeded(game, location, action, triggerCountBeforeEffects);
        location.ClearProject();
        return LocationActionResult.Resolved(location, action, null, null, texts, expeditionMoved);
    }
}
}
