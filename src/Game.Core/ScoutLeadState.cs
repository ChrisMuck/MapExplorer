#nullable enable
using System;

namespace Game.Core
{

/// <summary>Player-facing search scope of a scout lead. It deliberately carries no hex coordinate.</summary>
public enum ScoutLeadScope
{
    Local,
    Directional
}

/// <summary>Neutral category of an uncertain scout observation.</summary>
public enum ScoutLeadKind
{
    LocationSighting,
    RouteHint,
    HazardIndication,
    FactionSignature,
    WitnessTrace,
    EnvironmentalChange,
    LocalContext
}

/// <summary>
/// An earned but approximate player-facing lead. Exact pathing and target coordinates remain
/// transient simulation data and are never written into KnowledgeState.
/// </summary>
public sealed class ScoutLeadState
{
    public ScoutLeadState(ScoutLeadKind kind, ScoutLeadScope scope, ScoutDirection direction, int confidence, string summary, string? symbolId = null, string? sourceLocationId = null)
    {
        if (confidence < 0 || confidence > 100) throw new ArgumentOutOfRangeException(nameof(confidence));
        if (string.IsNullOrWhiteSpace(summary)) throw new ArgumentException("Lead summary must not be empty.", nameof(summary));
        Kind = kind;
        Scope = scope;
        Direction = direction;
        Confidence = confidence;
        Summary = summary.Trim();
        SymbolId = string.IsNullOrWhiteSpace(symbolId) ? null : symbolId.Trim();
        SourceLocationId = string.IsNullOrWhiteSpace(sourceLocationId) ? null : sourceLocationId.Trim();
    }

    public ScoutLeadKind Kind { get; }
    public ScoutLeadScope Scope { get; }
    public ScoutDirection Direction { get; }
    public int Confidence { get; }
    public string Summary { get; }
    /// <summary>Stable recurring sign, if the expedition has observed one; never an owning faction ID.</summary>
    public string? SymbolId { get; }
    public string? SourceLocationId { get; }
}
}
