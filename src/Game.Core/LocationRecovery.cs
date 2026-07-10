namespace Game.Core
{

/// <summary>
/// Outcome of a Recovery Check (concept §9.9): failure does not automatically mean erasure. A
/// member/finding at stake on a Failure/SevereFailure gets a softer second roll.
/// </summary>
public enum LocationRecoveryOutcome
{
    /// <summary>The member/finding is kept despite the failure.</summary>
    Preserved,

    /// <summary>A degraded result — e.g. a member injured instead of lost.</summary>
    PartiallyPreserved,

    /// <summary>The full Failure/SevereFailure consequence applies.</summary>
    Lost
}
}
