#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{

public sealed class LeverageInventoryState
{
    private readonly HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);

    public LeverageInventoryState(IEnumerable<string>? itemIds = null)
    {
        if (itemIds == null)
        {
            return;
        }

        foreach (var itemId in itemIds)
        {
            Add(itemId);
        }
    }

    public IReadOnlyCollection<string> ItemIds
    {
        get { return itemIds; }
    }

    public bool Add(string itemId)
    {
        return itemIds.Add(RequireText(itemId, nameof(itemId)));
    }

    public bool Contains(string itemId)
    {
        return itemIds.Contains(RequireText(itemId, nameof(itemId)));
    }

    public bool Consume(string itemId)
    {
        return itemIds.Remove(RequireText(itemId, nameof(itemId)));
    }

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
