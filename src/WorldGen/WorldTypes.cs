using System.Collections.Generic;

namespace WorldGen
{
    /// <summary>Faction Data record (design §7A). Combinatorial trait axes + naming.</summary>
    public sealed class Faction
    {
        public int Id;
        public string Color;                 // optional (rendering aid only)
        public string Attitude;              // "welcoming" / "neutral-cautious" / "hostile"
        public string Strictness, Aggression, Communication, Leadership, Origin;
        public List<string> Values = new List<string>();
        public List<string> Taboo = new List<string>();
        public int PreferredBiome;
        public int TabooBiome = -1;

        public HexCell Seed;                 // capital location
        public List<HexCell> Towns = new List<HexCell>();

        public NamingStyle Style;            // assigned World Naming Style
        public string StyleId;
        public string Name;                  // faction / people name
    }

    public enum AnchorKind { Point, Edge, Area }

    public sealed class Anchor
    {
        public AnchorKind Kind;
        public List<HexCell> Cells;
        public Anchor(AnchorKind kind, List<HexCell> cells) { Kind = kind; Cells = cells; }
    }

    /// <summary>
    /// A placed special location (design §9 + Location Archetypes §18). Placement
    /// is terrain-driven; ownership is derived from territory containment.
    /// </summary>
    public sealed class SpecialLocation
    {
        public HexCell Cell;
        public string Archetype;             // e.g. "Wegehindernis", "Untersuchungsort"
        public string Variant;               // e.g. "Zerstörte Brücke"
        public int Owner = -1;               // faction id if FactionOwned, else -1
        public int WatchedBy = -1;           // faction whose territory it sits in (neutral-in-origin)
        public List<string> Modifiers = new List<string>();
        public List<string> ContextTags = new List<string>();
        public List<string> EvidenceSeedIds = new List<string>();
        public bool Ancient;
        public Anchor Anchor;
    }

    /// <summary>A named connected river system (fulfils the §6 stable-ID requirement).</summary>
    public sealed class River
    {
        public string Name;
        public List<HexCell> Cells;
        public HexCell Mouth;
        public int Faction = -1;
    }
}
