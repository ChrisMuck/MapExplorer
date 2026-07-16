#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public sealed class LocationInteractionQueryResult
{
    private LocationInteractionQueryResult(bool success, LocationInteractionModel? interaction, LocationInteractionPresentation? presentation, string? error)
    {
        Success = success;
        Interaction = interaction;
        Presentation = presentation;
        Error = error;
    }

    public bool Success { get; }

    public LocationInteractionModel? Interaction { get; }

    /// <summary>Player-facing location wording derived only from authored presentation and KnowledgeState.</summary>
    public LocationInteractionPresentation? Presentation { get; }

    public string? Error { get; }

    public static LocationInteractionQueryResult Found(LocationInteractionModel interaction, LocationInteractionPresentation? presentation = null)
    {
        return new LocationInteractionQueryResult(true, interaction ?? throw new ArgumentNullException(nameof(interaction)), presentation, null);
    }

    public static LocationInteractionQueryResult Rejected(string error)
    {
        return new LocationInteractionQueryResult(false, null, null, error);
    }
}

public sealed class LocationInteractionPresentation
{
    public LocationInteractionPresentation(string title, string subtitle, string description, string? imageId,
        string knowledgeLabel, string interactionStateText, string operationalStateText, string presenceStateText,
        IReadOnlyList<string> knownContextTags, SceneDescriptionResult? scene = null)
    {
        Title = title;
        Subtitle = subtitle;
        Description = description;
        ImageId = imageId;
        KnowledgeLabel = knowledgeLabel;
        InteractionStateText = interactionStateText;
        OperationalStateText = operationalStateText;
        PresenceStateText = presenceStateText;
        KnownContextTags = knownContextTags;
        Scene = scene;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public string Description { get; }
    public string? ImageId { get; }
    public string KnowledgeLabel { get; }
    public string InteractionStateText { get; }
    public string OperationalStateText { get; }
    public string PresenceStateText { get; }
    public IReadOnlyList<string> KnownContextTags { get; }
    public SceneDescriptionResult? Scene { get; }
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
