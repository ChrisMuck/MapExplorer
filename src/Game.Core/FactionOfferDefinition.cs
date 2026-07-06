#nullable enable
using System;

namespace Game.Core
{

public sealed class FactionOfferDefinition
{
    public FactionOfferDefinition(
        string id,
        string factionId,
        string title,
        string description,
        FactionOfferEffectKind effectKind,
        int knowledgeCost = 0,
        int medicineCost = 0,
        int supplyReward = 0,
        bool repeatable = false,
        string? requiredLeverageItemId = null,
        bool consumesRequiredLeverage = false,
        string? resolvedMemoryId = null,
        string? lockedDescription = null,
        string? lockedReasonWhenMissing = null,
        string? lockedReasonWhenResolved = null)
    {
        if (knowledgeCost < 0 || medicineCost < 0 || supplyReward < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(knowledgeCost), "Offer costs and rewards must not be negative.");
        }

        Id = RequireText(id, nameof(id));
        FactionId = RequireText(factionId, nameof(factionId));
        Title = RequireText(title, nameof(title));
        Description = RequireText(description, nameof(description));
        EffectKind = effectKind;
        KnowledgeCost = knowledgeCost;
        MedicineCost = medicineCost;
        SupplyReward = supplyReward;
        Repeatable = repeatable;
        RequiredLeverageItemId = string.IsNullOrWhiteSpace(requiredLeverageItemId) ? null : requiredLeverageItemId;
        ConsumesRequiredLeverage = consumesRequiredLeverage;
        ResolvedMemoryId = string.IsNullOrWhiteSpace(resolvedMemoryId) ? null : resolvedMemoryId;
        LockedDescription = string.IsNullOrWhiteSpace(lockedDescription) ? null : lockedDescription;
        LockedReasonWhenMissing = string.IsNullOrWhiteSpace(lockedReasonWhenMissing) ? null : lockedReasonWhenMissing;
        LockedReasonWhenResolved = string.IsNullOrWhiteSpace(lockedReasonWhenResolved) ? null : lockedReasonWhenResolved;
    }

    public string Id { get; }

    public string FactionId { get; }

    public string Title { get; }

    public string Description { get; }

    public FactionOfferEffectKind EffectKind { get; }

    public int KnowledgeCost { get; }

    public int MedicineCost { get; }

    public int SupplyReward { get; }

    public bool Repeatable { get; }

    public string? RequiredLeverageItemId { get; }

    public bool ConsumesRequiredLeverage { get; }

    public string? ResolvedMemoryId { get; }

    public string? LockedDescription { get; }

    public string? LockedReasonWhenMissing { get; }

    public string? LockedReasonWhenResolved { get; }

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
