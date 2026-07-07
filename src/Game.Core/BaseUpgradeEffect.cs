namespace Game.Core
{

/// <summary>
/// The mechanical effect a built base upgrade has. Most catalog entries are structural hooks for
/// later systems (porter/soldier stock, scout range, supply cap) and use <see cref="None"/> until
/// those systems land; <see cref="CheaperHealing"/> is wired into base healing today.
/// </summary>
public enum BaseUpgradeEffect
{
    None,
    CheaperHealing,
    GrowPorterStock,
    GrowSoldierStock,
    BetterScoutReports,
    ScoutRangePlus,
    HigherSupplyCap,
    SlowerSpoilage
}
}
