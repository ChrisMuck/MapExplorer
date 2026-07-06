#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class PurchaseFactionOfferCommand
{
    public FactionOfferResult Execute(GameState game, string offerId)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (string.IsNullOrWhiteSpace(offerId))
        {
            throw new ArgumentException("Offer id must not be empty.", nameof(offerId));
        }

        var interaction = game.ActiveFactionInteraction;
        if (interaction == null)
        {
            return FactionOfferResult.Rejected("No active faction interaction.");
        }

        var offer = interaction.FindOffer(offerId);
        if (offer == null)
        {
            return FactionOfferResult.Rejected("Unknown offer.");
        }

        if (!offer.IsAvailable)
        {
            return FactionOfferResult.Rejected(offer.LockedReason ?? "Offer is not available.");
        }

        if (!offer.Repeatable && interaction.HasAcceptedOffer(offer.Id))
        {
            return FactionOfferResult.Rejected("Offer already accepted.");
        }

        if (!string.IsNullOrWhiteSpace(offer.RequiredLeverageItemId) && !game.LeverageItems.Contains(offer.RequiredLeverageItemId))
        {
            return FactionOfferResult.Rejected(offer.LockedReason ?? "Required leverage is missing.");
        }

        if (offer.KnowledgeCost > 0 && !game.Base.SpendKnowledgePoints(offer.KnowledgeCost))
        {
            return FactionOfferResult.Rejected($"Not enough Knowledge Points. Need {offer.KnowledgeCost}.");
        }

        if (offer.ConsumesRequiredLeverage && !string.IsNullOrWhiteSpace(offer.RequiredLeverageItemId))
        {
            game.LeverageItems.Consume(offer.RequiredLeverageItemId);
        }

        ApplyOffer(game, interaction, offer);
        interaction.MarkOfferAccepted(offer.Id);
        MarkOfferResolved(game, interaction, offer);

        var archiveEntry = $"Day {game.World.WorldDay}: accepted faction offer '{offer.Title}' from {interaction.FactionName}.";
        game.Base.AddArchiveEntry(archiveEntry);

        return FactionOfferResult.Accepted(offer, BuildResultMessage(offer), archiveEntry);
    }

    private static void ApplyOffer(GameState game, FactionInteractionState interaction, FactionOfferState offer)
    {
        switch (offer.EffectKind)
        {
            case FactionOfferEffectKind.SuppliesForKnowledge:
                game.Expedition.AddSupplies(offer.SupplyReward);
                break;
            case FactionOfferEffectKind.RouteHint:
                RevealNearbyReportedHexes(game, interaction.Coord, maxCount: 2);
                game.PlayerNotes.AddMarker(new PlayerMapMarkerState(
                    $"route-hint-{interaction.FactionId}-{game.World.WorldDay}",
                    interaction.Coord,
                    PlayerMapMarkerKind.FactionRumor,
                    $"{interaction.FactionName} route hint",
                    interaction.FactionId));
                break;
            case FactionOfferEffectKind.SafeCampHint:
                AddFactionMarker(
                    game,
                    interaction,
                    PlayerMapMarkerKind.Resource,
                    "safe-camp",
                    $"{interaction.FactionName} safe camp");
                break;
            case FactionOfferEffectKind.SpringLocation:
                AddFactionMarker(
                    game,
                    interaction,
                    PlayerMapMarkerKind.Resource,
                    "spring",
                    $"{interaction.FactionName} guarded spring");
                break;
            case FactionOfferEffectKind.WarningInterpretation:
                game.PlayerNotes.AddMarker(new PlayerMapMarkerState(
                    $"warning-interpretation-{interaction.FactionId}-{game.World.WorldDay}",
                    interaction.Coord,
                    PlayerMapMarkerKind.FactionWarning,
                    $"{interaction.FactionName} warning signs interpreted",
                    interaction.FactionId));
                break;
            case FactionOfferEffectKind.PassageNegotiation:
                ApplyPassageNegotiation(game, interaction, offer);
                break;
            case FactionOfferEffectKind.ForbiddenZoneWarning:
                AddFactionMarker(
                    game,
                    interaction,
                    PlayerMapMarkerKind.Danger,
                    "forbidden-zone",
                    $"{interaction.FactionName} forbidden boundary");
                break;
        }
    }

    private static void AddFactionMarker(
        GameState game,
        FactionInteractionState interaction,
        PlayerMapMarkerKind kind,
        string markerPrefix,
        string label)
    {
        game.PlayerNotes.AddMarker(new PlayerMapMarkerState(
            $"{markerPrefix}-{interaction.FactionId}-{game.World.WorldDay}",
            interaction.Coord,
            kind,
            label,
            interaction.FactionId));
    }

    private static void ApplyPassageNegotiation(GameState game, FactionInteractionState interaction, FactionOfferState offer)
    {
        var faction = game.FindFaction(interaction.FactionId);
        if (faction != null)
        {
            faction.Adjust(trustDelta: 8, angerDelta: -4);
        }

        game.PlayerNotes.AddMarker(new PlayerMapMarkerState(
            $"passage-negotiation-{interaction.FactionId}-{game.World.WorldDay}",
            interaction.Coord,
            PlayerMapMarkerKind.FactionContact,
            $"{interaction.FactionName} limited passage",
            interaction.FactionId));
    }

    private static void MarkOfferResolved(GameState game, FactionInteractionState interaction, FactionOfferState offer)
    {
        if (string.IsNullOrWhiteSpace(offer.ResolvedMemoryId))
        {
            return;
        }

        var faction = game.FindFaction(interaction.FactionId);
        faction?.AddMemory(offer.ResolvedMemoryId);
    }

    private static void RevealNearbyReportedHexes(GameState game, HexCoord origin, int maxCount)
    {
        var count = 0;
        foreach (var neighbor in origin.Neighbors())
        {
            if (count >= maxCount)
            {
                return;
            }

            if (!game.World.Map.Contains(neighbor))
            {
                continue;
            }

            game.Knowledge.PromoteTileKnowledge(neighbor, KnowledgeLevel.Reported);
            count += 1;
        }
    }

    private static string BuildResultMessage(FactionOfferState offer)
    {
        switch (offer.EffectKind)
        {
            case FactionOfferEffectKind.SuppliesForKnowledge:
                return $"Supplies gained: +{offer.SupplyReward}.";
            case FactionOfferEffectKind.RouteHint:
                return "Route hint recorded on the map.";
            case FactionOfferEffectKind.SafeCampHint:
                return "Safe camp hint recorded on the map.";
            case FactionOfferEffectKind.SpringLocation:
                return "Spring location recorded on the map.";
            case FactionOfferEffectKind.WarningInterpretation:
                return "Warning signs recorded as faction notes.";
            case FactionOfferEffectKind.PassageNegotiation:
                return "The grave token was returned. A limited passage contact was marked.";
            case FactionOfferEffectKind.ForbiddenZoneWarning:
                return "Forbidden boundary warning recorded on the map.";
            default:
                return "Offer accepted.";
        }
    }
}
}
