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
    private readonly LocationScenarioActionResolver? scenarioActionResolver;
    private readonly SceneDescriptionResolver? sceneResolver;

    public GetLocationInteractionCommand(LocationInteractionService interactionService,
        LocationScenarioActionResolver? scenarioActionResolver = null, SceneDescriptionCatalog? scenes = null)
    {
        this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
        this.scenarioActionResolver = scenarioActionResolver;
        sceneResolver = scenes == null ? null : new SceneDescriptionResolver(scenes);
    }

    public LocationInteractionQueryResult Execute(GameState game, string locationId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        return Execute(game, locationId, game.Expedition);
    }

    /// <summary>
    /// Read-only option query for an explicitly supplied expedition composition. Development
    /// planners use this to preview specialist gates without changing the active expedition.
    /// </summary>
    public LocationInteractionQueryResult Execute(GameState game, string locationId, ExpeditionState expedition)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }
        if (expedition == null)
        {
            throw new ArgumentNullException(nameof(expedition));
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

        scenarioActionResolver?.ValidateRuntimeState(location);

        var interaction = interactionService.BuildInteraction(
                location,
                expedition,
                LocationInteractionSupport.LinkedFactions(game, location),
                scenarioActionResolver?.ResolveBaseActionIds(location, game.Knowledge));
        return LocationInteractionQueryResult.Found(interaction, BuildPresentation(game, interaction));
    }

    private LocationInteractionPresentation BuildPresentation(GameState game, LocationInteractionModel interaction)
    {
        var location = interaction.Location;
        var profile = interaction.ContentProfile;
        var known = game.Knowledge.KnownLocationConditions.FirstOrDefault(item => item.LocationId == location.Id);
        var knowledgeLabel = known == null
            ? "Bestaetigt, Zustand noch nicht aufgenommen"
            : known.IsDoubtful ? $"Zweifelhaft, zuletzt beobachtet an Tag {known.ObservedWorldDay}" : $"Bestaetigt an Tag {known.ObservedWorldDay}";
        var interactionText = "Der Interaktionszustand wird in der Szenenbeschreibung zusammengefasst.";
        var operationalText = "Der bekannte Zustand wird in der Szenenbeschreibung zusammengefasst.";
        var presenceText = "Die bekannte Anwesenheit wird in der Szenenbeschreibung zusammengefasst.";
        var description = known == null
            ? profile?.ShortDescription ?? "Der Ort ist bestaetigt, wurde aber noch nicht aus der Naehe aufgenommen."
            : string.Join(" ", new[] { interactionText, operationalText, presenceText });
        SceneDescriptionResult? scene = null;
        if (sceneResolver != null && known != null && profile != null &&
            !string.IsNullOrWhiteSpace(location.ArchetypeId) && !string.IsNullOrWhiteSpace(location.VariantId))
        {
            var tags = game.Knowledge.KnownLocationContextTags(location.Id);
            var relationKinds = tags.Where(tag => tag.StartsWith("inspection-relation:", StringComparison.Ordinal))
                .Select(tag => tag["inspection-relation:".Length..]);
            var factionId = tags.Where(tag => tag.StartsWith("inspection-faction:", StringComparison.Ordinal))
                .Select(tag => tag["inspection-faction:".Length..]).OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();
            var faction = factionId == null ? null : game.FindFaction(factionId);
            var identified = faction != null && faction.ContactStatus is FactionContactStatus.Contacted or FactionContactStatus.Open or FactionContactStatus.Hostile;
            var view = new LocationSceneView(location.Id, SceneTrigger.RemoteLocationView, location.ArchetypeId!, location.VariantId!,
                profile.Title, profile.Subtitle, profile.ImageId, known.InteractionStateId, known.OperationalStateId,
                known.PresenceStateId, game.World.WorldDay, known, game.Knowledge.GetTileKnowledge(location.Coord), tags,
                null, relationKinds, identified ? "identified" : "anonymous",
                faction?.ContactStatus.ToString() ?? FactionContactStatus.Unknown.ToString(), identified ? faction!.Name : null,
                knowledgeLabel);
            scene = sceneResolver.ResolveLocation(view);
            description = scene.Message;
        }
        return new LocationInteractionPresentation(
            profile?.Title ?? location.Name,
            profile?.Subtitle ?? "Bestaetigter besonderer Ort",
            description,
            profile?.ImageId,
            knowledgeLabel,
            interactionText,
            operationalText,
            presenceText,
            game.Knowledge.KnownLocationContextTags(location.Id).OrderBy(item => item, StringComparer.Ordinal).ToList(), scene);
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
    private readonly LocationScenarioActionResolver? scenarioActionResolver;
    private readonly LocationFindingAcquisitionService? findingAcquisitionService;

    public ResolveLocationActionCommand(
        LocationInteractionService interactionService,
        LocationScenarioActionResolver? scenarioActionResolver = null,
        LocationFindingAcquisitionService? findingAcquisitionService = null)
    {
        this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
        this.scenarioActionResolver = scenarioActionResolver;
        this.findingAcquisitionService = findingAcquisitionService;
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

        scenarioActionResolver?.ValidateRuntimeState(location);

        var interaction = interactionService.BuildInteraction(
            location,
            game.Expedition,
            LocationInteractionSupport.LinkedFactions(game, location),
            scenarioActionResolver?.ResolveBaseActionIds(location, game.Knowledge));
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
            resolution = interactionService.ResolveOutcome(
                option.Action,
                option.RiskBand,
                forcedTier,
                new WorldDeterministicRandomSource(game.World));
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
        var commandTrace = game.World.RecordTrace(
            SimulationTraceKind.Command,
            $"Location action '{option.Action.Id}' resolved at '{location.Id}'.",
            subjectIds: new[] { location.Id, option.Action.Id });

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
        var effectTexts = ApplyEffects(game, location, resolution.Effects, recovery, out var expeditionMoved, option.Action.ActionTags, commandTrace.TraceId, findingAcquisitionService);
        scenarioActionResolver?.ValidateRuntimeState(location);
        game.Knowledge.ObserveLocationCondition(location, game.World.WorldDay);
        QueueGenericActionTriggerIfNeeded(game, location, option.Action, triggerCountBeforeEffects, commandTrace.TraceId);
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
        if (action.Commitment == LocationActionCommitment.DayOperation && expedition.MovementPoints == 0)
        {
            return "No daily capacity remains. End the day before starting this operation.";
        }

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

        if (action.Commitment == LocationActionCommitment.DayOperation && expedition.MovementPoints > 0)
        {
            var committedMovement = expedition.MovementPoints;
            expedition.SpendMovementPoints(committedMovement);
            texts.Add("Die Expedition ist fuer den restlichen Tag gebunden.");
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
        int triggerCountBeforeAction,
        string? causedByTraceId = null)
    {
        if (action.ActionTags.Count == 0 || game.World.WorldTriggers.Count > triggerCountBeforeAction)
        {
            return;
        }

        var triggerTrace = game.World.RecordTrace(
            SimulationTraceKind.WorldTrigger,
            $"Location action '{action.Id}' raised generic trigger '{FactionTerritorialPolicyResolver.LocationActionCompletedTriggerId}'.",
            causedByTraceId == null ? null : new[] { causedByTraceId },
            new[] { location.Id, action.Id });
        game.World.QueueWorldTrigger(new WorldTriggerState(
            game.World.RuntimeIds.Allocate("world-trigger"),
            FactionTerritorialPolicyResolver.LocationActionCompletedTriggerId,
            game.World.WorldDay,
            action.ActionTags,
            location.Id,
            location.Coord,
            triggerTrace.TraceId));
    }

    internal static IReadOnlyList<string> ApplyEffects(
        GameState game,
        SpecialLocationState location,
        IReadOnlyList<LocationEffectDefinition> effects,
        LocationRecoveryOutcome? recovery,
        out bool expeditionMoved,
        IReadOnlyList<string>? actionTags = null,
        string? causedByTraceId = null,
        LocationFindingAcquisitionService? findingAcquisitionService = null)
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

            expeditionMoved |= ApplyEffect(game, location, effect, actionTags, causedByTraceId, findingAcquisitionService);
            texts.Add(effect.Text);
        }

        return texts;
    }

    private static bool ApplyEffect(
        GameState game,
        SpecialLocationState location,
        LocationEffectDefinition effect,
        IReadOnlyList<string>? actionTags,
        string? causedByTraceId,
        LocationFindingAcquisitionService? findingAcquisitionService)
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
                    game.World.RuntimeIds.Allocate("evidence"),
                    effect.ReferenceId ?? effect.Id,
                    EvidenceSourceKind.LocationInspection,
                    EvidenceKnowledgeState.Reported,
                    effect.Text,
                    subjectLocationId: location.Id));
                game.World.RecordTrace(
                    SimulationTraceKind.KnowledgeObserved,
                    $"Location effect '{effect.Id}' created evidence '{effect.ReferenceId ?? effect.Id}'.",
                    causedByTraceId == null ? null : new[] { causedByTraceId },
                    new[] { location.Id, effect.ReferenceId ?? effect.Id });
                return false;
            case LocationEffectKind.AddFinding:
                if (effect.ReferenceId == null)
                {
                    throw new InvalidOperationException("AddFinding effect needs a finding definition reference.");
                }

                if (findingAcquisitionService == null)
                {
                    throw new InvalidOperationException("AddFinding effect requires authored finding content.");
                }

                findingAcquisitionService.TryAcquire(game, location, effect.ReferenceId);
                return false;
            case LocationEffectKind.RollFindingTable:
                if (effect.ReferenceId == null) throw new InvalidOperationException("RollFindingTable effect needs a table reference.");
                if (findingAcquisitionService == null) throw new InvalidOperationException("RollFindingTable effect requires authored finding content.");
                findingAcquisitionService.RollTable(game, location, effect.ReferenceId);
                return false;
            case LocationEffectKind.EstablishRelatedFactionContact:
                var relatedFactionIds = location.FactionRelations.Select(relation => relation.FactionId)
                    .Distinct(StringComparer.Ordinal).ToList();
                if (relatedFactionIds.Count != 1) return false;
                var relatedFaction = game.FindFaction(relatedFactionIds[0]);
                if (relatedFaction == null) return false;
                if (relatedFaction.ContactStatus == FactionContactStatus.Unknown || relatedFaction.ContactStatus == FactionContactStatus.Rumored)
                    relatedFaction.SetContactStatus(FactionContactStatus.Contacted);
                return false;
            case LocationEffectKind.RaiseWorldTrigger:
                var triggerTrace = game.World.RecordTrace(
                    SimulationTraceKind.WorldTrigger,
                    $"Location effect '{effect.Id}' raised trigger '{effect.ReferenceId ?? effect.Id}'.",
                    causedByTraceId == null ? null : new[] { causedByTraceId },
                    new[] { location.Id, effect.ReferenceId ?? effect.Id });
                game.World.QueueWorldTrigger(new WorldTriggerState(
                    game.World.RuntimeIds.Allocate("world-trigger"),
                    effect.ReferenceId ?? effect.Id,
                    game.World.WorldDay,
                    actionTags: actionTags,
                    sourceLocationId: location.Id,
                    sourceCoord: location.Coord,
                    causedByTraceId: triggerTrace.TraceId));
                return false;
            case LocationEffectKind.ScheduleConsequence:
                var delayDays = Math.Max(0, effect.DelayDays);
                game.World.ScheduleConsequence(new ScheduledConsequenceState(
                    game.World.RuntimeIds.Allocate("world-process"),
                    effect.ReferenceId ?? effect.Id,
                    location.Id,
                    game.World.WorldDay + delayDays,
                    new[] { effect.Id }));
                game.World.RecordTrace(
                    SimulationTraceKind.WorldProcessScheduled,
                    $"Location effect '{effect.Id}' directly scheduled consequence '{effect.ReferenceId ?? effect.Id}'.",
                    causedByTraceId == null ? null : new[] { causedByTraceId },
                    new[] { location.Id, effect.ReferenceId ?? effect.Id });
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
    private readonly LocationFindingAcquisitionService? findingAcquisitionService;

    public AdvanceLocationProjectCommand(LocationInteractionDefinitionSet definitions, LocationFindingAcquisitionService? findingAcquisitionService = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.findingAcquisitionService = findingAcquisitionService;
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
        var texts = ResolveLocationActionCommand.ApplyEffects(game, location, action.ProjectCompletionEffects, null, out var expeditionMoved, action.ActionTags, findingAcquisitionService: findingAcquisitionService);
        ResolveLocationActionCommand.QueueGenericActionTriggerIfNeeded(game, location, action, triggerCountBeforeEffects);
        game.Knowledge.ObserveLocationCondition(location, game.World.WorldDay);
        location.ClearProject();
        return LocationActionResult.Resolved(location, action, null, null, texts, expeditionMoved);
    }
}
}
