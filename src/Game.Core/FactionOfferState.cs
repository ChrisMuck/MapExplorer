#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class FactionOfferState
{
    public FactionOfferState(
        string id,
        string title,
        string description,
        FactionOfferEffectKind effectKind,
        int knowledgeCost = 0,
        int medicineCost = 0,
        int supplyReward = 0,
        bool repeatable = false,
        bool isAvailable = true,
        string? lockedReason = null,
        string? requiredLeverageItemId = null,
        bool consumesRequiredLeverage = false,
        string? resolvedMemoryId = null,
        int medicineReward = 0)
    {
        if (knowledgeCost < 0 || medicineCost < 0 || supplyReward < 0 || medicineReward < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(knowledgeCost), "Offer costs and rewards must not be negative.");
        }

        Id = RequireText(id, nameof(id));
        Title = RequireText(title, nameof(title));
        Description = RequireText(description, nameof(description));
        EffectKind = effectKind;
        KnowledgeCost = knowledgeCost;
        MedicineCost = medicineCost;
        SupplyReward = supplyReward;
        MedicineReward = medicineReward;
        Repeatable = repeatable;
        IsAvailable = isAvailable;
        LockedReason = string.IsNullOrWhiteSpace(lockedReason) ? null : lockedReason;
        RequiredLeverageItemId = string.IsNullOrWhiteSpace(requiredLeverageItemId) ? null : requiredLeverageItemId;
        ConsumesRequiredLeverage = consumesRequiredLeverage;
        ResolvedMemoryId = string.IsNullOrWhiteSpace(resolvedMemoryId) ? null : resolvedMemoryId;
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public FactionOfferEffectKind EffectKind { get; }

    public int KnowledgeCost { get; }

    public int MedicineCost { get; }

    public int SupplyReward { get; }

    public int MedicineReward { get; }

    public bool Repeatable { get; }

    public bool IsAvailable { get; }

    public string? LockedReason { get; }

    public string? RequiredLeverageItemId { get; }

    public bool ConsumesRequiredLeverage { get; }

    public string? ResolvedMemoryId { get; }

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
