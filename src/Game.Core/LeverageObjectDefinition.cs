#nullable enable
using System;

namespace Game.Core
{

public sealed class LeverageObjectDefinition
{
    public LeverageObjectDefinition(
        string itemId,
        string displayName,
        string source,
        string interestedFactionId,
        string unlocksOfferId,
        string persistenceRule)
    {
        ItemId = RequireText(itemId, nameof(itemId));
        DisplayName = RequireText(displayName, nameof(displayName));
        Source = RequireText(source, nameof(source));
        InterestedFactionId = RequireText(interestedFactionId, nameof(interestedFactionId));
        UnlocksOfferId = RequireText(unlocksOfferId, nameof(unlocksOfferId));
        PersistenceRule = RequireText(persistenceRule, nameof(persistenceRule));
    }

    public string ItemId { get; }

    public string DisplayName { get; }

    public string Source { get; }

    public string InterestedFactionId { get; }

    public string UnlocksOfferId { get; }

    public string PersistenceRule { get; }

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
