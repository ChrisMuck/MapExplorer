#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{

/// <summary>
/// A single persistent base upgrade. Costs Knowledge Points, may require other upgrades first, and
/// once built stays built across expeditions.
/// </summary>
public sealed class BaseUpgradeState
{
    private readonly List<string> prerequisiteIds = new();

    public BaseUpgradeState(
        string id,
        string category,
        string name,
        string description,
        int cost,
        BaseUpgradeEffect effect = BaseUpgradeEffect.None,
        IEnumerable<string>? prerequisiteIds = null,
        bool isBuilt = false)
    {
        Id = RequireText(id, nameof(id));
        Category = RequireText(category, nameof(category));
        Name = RequireText(name, nameof(name));
        Description = description ?? string.Empty;
        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "Upgrade cost must not be negative.");
        }

        Cost = cost;
        Effect = effect;
        IsBuilt = isBuilt;
        if (prerequisiteIds != null)
        {
            this.prerequisiteIds.AddRange(prerequisiteIds);
        }
    }

    public string Id { get; }

    public string Category { get; }

    public string Name { get; }

    public string Description { get; }

    public int Cost { get; }

    public BaseUpgradeEffect Effect { get; }

    public IReadOnlyList<string> PrerequisiteIds => prerequisiteIds;

    public bool IsBuilt { get; private set; }

    public void MarkBuilt()
    {
        IsBuilt = true;
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
