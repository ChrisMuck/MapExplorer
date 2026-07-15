#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class ScoutReportState
{
    private readonly List<HexCoord> relatedCoords;
    private readonly List<string> hints;
    private readonly List<ScoutLeadState> leads;

    public ScoutReportState(
        string id,
        string missionId,
        string title,
        string body,
        int reliability,
        IEnumerable<HexCoord> relatedCoords,
        IEnumerable<string> hints,
        IEnumerable<ScoutLeadState>? leads = null)
    {
        if (reliability < 0 || reliability > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(reliability), reliability, "Reliability must be between 0 and 100.");
        }

        Id = RequireText(id, nameof(id));
        MissionId = RequireText(missionId, nameof(missionId));
        Title = RequireText(title, nameof(title));
        Body = RequireText(body, nameof(body));
        Reliability = reliability;
        // The old constructor shape accepts the former internal corridor for source
        // compatibility, but player knowledge must never retain exact scout coordinates.
        _ = relatedCoords ?? Enumerable.Empty<HexCoord>();
        this.relatedCoords = new List<HexCoord>();
        this.hints = (hints ?? Enumerable.Empty<string>())
            .Where(hint => !string.IsNullOrWhiteSpace(hint))
            .Select(hint => hint.Trim())
            .ToList();
        this.leads = new List<ScoutLeadState>(leads ?? Enumerable.Empty<ScoutLeadState>());
    }

    public string Id { get; }

    public string MissionId { get; }

    public string Title { get; }

    public string Body { get; }

    public int Reliability { get; }

    /// <summary>Always empty for newly resolved reports; use <see cref="Leads"/> for player-facing information.</summary>
    [Obsolete("Scout reports no longer reveal exact coordinates. Use Leads instead.")]
    public IReadOnlyList<HexCoord> RelatedCoords
    {
        get { return relatedCoords; }
    }

    public IReadOnlyList<string> Hints
    {
        get { return hints; }
    }

    public IReadOnlyList<ScoutLeadState> Leads => leads;

    public bool HasExactCoordinates => false;

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value.Trim();
    }
}
}
