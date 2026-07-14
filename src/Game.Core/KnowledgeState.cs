using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class KnowledgeState
{
    private readonly Dictionary<HexCoord, KnowledgeLevel> tileKnowledge = new();
    private readonly List<ScoutReportState> scoutReports = new();
    private readonly List<EvidenceState> evidence = new();
    private readonly HashSet<string> claimedKnowledgeSources = new();
    private readonly Dictionary<string, HashSet<string>> knownLocationContextTags = new(StringComparer.Ordinal);

    public KnowledgeLevel GetTileKnowledge(HexCoord coord)
    {
        return tileKnowledge.TryGetValue(coord, out var knowledge)
            ? knowledge
            : KnowledgeLevel.Unknown;
    }

    public void SetTileKnowledge(HexCoord coord, KnowledgeLevel knowledge)
    {
        if (knowledge == KnowledgeLevel.Unknown)
        {
            tileKnowledge.Remove(coord);
            return;
        }

        tileKnowledge[coord] = knowledge;
    }

    public bool PromoteTileKnowledge(HexCoord coord, KnowledgeLevel knowledge)
    {
        var current = GetTileKnowledge(coord);
        if (KnowledgeRank(knowledge) <= KnowledgeRank(current))
        {
            return false;
        }

        SetTileKnowledge(coord, knowledge);
        return true;
    }

    public IReadOnlyDictionary<HexCoord, KnowledgeLevel> KnownTiles
    {
        get { return tileKnowledge; }
    }

    public IReadOnlyList<ScoutReportState> ScoutReports
    {
        get { return scoutReports; }
    }

    public IReadOnlyList<EvidenceState> Evidence
    {
        get { return evidence; }
    }

    public bool ClaimKnowledgeSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Knowledge source id must not be empty.", nameof(sourceId));
        }

        return claimedKnowledgeSources.Add(sourceId);
    }

    public bool HasClaimedKnowledgeSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("Knowledge source id must not be empty.", nameof(sourceId));
        }

        return claimedKnowledgeSources.Contains(sourceId);
    }

    public void AddScoutReport(ScoutReportState report)
    {
        scoutReports.Add(report ?? throw new ArgumentNullException(nameof(report)));
    }

    public bool AddEvidence(EvidenceState item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (evidence.Any(existing => existing.Id == item.Id))
        {
            return false;
        }

        evidence.Add(item);
        return true;
    }

    /// <summary>
    /// Records a context conclusion that the expedition has actually earned for one location.
    /// It deliberately does not mirror generated WorldState context tags.
    /// </summary>
    public bool LearnLocationContextTag(string locationId, string contextTag)
    {
        if (string.IsNullOrWhiteSpace(locationId)) throw new ArgumentException("Location id must not be empty.", nameof(locationId));
        if (string.IsNullOrWhiteSpace(contextTag)) throw new ArgumentException("Context tag must not be empty.", nameof(contextTag));
        if (!knownLocationContextTags.TryGetValue(locationId.Trim(), out var tags))
        {
            tags = new HashSet<string>(StringComparer.Ordinal);
            knownLocationContextTags.Add(locationId.Trim(), tags);
        }

        return tags.Add(contextTag.Trim());
    }

    public bool KnowsLocationContextTag(string locationId, string contextTag)
    {
        return !string.IsNullOrWhiteSpace(locationId)
            && !string.IsNullOrWhiteSpace(contextTag)
            && knownLocationContextTags.TryGetValue(locationId.Trim(), out var tags)
            && tags.Contains(contextTag.Trim());
    }

    public IReadOnlyCollection<string> KnownLocationContextTags(string locationId)
    {
        return !string.IsNullOrWhiteSpace(locationId)
            && knownLocationContextTags.TryGetValue(locationId.Trim(), out var tags)
            ? tags.ToArray()
            : Array.Empty<string>();
    }

    public EvidenceState? FindEvidence(string evidenceId)
    {
        foreach (var item in evidence)
        {
            if (item.Id == evidenceId)
            {
                return item;
            }
        }

        return null;
    }

    public void Clear()
    {
        tileKnowledge.Clear();
        scoutReports.Clear();
        evidence.Clear();
        claimedKnowledgeSources.Clear();
        knownLocationContextTags.Clear();
    }

    private static int KnowledgeRank(KnowledgeLevel knowledge)
    {
        switch (knowledge)
        {
            case KnowledgeLevel.Unknown:
                return 0;
            case KnowledgeLevel.OldOrDoubtful:
                return 1;
            case KnowledgeLevel.Reported:
                return 2;
            case KnowledgeLevel.Confirmed:
                return 3;
            default:
                throw new ArgumentOutOfRangeException(nameof(knowledge), knowledge, "Unknown knowledge level.");
        }
    }
}
}
