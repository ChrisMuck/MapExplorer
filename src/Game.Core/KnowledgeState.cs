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
