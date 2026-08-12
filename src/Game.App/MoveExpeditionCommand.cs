using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class MoveExpeditionCommand
{
    private const int FirstFactionContactKnowledge = 10;

    private readonly MovementCostService movementCostService;
    private readonly KnowledgeService knowledgeService;
    private readonly FactionTerritoryEntryResolver factionTerritoryEntryResolver;
    private readonly LocationInspectionPresentationResolver? locationPresentationResolver;

    public MoveExpeditionCommand(MovementCostService movementCostService)
        : this(movementCostService, new KnowledgeService())
    {
    }

    public MoveExpeditionCommand(MovementCostService movementCostService, KnowledgeService knowledgeService)
        : this(movementCostService, knowledgeService, null)
    {
    }

    public MoveExpeditionCommand(
        MovementCostService movementCostService,
        KnowledgeService knowledgeService,
        FactionTerritoryEntryResolver? factionTerritoryEntryResolver,
        LocationInspectionPresentationResolver? locationPresentationResolver = null)
    {
        this.movementCostService = movementCostService ?? throw new ArgumentNullException(nameof(movementCostService));
        this.knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        this.factionTerritoryEntryResolver = factionTerritoryEntryResolver ?? new FactionTerritoryEntryResolver(null);
        this.locationPresentationResolver = locationPresentationResolver;
    }

    public MoveExpeditionResult Execute(GameState game, HexCoord destination)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return MoveExpeditionResult.Rejected("Expedition is not active.");
        }

        var from = game.Expedition.Position;
        if (from == destination)
        {
            return MoveExpeditionResult.Rejected("Expedition is already at the destination.");
        }

        if (from.DistanceTo(destination) != 1)
        {
            return MoveExpeditionResult.Rejected("Destination is not adjacent.");
        }

        if (!game.World.Map.TryGetTile(destination, out var destinationTile) || destinationTile == null)
        {
            return MoveExpeditionResult.Rejected("Destination is outside the map.");
        }

        if (IsBlockedByEdgeLocation(game, from, destination))
        {
            return MoveExpeditionResult.Rejected("The route is blocked by a location obstacle.");
        }

        var cost = movementCostService.GetEntryCost(destinationTile);
        if (!cost.CanEnter)
        {
            return MoveExpeditionResult.Rejected(cost.Reason ?? "Destination cannot be entered.");
        }

        if (cost.Cost > game.Expedition.MovementPoints)
        {
            return MoveExpeditionResult.Rejected("Not enough movement points.");
        }

        var destinationWasConfirmed = game.Knowledge.GetTileKnowledge(destination) == KnowledgeLevel.Confirmed;

        game.Expedition.SpendMovementPoints(cost.Cost);
        game.Expedition.SetPosition(destination);
        knowledgeService.RevealFromExpedition(game.World.Map, game.Knowledge, destination);
        if (!destinationWasConfirmed && game.Knowledge.ClaimKnowledgeSource($"confirmed-hex:{destination.Q}:{destination.R}"))
        {
            game.Expedition.AddUnsecuredKnowledge(1);
        }

        ApplyFactionEntry(game, destinationTile, destination);

        var arrivalScenes = ObserveArrivedLocations(game, destination);

        return MoveExpeditionResult.Moved(from, destination, cost.Cost, arrivalScenes);
    }

    private IReadOnlyList<SceneDescriptionResult> ObserveArrivedLocations(GameState game, HexCoord destination)
    {
        if (locationPresentationResolver == null) return Array.Empty<SceneDescriptionResult>();
        var scenes = new List<SceneDescriptionResult>();
        foreach (var location in game.World.Locations.Where(item => item.Anchor.Coords.Contains(destination)).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            var previous = game.Knowledge.FindLocationCondition(location.Id);
            location.Discover(game.World.WorldDay);
            var presentation = locationPresentationResolver.ObserveArrivalAndResolve(game, location, previous);
            game.Knowledge.ObserveLocationCondition(location, game.World.WorldDay);
            if (presentation?.Scene != null) scenes.Add(presentation.Scene);
        }
        return scenes;
    }

    private static bool IsBlockedByEdgeLocation(GameState game, HexCoord from, HexCoord to)
    {
        if (HasOpenedRouteAcrossEdge(game.World, from, to))
        {
            return false;
        }

        foreach (var location in game.World.Locations)
        {
            if (location.Anchor.Kind == LocationAnchorKind.Edge &&
                location.Anchor.Coords.Count == 2 &&
                EdgeMatches(location.Anchor.Coords[0], location.Anchor.Coords[1], from, to) &&
                IsBlockingOperationalState(location.OperationalStateId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOpenedRouteAcrossEdge(WorldState world, HexCoord from, HexCoord to)
    {
        foreach (var path in world.Paths)
        {
            // A authored visual road can still terminate at a damaged bridge or ravine. Only the
            // ordinary OpenRoute effect creates a route that overrides an active edge obstacle.
            // Without this distinction, a map artist could accidentally make a blocked location
            // traversable merely by drawing the historical road through it.
            if (path.Kind != WorldPathKind.Road ||
                !path.Id.StartsWith("route-opened-", StringComparison.Ordinal))
            {
                continue;
            }

            for (var i = 0; i < path.Coords.Count - 1; i++)
            {
                if (EdgeMatches(path.Coords[i], path.Coords[i + 1], from, to))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool EdgeMatches(HexCoord a, HexCoord b, HexCoord from, HexCoord to)
    {
        return (a == from && b == to) || (a == to && b == from);
    }

    private static bool IsBlockingOperationalState(string operationalStateId)
    {
        return operationalStateId == LocationStateIds.Operational.Blocked ||
            operationalStateId == LocationStateIds.Operational.RiskyPassage ||
            operationalStateId == LocationStateIds.Operational.Destroyed ||
            operationalStateId == LocationStateIds.Operational.Sealed;
    }

    private void ApplyFactionEntry(GameState game, HexTileState destinationTile, HexCoord destination)
    {
        if (string.IsNullOrWhiteSpace(destinationTile.OwnerId))
        {
            return;
        }

        var faction = game.FindFaction(destinationTile.OwnerId);
        if (faction == null)
        {
            return;
        }

        ApplyFactionKnowledge(game, faction);

        var isWarningZone = faction.IsWarningZone(destination);
        if (factionTerritoryEntryResolver.TryResolve(game, faction, destination, isWarningZone))
        {
            return;
        }

        if (isWarningZone)
        {
            ApplyFactionWarningZoneEntry(game, faction, destination);
            return;
        }

        ApplyFactionTerritoryReaction(game, faction, destination);
    }

    private static void ApplyFactionWarningZoneEntry(GameState game, FactionState faction, HexCoord destination)
    {
        var memoryKey = $"warning-zone-entered:{destination.Q}:{destination.R}";
        if (faction.HasMemory(memoryKey))
        {
            return;
        }

        faction.AddMemory(memoryKey);
        if (faction.ContactStatus == FactionContactStatus.Unknown)
        {
            faction.SetContactStatus(FactionContactStatus.Rumored);
        }

        faction.Adjust(angerDelta: 6, fearDelta: 3);
        game.Events.Enqueue(CreateFactionWarningEvent(game, faction, destination));
    }

    private static void ApplyFactionTerritoryReaction(GameState game, FactionState faction, HexCoord destination)
    {
        var memoryKey = $"territory-entry-expedition-{game.Expedition.ExpeditionNumber}:{faction.Id}";
        if (faction.HasMemory(memoryKey))
        {
            return;
        }

        faction.AddMemory(memoryKey);
        ApplyFactionReactionMetrics(faction);
        game.Events.Enqueue(CreateFactionTerritoryReactionEvent(game, faction, destination));
    }

    private static void ApplyFactionReactionMetrics(FactionState faction)
    {
        faction.Adjust(fearDelta: 1);
    }

    private static void ApplyFactionKnowledge(GameState game, FactionState faction)
    {
        if (game.Knowledge.ClaimKnowledgeSource($"faction-contact:{faction.Id}"))
        {
            game.Expedition.AddUnsecuredKnowledge(FirstFactionContactKnowledge);
            faction.AddMemory($"expedition-entered-territory:{game.World.WorldDay}");
        }

        if (faction.ContactStatus == FactionContactStatus.Unknown)
        {
            faction.SetContactStatus(FactionContactStatus.Rumored);
        }
    }

    private static EventState CreateFactionWarningEvent(GameState game, FactionState faction, HexCoord coord)
    {
        return new EventState(
            $"event-{game.Events.Events.Count + 1}",
            EventKind.WarningSign,
            "Ein fremdes Warnzeichen",
            "Unbekannte Gruppe",
            "The expedition crossed a line marked by old stones and carved posts. This is not a clean border on a map, but someone likely expects it to be respected.",
            new[]
            {
                new EventOptionState("mark", "Mark warning", "A faction warning marker was added to the map.", EventOptionEffectKind.AddWarningMarker),
                new EventOptionState("archive", "Archive observation", "The expedition recorded a suspected warning zone.", EventOptionEffectKind.Archive),
                new EventOptionState("continue", "Continue carefully", "The expedition continues, aware that the crossing may be remembered.", EventOptionEffectKind.None)
            },
            coord);
    }

    private static EventState CreateFactionTerritoryReactionEvent(GameState game, FactionState faction, HexCoord coord)
    {
        var options = new List<EventOptionState>();
        if (CanOpenFactionInteraction(faction))
        {
            options.Add(new EventOptionState(
                "contact",
                "Open contact",
                "The expedition approaches the visible representative.",
                EventOptionEffectKind.OpenFactionInteraction));
        }

        options.Add(new EventOptionState("archive", "Archive observation", "The expedition recorded how the unknown group reacted to its presence.", EventOptionEffectKind.Archive));
        options.Add(new EventOptionState("continue", "Continue carefully", "The expedition continues while watching for further signs.", EventOptionEffectKind.None));

        return new EventState(
            $"event-{game.Events.Events.Count + 1}",
            EventKind.FactionReaction,
            "Fremdes Gebiet",
            faction.ContactStatus is FactionContactStatus.Contacted or FactionContactStatus.Open or FactionContactStatus.Hostile ? faction.Name : "Unbekannte Gruppe",
            "The expedition has entered another group's territory. The reaction is subtle, but the crossing will likely be remembered.",
            options,
            coord,
            faction.Id);
    }

    private static bool CanOpenFactionInteraction(FactionState faction)
    {
        return faction.ContactStatus is FactionContactStatus.Contacted or FactionContactStatus.Open or FactionContactStatus.Hostile;
    }
}
}
