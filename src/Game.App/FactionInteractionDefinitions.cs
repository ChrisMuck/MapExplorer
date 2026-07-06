#nullable enable
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public static class FactionInteractionDefinitions
{
    public const string BorderWardenGraveTokenId = "border-warden-grave-token";
    public const string BorderWardenGraveTokenReturnedMemory = "grave-token-returned";
    public const string CoastalRiverChartFragmentId = "coastal-river-chart-fragment";
    public const string CoastalRiverChartSharedMemory = "coastal-river-chart-shared";
    public const string HiddenSealedSymbolId = "hidden-sealed-symbol";
    public const string HiddenSealedSymbolUnderstoodMemory = "hidden-sealed-symbol-understood";

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
            "coastal-chart-guidance",
            "coastal-people",
            "Read the old river chart",
            "Share the old river chart fragment from the abandoned camp. The messenger marks a safer crossing.",
            FactionOfferEffectKind.RouteHint,
            requiredLeverageItemId: CoastalRiverChartFragmentId,
            resolvedMemoryId: CoastalRiverChartSharedMemory,
            lockedDescription: "The messenger could compare routes if the expedition found an old river chart or similar trace.",
            lockedReasonWhenMissing: "Requires river chart",
            lockedReasonWhenResolved: "Already shared"),
        new FactionOfferDefinition(
            "coastal-safe-camp",
            "coastal-people",
            "Safe reed camp",
            "The messenger points out a dry camp above the wet ground where fires are hard to see.",
            FactionOfferEffectKind.SafeCampHint,
            knowledgeCost: 5),
        new FactionOfferDefinition(
            "warning-interpretation",
            "border-wardens",
            "Warning sign interpretation",
            "The scout explains which carved posts mark watched land and which mark forbidden land.",
            FactionOfferEffectKind.WarningInterpretation,
            knowledgeCost: 5),
        new FactionOfferDefinition(
            "border-spring-location",
            "border-wardens",
            "Guarded spring",
            "The scout names a spring near the ridge that patrols tolerate if visitors do not camp there.",
            FactionOfferEffectKind.SpringLocation,
            knowledgeCost: 6),
        new FactionOfferDefinition(
            "grave-token-passage",
            "border-wardens",
            "Proper negotiation",
            "Return the grave token as proof of respect. The patrol marks a limited pass through watched land.",
            FactionOfferEffectKind.PassageNegotiation,
            requiredLeverageItemId: BorderWardenGraveTokenId,
            consumesRequiredLeverage: true,
            resolvedMemoryId: BorderWardenGraveTokenReturnedMemory,
            lockedDescription: "The patrol will not discuss passage until a grave token or equivalent proof is returned.",
            lockedReasonWhenMissing: "Requires grave token",
            lockedReasonWhenResolved: "Already resolved"),
        new FactionOfferDefinition(
            "hidden-boundary-warning",
            "hidden-ones",
            "Forbidden boundary warning",
            "A masked watcher marks where the expedition must not cross if it wants to leave alive.",
            FactionOfferEffectKind.ForbiddenZoneWarning,
            resolvedMemoryId: "hidden-boundary-warning-given",
            lockedReasonWhenResolved: "Already warned"),
        new FactionOfferDefinition(
            "hidden-sealed-symbol-reading",
            "hidden-ones",
            "Meaning of the sealed mark",
            "Show the symbol copied from the old watchtower. The watcher gives one warning about the sealed place.",
            FactionOfferEffectKind.ForbiddenZoneWarning,
            requiredLeverageItemId: HiddenSealedSymbolId,
            resolvedMemoryId: HiddenSealedSymbolUnderstoodMemory,
            lockedDescription: "The watcher refuses to explain the sealed place without proof that the expedition has seen its mark.",
            lockedReasonWhenMissing: "Requires sealed symbol",
            lockedReasonWhenResolved: "Already interpreted")
    };

    private static readonly LeverageObjectDefinition[] leverageDefinitions =
    {
        new LeverageObjectDefinition(
            BorderWardenGraveTokenId,
            "Border grave token",
            "marked-grave",
            "border-wardens",
            "grave-token-passage",
            "Consumed when returned through negotiation; lost if the expedition fails before archival return."),
        new LeverageObjectDefinition(
            CoastalRiverChartFragmentId,
            "Old river chart fragment",
            "abandoned-camp",
            "coastal-people",
            "coastal-chart-guidance",
            "Can be used as evidence in conversation; lost if the expedition fails before archival return."),
        new LeverageObjectDefinition(
            HiddenSealedSymbolId,
            "Copied sealed symbol",
            "watchtower",
            "hidden-ones",
            "hidden-sealed-symbol-reading",
            "Can be used as evidence in conversation; lost if the expedition fails before archival return.")
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
            definition.ConsumesRequiredLeverage,
            definition.ResolvedMemoryId);
    }

    public static IEnumerable<LeverageObjectDefinition> LeverageForLocation(SpecialLocationState location)
    {
        foreach (var definition in leverageDefinitions)
        {
            if (definition.Source == location.Id)
            {
                yield return definition;
            }
        }
    }
}
}
