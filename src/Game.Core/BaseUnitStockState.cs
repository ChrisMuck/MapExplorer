#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>
/// The base's stock of generic units (porters and soldiers). The pool grows through base upgrades
/// (Trägerunterkünfte / Baracken) and the player draws a departure loadout from it.
/// </summary>
public sealed class BaseUnitStockState
{
    private readonly List<BaseUnitState> units = new();
    private int counter;

    public BaseUnitStockState()
    {
    }

    public BaseUnitStockState(IEnumerable<BaseUnitState> initialUnits)
    {
        foreach (var unit in initialUnits ?? throw new ArgumentNullException(nameof(initialUnits)))
        {
            units.Add(unit);
            counter++;
        }
    }

    public IReadOnlyList<BaseUnitState> Units => units;

    public IReadOnlyList<BaseUnitState> Porters => units.Where(u => u.Kind == BaseUnitKind.Porter).ToList();

    public IReadOnlyList<BaseUnitState> Soldiers => units.Where(u => u.Kind == BaseUnitKind.Soldier).ToList();

    public BaseUnitState? FindUnit(string id)
    {
        return units.FirstOrDefault(u => u.Id == id);
    }

    /// <summary>Adds units of a kind with stable unique ids and returns them.</summary>
    public IReadOnlyList<BaseUnitState> Add(BaseUnitKind kind, int count, bool exhausted = false)
    {
        var added = new List<BaseUnitState>();
        for (var i = 0; i < count; i++)
        {
            counter++;
            var unit = new BaseUnitState($"{(kind == BaseUnitKind.Porter ? "porter" : "soldier")}-{counter}", kind, exhausted);
            units.Add(unit);
            added.Add(unit);
        }

        return added;
    }
}
}
