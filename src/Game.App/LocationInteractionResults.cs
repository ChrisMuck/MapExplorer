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
        LocationOutcomeDefinition? outcome,
        IReadOnlyList<string> effectTexts,
        string? error)
    {
        Success = success;
        Location = location;
        Action = action;
        Outcome = outcome;
        EffectTexts = effectTexts;
        Error = error;
    }

    public bool Success { get; }

    public SpecialLocationState? Location { get; }

    public LocationActionDefinition? Action { get; }

    public LocationOutcomeDefinition? Outcome { get; }

    public IReadOnlyList<string> EffectTexts { get; }

    public string? Error { get; }

    public static LocationActionResult Resolved(
        SpecialLocationState location,
        LocationActionDefinition action,
        LocationOutcomeDefinition? outcome,
        IReadOnlyList<string> effectTexts)
    {
        return new LocationActionResult(true, location, action, outcome, effectTexts, null);
    }

    public static LocationActionResult Rejected(string error)
    {
        return new LocationActionResult(false, null, null, null, Array.Empty<string>(), error);
    }
}
}
