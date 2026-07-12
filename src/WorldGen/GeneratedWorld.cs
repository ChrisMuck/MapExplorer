using System.Collections.Generic;

namespace WorldGen
{
    /// <summary>Post-generation statistics + validation warnings (design §7.1, §15.1).</summary>
    public sealed class WorldStats
    {
        public int Land, Total;
        public float LandPct;
        public float ShapeIndex;                        // coastline irregularity (§4 Revision)
        public Dictionary<int, int> BiomeCounts = new Dictionary<int, int>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// The full generator output: immutable terrain layer + mutable world-state
    /// layer, all as logic data (design §14). No meshes, no rendering.
    /// </summary>
    public sealed class GeneratedWorld
    {
        public GenerationParams Params;
        public HexGrid Grid;

        public List<Faction> Factions = new List<Faction>();
        public HexCell Base;                            // expedition arrival camp
        public HexCell MysteryCore;
        public List<List<HexCell>> Roads = new List<List<HexCell>>();
        public HashSet<string> RoadEdges = new HashSet<string>();
        public List<River> Rivers = new List<River>();
        public List<SpecialLocation> Specials = new List<SpecialLocation>();

        public float ElevMax, ElevMin;
        public float MinSpacing;
        public WorldStats Stats = new WorldStats();
    }
}
