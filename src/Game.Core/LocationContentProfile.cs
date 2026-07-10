#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{

/// <summary>
/// Authored presentation content for a location instance or variant (concept Section 17.7). Supplies
/// title, flavor and journal text; it must not define gameplay rules — those live in the archetype,
/// actions, modifiers and outcome tables.
/// </summary>
public sealed class LocationContentProfileDefinition
{
    private readonly Dictionary<string, string> flavorByState;

    public LocationContentProfileDefinition(
        string id,
        string title,
        string? subtitle = null,
        string? shortDescription = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? flavorByState = null,
        string? imageId = null,
        string? journalDiscovered = null,
        string? journalResolved = null)
    {
        Id = RequireText(id, nameof(id));
        Title = RequireText(title, nameof(title));
        Subtitle = Normalize(subtitle);
        ShortDescription = Normalize(shortDescription);
        Description = Normalize(description);
        ImageId = Normalize(imageId);
        JournalDiscovered = Normalize(journalDiscovered);
        JournalResolved = Normalize(journalResolved);

        this.flavorByState = new Dictionary<string, string>();
        if (flavorByState != null)
        {
            foreach (var pair in flavorByState)
            {
                this.flavorByState[pair.Key] = pair.Value;
            }
        }
    }

    public string Id { get; }

    public string Title { get; }

    public string? Subtitle { get; }

    public string? ShortDescription { get; }

    public string? Description { get; }

    public string? ImageId { get; }

    public string? JournalDiscovered { get; }

    public string? JournalResolved { get; }

    public IReadOnlyDictionary<string, string> FlavorByState => flavorByState;

    /// <summary>Flavor text for an operational (or other) state id, falling back to the description.</summary>
    public string? FlavorForState(string? stateId)
    {
        if (!string.IsNullOrWhiteSpace(stateId) && flavorByState.TryGetValue(stateId!, out var flavor))
        {
            return flavor;
        }

        return Description ?? ShortDescription;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
}
