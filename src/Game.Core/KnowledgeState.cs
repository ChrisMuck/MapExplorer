using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class KnowledgeState
{
    private readonly Dictionary<HexCoord, KnowledgeLevel> tileKnowledge = new();

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

    public IReadOnlyDictionary<HexCoord, KnowledgeLevel> KnownTiles
    {
        get { return tileKnowledge; }
    }
}
}
