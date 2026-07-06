#nullable enable
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public static class FactionInteractionDefinitions
{
    public const string BorderWardenGraveTokenId = "border-warden-grave-token";
    public const string BorderWardenGraveTokenReturnedMemory = "grave-token-returned";

    private static readonly FactionOfferDefinition[] offerDefinitions =
    {
        new FactionOfferDefinition(
            "knowledge-for-supplies",
            "coastal-people",
            "10 Supplies",
            "Spend archived knowledge to buy food, local guidance and pack support.",
            FactionOfferEffectKind.SuppliesForKnowledge,
            knowledgeCost: 6,
            supplyReward: 10,
            repeatable: true),
        new FactionOfferDefinition(
            "knowledge-for-supplies",
            "border-wardens",
            "10 Supplies",
            "Spend archived knowledge to buy food, local guidance and pack support.",
            FactionOfferEffectKind.SuppliesForKnowledge,
            knowledgeCost: 6,
            supplyReward: 10,
            repeatable: true),
        new FactionOfferDefinition(
            "river-route-hint",
            "coastal-people",
            "River route hint",
            "A messenger describes two safer river bends beyond the reeds.",
            FactionOfferEffectKind.RouteHint,
            knowledgeCost: 4),
        new FactionOfferDefinition(
            "warning-interpretation",
            "border-wardens",
            "Warning sign interpretation",
            "The scout explains which carved posts mark watched land and which mark forbidden land.",
            FactionOfferEffectKind.WarningInterpretation,
            knowledgeCost: 5),
        new FactionOfferDefinition(
            "grave-token-passage",
            "border-wardens",
            "Proper negotiation",
            "Return the grave token as proof of respect. The patrol marks a limited pass through watched land.",
            FactionOfferEffectKind.PassageNegotiation,
            requiredLeverageItemId: BorderWardenGraveTokenId,
            resolvedMemoryId: BorderWardenGraveTokenReturnedMemory,
            lockedDescription: "The patrol will not discuss passage until a grave token or equivalent proof is returned.",
            lockedReasonWhenMissing: "Requires grave token",
            lockedReasonWhenResolved: "Already resolved")
    };

    private static readonly LeverageObjectDefinition[] leverageDefinitions =
    {
        new LeverageObjectDefinition(
            BorderWardenGraveTokenId,
            "Border grave token",
            "marked-grave",
            "border-wardens",
            "grave-token-passage",
            "Consumed when returned through negotiation; lost if the expedition fails before archival return.")
    };

    public static IReadOnlyList<FactionOfferDefinition> OfferDefinitions
    {
        get { return offerDefinitions; }
    }

    public static IReadOnlyList<LeverageObjectDefinition> LeverageDefinitions
    {
        get { return leverageDefinitions; }
    }

    public static IEnumerable<FactionOfferState> BuildOffers(GameState game, FactionState faction)
    {
        foreach (var definition in offerDefinitions)
        {
            if (definition.FactionId != faction.Id)
            {
                continue;
            }

            yield return BuildOffer(game, faction, definition);
        }
    }

    private static FactionOfferState BuildOffer(GameState game, FactionState faction, FactionOfferDefinition definition)
    {
        var isAvailable = true;
        string? lockedReason = null;
        var description = definition.Description;

        if (!string.IsNullOrWhiteSpace(definition.ResolvedMemoryId) && faction.HasMemory(definition.ResolvedMemoryId))
        {
            isAvailable = false;
            lockedReason = definition.LockedReasonWhenResolved ?? "Already resolved";
        }
        else if (!string.IsNullOrWhiteSpace(definition.RequiredLeverageItemId) && !game.LeverageItems.Contains(definition.RequiredLeverageItemId))
        {
            isAvailable = false;
            lockedReason = definition.LockedReasonWhenMissing ?? "Requires leverage";
            description = definition.LockedDescription ?? definition.Description;
        }

        return new FactionOfferState(
            definition.Id,
            definition.Title,
            description,
            definition.EffectKind,
            definition.KnowledgeCost,
            definition.MedicineCost,
            definition.SupplyReward,
            definition.Repeatable,
            isAvailable,
            lockedReason,
            definition.RequiredLeverageItemId,
            definition.ResolvedMemoryId);
    }
}
}
