#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public sealed class LocationInteractionQueryResult
{
    private LocationInteractionQueryResult(bool success, LocationInteractionModel? interaction, string? error)
    {
        Success = success;
        Interaction = interaction;
        Error = error;
    }

    public bool Success { get; }

    public LocationInteractionModel? Interaction { get; }

    public string? Error { get; }

    public static LocationInteractionQueryResult Found(LocationInteractionModel interaction)
    {
        return new LocationInteractionQueryResult(true, interaction ?? throw new ArgumentNullException(nameof(interaction)), null);
    }

    public static LocationInteractionQueryResult Rejected(string error)
    {
        return new LocationInteractionQueryResult(false, null, error);
    }
}

public sealed class LocationActionResult
{
    private LocationActionResult(
        bool success,
        SpecialLocationState? location,
        LocationActionDefinition? action,
        LocationOutcomeTier? resolvedTier,
        string? outcomeLabel,
        IReadOnlyList<string> effectTexts,
        bool expeditionMoved,
        string? error)
    {
        Success = success;
        Location = location;
        Action = action;
        ResolvedTier = resolvedTier;
        OutcomeLabel = outcomeLabel;
        EffectTexts = effectTexts;
        ExpeditionMoved = expeditionMoved;
        Error = error;
    }

    public bool Success { get; }

    public SpecialLocationState? Location { get; }

    public LocationActionDefinition? Action { get; }

    /// <summary>The rolled outcome tier, or null for project start/advance (no tier roll).</summary>
    public LocationOutcomeTier? ResolvedTier { get; }

    /// <summary>Display label for the resolved tier, or null when there was no tier roll.</summary>
    public string? OutcomeLabel { get; }

    public IReadOnlyList<string> EffectTexts { get; }

    public bool ExpeditionMoved { get; }

    public string? Error { get; }

    public static LocationActionResult Resolved(
        SpecialLocationState location,
        LocationActionDefinition action,
        LocationOutcomeTier? resolvedTier,
        string? outcomeLabel,
        IReadOnlyList<string> effectTexts,
        bool expeditionMoved = false)
    {
        return new LocationActionResult(true, location, action, resolvedTier, outcomeLabel, effectTexts, expeditionMoved, null);
    }

    public static LocationActionResult Rejected(string error)
    {
        return new LocationActionResult(false, null, null, null, null, Array.Empty<string>(), false, error);
    }
}
}
