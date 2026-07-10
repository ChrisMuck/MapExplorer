#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>Fixed outcome-tier vocabulary shared by every location action (concept Section 17.6).</summary>
public enum LocationOutcomeTier
{
    MajorSuccess,
    Success,
    SuccessWithCost,
    PartialResult,
    Failure,
    SevereFailure
}

public static class LocationOutcomeTiers
{
    public static string DisplayLabel(LocationOutcomeTier tier)
    {
        switch (tier)
        {
            case LocationOutcomeTier.MajorSuccess: return "Grosser Erfolg";
            case LocationOutcomeTier.Success: return "Erfolg";
            case LocationOutcomeTier.SuccessWithCost: return "Erfolg mit Kosten";
            case LocationOutcomeTier.PartialResult: return "Teilergebnis";
            case LocationOutcomeTier.Failure: return "Fehlschlag";
            case LocationOutcomeTier.SevereFailure: return "Schwerer Fehlschlag";
            default: return tier.ToString();
        }
    }
}

/// <summary>One weighted tier entry within a risk band's row of an outcome table.</summary>
public sealed class LocationOutcomeTierWeight
{
    public LocationOutcomeTierWeight(LocationOutcomeTier tier, int weight)
    {
        if (weight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), weight, "Outcome weight must not be negative.");
        }

        Tier = tier;
        Weight = weight;
    }

    public LocationOutcomeTier Tier { get; }

    public int Weight { get; }
}

/// <summary>
/// A band-weighted outcome table (concept Section 17.6). Authored per archetype + action; a weighted
/// tier is rolled from the row matching the resolved risk band, and the tier's effect bundle applies.
/// </summary>
public sealed class LocationOutcomeTableDefinition
{
    private readonly Dictionary<LocationRiskBand, List<LocationOutcomeTierWeight>> weightsByBand;
    private readonly Dictionary<LocationOutcomeTier, List<LocationEffectDefinition>> effectBundles;

    public LocationOutcomeTableDefinition(
        string id,
        IReadOnlyDictionary<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>> weightsByBand,
        IReadOnlyDictionary<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>> effectBundles,
        string? archetypeId = null,
        string? actionId = null)
    {
        Id = RequireText(id, nameof(id));
        ArchetypeId = string.IsNullOrWhiteSpace(archetypeId) ? null : archetypeId;
        ActionId = string.IsNullOrWhiteSpace(actionId) ? null : actionId;

        this.weightsByBand = new Dictionary<LocationRiskBand, List<LocationOutcomeTierWeight>>();
        foreach (var pair in weightsByBand ?? throw new ArgumentNullException(nameof(weightsByBand)))
        {
            this.weightsByBand[pair.Key] = new List<LocationOutcomeTierWeight>(pair.Value);
        }

        this.effectBundles = new Dictionary<LocationOutcomeTier, List<LocationEffectDefinition>>();
        foreach (var pair in effectBundles ?? throw new ArgumentNullException(nameof(effectBundles)))
        {
            this.effectBundles[pair.Key] = new List<LocationEffectDefinition>(pair.Value);
        }
    }

    public string Id { get; }

    public string? ArchetypeId { get; }

    public string? ActionId { get; }

    public IReadOnlyList<LocationOutcomeTierWeight> WeightsForBand(LocationRiskBand band)
    {
        if (weightsByBand.TryGetValue(band, out var weights))
        {
            return weights;
        }

        // No explicit row for this band: fall back to the nearest lower authored band (e.g. None -> Low
        // is handled by the ascending pass below), then the nearest higher one.
        for (var candidate = band - 1; candidate >= LocationRiskBand.None; candidate--)
        {
            if (weightsByBand.TryGetValue(candidate, out var lower))
            {
                return lower;
            }
        }

        for (var candidate = band + 1; candidate <= LocationRiskBand.Extreme; candidate++)
        {
            if (weightsByBand.TryGetValue(candidate, out var higher))
            {
                return higher;
            }
        }

        return Array.Empty<LocationOutcomeTierWeight>();
    }

    public IReadOnlyList<LocationEffectDefinition> EffectsForTier(LocationOutcomeTier tier)
    {
        return effectBundles.TryGetValue(tier, out var effects)
            ? effects
            : (IReadOnlyList<LocationEffectDefinition>)Array.Empty<LocationEffectDefinition>();
    }

    public bool HasEffectBundle(LocationOutcomeTier tier)
    {
        return effectBundles.ContainsKey(tier);
    }

    public IEnumerable<LocationRiskBand> Bands => weightsByBand.Keys;

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

/// <summary>Result of resolving an action's outcome table: the rolled tier plus its effect bundle.</summary>
public sealed class LocationOutcomeResolution
{
    public LocationOutcomeResolution(LocationOutcomeTier tier, IReadOnlyList<LocationEffectDefinition> effects)
    {
        Tier = tier;
        Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        Label = LocationOutcomeTiers.DisplayLabel(tier);
    }

    public LocationOutcomeTier Tier { get; }

    public string Label { get; }

    public IReadOnlyList<LocationEffectDefinition> Effects { get; }
}
}
