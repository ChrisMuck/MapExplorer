#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>The persistent catalog of base upgrades and their built state.</summary>
public sealed class BaseUpgradesState
{
    private readonly List<BaseUpgradeState> upgrades = new();

    public BaseUpgradesState()
    {
    }

    public BaseUpgradesState(IEnumerable<BaseUpgradeState> catalog)
    {
        upgrades.AddRange(catalog ?? throw new ArgumentNullException(nameof(catalog)));
    }

    public IReadOnlyList<BaseUpgradeState> Upgrades => upgrades;

    public BaseUpgradeState? FindUpgrade(string id)
    {
        return upgrades.FirstOrDefault(upgrade => upgrade.Id == id);
    }

    public bool IsBuilt(string id)
    {
        var upgrade = FindUpgrade(id);
        return upgrade != null && upgrade.IsBuilt;
    }

    /// <summary>True if any built upgrade provides the given effect.</summary>
    public bool HasEffect(BaseUpgradeEffect effect)
    {
        return upgrades.Any(upgrade => upgrade.IsBuilt && upgrade.Effect == effect);
    }

    /// <summary>True if every prerequisite of the upgrade is already built.</summary>
    public bool PrerequisitesMet(BaseUpgradeState upgrade)
    {
        if (upgrade == null)
        {
            throw new ArgumentNullException(nameof(upgrade));
        }

        return upgrade.PrerequisiteIds.All(IsBuilt);
    }
}
}
