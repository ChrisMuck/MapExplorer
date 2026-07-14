#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App;

/// <summary>Resolves profile-based authored offers for one generated faction instance.</summary>
public sealed class AuthoredFactionOfferService
{
    private readonly CrossSystemDataBundle crossSystem;
    private readonly CrossSystemAuthoringBundle authoring;

    public AuthoredFactionOfferService(CrossSystemDataBundle crossSystem, CrossSystemAuthoringBundle authoring)
    {
        this.crossSystem = crossSystem ?? throw new ArgumentNullException(nameof(crossSystem));
        this.authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
    }

    public IEnumerable<FactionOfferState> BuildOffers(FactionState faction)
    {
        var profile = crossSystem.FactionProfiles.Find(faction.ReactionProfileId);
        if (profile == null) yield break;

        foreach (var offer in authoring.FactionOffers.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!offer.EligibleProfileTags.Any(profile.Values.Contains) || !MatchesContactStatus(offer, faction.ContactStatus)) continue;
            var effect = offer.Effects[0];
            yield return new FactionOfferState(
                offer.Id,
                offer.Title,
                offer.Description,
                ToEffectKind(effect.Kind),
                knowledgeCost: offer.KnowledgePointCost,
                supplyReward: effect.Supplies,
                repeatable: string.Equals(offer.RepeatPolicy, "repeatable", StringComparison.Ordinal),
                medicineReward: effect.Medicine);
        }
    }

    private static bool MatchesContactStatus(FactionOfferContentDefinition offer, FactionContactStatus status)
    {
        if (offer.RequiredContactStatuses.Count == 0) return true;
        return offer.RequiredContactStatuses.Any(required => required switch
        {
            "first-contact" => status is FactionContactStatus.Contacted or FactionContactStatus.Open,
            "trusted" => status == FactionContactStatus.Open,
            _ => Enum.TryParse<FactionContactStatus>(required, ignoreCase: true, out var parsed) && status == parsed
        });
    }

    private static FactionOfferEffectKind ToEffectKind(string kind) => kind switch
    {
        "grant-supplies" => FactionOfferEffectKind.SuppliesForKnowledge,
        "grant-medicine" => FactionOfferEffectKind.MedicineForKnowledge,
        "grant-contact-report" => FactionOfferEffectKind.ContactReport,
        "grant-access" => FactionOfferEffectKind.AccessHint,
        _ => throw new LocationDataException($"Faction offer effect '{kind}' has no runtime resolver.")
    };
}
