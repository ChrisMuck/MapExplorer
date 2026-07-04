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

    public ScoutReportState(
        string id,
        string missionId,
        string title,
        string body,
        int reliability,
        IEnumerable<HexCoord> relatedCoords,
        IEnumerable<string> hints)
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
        this.relatedCoords = new List<HexCoord>(relatedCoords ?? Enumerable.Empty<HexCoord>());
        this.hints = (hints ?? Enumerable.Empty<string>())
            .Where(hint => !string.IsNullOrWhiteSpace(hint))
            .Select(hint => hint.Trim())
            .ToList();
    }

    public string Id { get; }

    public string MissionId { get; }

    public string Title { get; }

    public string Body { get; }

    public int Reliability { get; }

    public IReadOnlyList<HexCoord> RelatedCoords
    {
        get { return relatedCoords; }
    }

    public IReadOnlyList<string> Hints
    {
        get { return hints; }
    }

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
