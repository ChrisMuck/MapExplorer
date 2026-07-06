#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class FactionInteractionState
{
    private readonly List<FactionOfferState> offers;
    private readonly HashSet<string> acceptedOfferIds = new();

    public FactionInteractionState(
        string id,
        string factionId,
        string factionName,
        HexCoord coord,
        FactionRepresentativeState representative,
        string attitudeText,
        string dialogueText,
        IEnumerable<FactionOfferState> offers)
    {
        Id = RequireText(id, nameof(id));
        FactionId = RequireText(factionId, nameof(factionId));
        FactionName = RequireText(factionName, nameof(factionName));
        Coord = coord;
        Representative = representative ?? throw new ArgumentNullException(nameof(representative));
        AttitudeText = RequireText(attitudeText, nameof(attitudeText));
        DialogueText = RequireText(dialogueText, nameof(dialogueText));
        this.offers = new List<FactionOfferState>(offers ?? throw new ArgumentNullException(nameof(offers)));
    }

    public string Id { get; }

    public string FactionId { get; }

    public string FactionName { get; }

    public HexCoord Coord { get; }

    public FactionRepresentativeState Representative { get; }

    public string AttitudeText { get; }

    public string DialogueText { get; }

    public IReadOnlyList<FactionOfferState> Offers
    {
        get { return offers; }
    }

    public IReadOnlyCollection<string> AcceptedOfferIds
    {
        get { return acceptedOfferIds; }
    }

    public FactionOfferState? FindOffer(string offerId)
    {
        foreach (var offer in offers)
        {
            if (offer.Id == offerId)
            {
                return offer;
            }
        }

        return null;
    }

    public bool HasAcceptedOffer(string offerId)
    {
        return acceptedOfferIds.Contains(offerId);
    }

    public void MarkOfferAccepted(string offerId)
    {
        if (!string.IsNullOrWhiteSpace(offerId))
        {
            acceptedOfferIds.Add(offerId);
        }
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}
}
