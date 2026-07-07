using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>
/// Derived readiness of a planned departure loadout (members + units + rations + medicine).
/// Computed here in Core so the UI only displays it and never re-derives the rules.
/// </summary>
public sealed class ExpeditionReadiness
{
    private ExpeditionReadiness(int heads, int carryCapacity, int load, bool overload, int foodDays, int defense, bool slowMarch)
    {
        Heads = heads;
        CarryCapacity = carryCapacity;
        Load = load;
        Overload = overload;
        FoodDays = foodDays;
        Defense = defense;
        SlowMarch = slowMarch;
    }

    public int Heads { get; }

    public int CarryCapacity { get; }

    public int Load { get; }

    public bool Overload { get; }

    public int FoodDays { get; }

    public int Defense { get; }

    public bool SlowMarch { get; }

    public static ExpeditionReadiness Compute(int memberCount, IReadOnlyList<BaseUnitState> selectedUnits, int rations, int medicine)
    {
        var units = selectedUnits ?? new List<BaseUnitState>();
        var porters = units.Where(u => u.Kind == BaseUnitKind.Porter).ToList();
        var soldiers = units.Where(u => u.Kind == BaseUnitKind.Soldier).ToList();

        var capacity = memberCount * 2;
        capacity += porters.Sum(p => p.IsExhausted ? 4 : 6);
        capacity += soldiers.Count * 2;

        var load = rations + medicine;
        var heads = memberCount + porters.Count + soldiers.Count;
        var foodDays = heads > 0 ? rations / heads : 0;
        var defense = soldiers.Sum(s => s.IsExhausted ? 2 : 3);
        var slowMarch = units.Any(u => u.IsExhausted);

        return new ExpeditionReadiness(heads, capacity, load, load > capacity, foodDays, defense, slowMarch);
    }
}
}
