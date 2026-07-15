#nullable enable
using System;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Resolves player-facing inspection wording from the shared content profile. It deliberately
/// reads no generated context, faction truth or hidden consequences.
/// </summary>
public sealed class LocationInspectionPresentationResolver
{
    private readonly LocationInteractionDefinitionSet definitions;

    public LocationInspectionPresentationResolver(LocationInteractionDefinitionSet definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    public LocationInspectionPresentation? Resolve(SpecialLocationState location)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        var profile = definitions.FindContentProfile(location.ContentProfileId);
        if (profile == null) return null;

        var message = profile.FlavorForState(location.OperationalStateId)
            ?? profile.Description
            ?? profile.ShortDescription
            ?? $"{profile.Title} wurde dokumentiert.";
        var journalText = profile.JournalDiscovered ?? message;
        return new LocationInspectionPresentation(profile.Title, message, journalText);
    }
}

public sealed class LocationInspectionPresentation
{
    public LocationInspectionPresentation(string title, string message, string journalText)
    {
        Title = RequireText(title, nameof(title));
        Message = RequireText(message, nameof(message));
        JournalText = RequireText(journalText, nameof(journalText));
    }

    public string Title { get; }
    public string Message { get; }
    public string JournalText { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}
}
