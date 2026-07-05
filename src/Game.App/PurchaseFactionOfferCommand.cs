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

        if (offer.KnowledgeCost > 0 && !game.Base.SpendKnowledgePoints(offer.KnowledgeCost))
        {
            return FactionOfferResult.Rejected($"Not enough Knowledge Points. Need {offer.KnowledgeCost}.");
        }

        ApplyOffer(game, interaction, offer);
        interaction.MarkOfferAccepted(offer.Id);

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
            case FactionOfferEffectKind.WarningInterpretation:
                game.PlayerNotes.AddMarker(new PlayerMapMarkerState(
                    $"warning-interpretation-{interaction.FactionId}-{game.World.WorldDay}",
                    interaction.Coord,
                    PlayerMapMarkerKind.FactionWarning,
                    $"{interaction.FactionName} warning signs interpreted",
                    interaction.FactionId));
                break;
        }
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
            case FactionOfferEffectKind.WarningInterpretation:
                return "Warning signs recorded as faction notes.";
            default:
                return "Offer accepted.";
        }
    }
}
}
