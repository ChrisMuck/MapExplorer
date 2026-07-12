namespace WorldGen
{
    /// <summary>Full 16-biome list (+ ocean/lake), design §7.</summary>
    public enum Biome
    {
        Plains = 0, Forest, DeepForest, Jungle, Desert, Swamp, Mountains, Highlands,
        Coast, Riverlands, Tundra, Volcanic, Wasteland, Ruins, SacredGroves, DeadZones,
        Ocean, Lake
    }

    public readonly struct BiomeDef
    {
        public readonly string Name;
        public readonly float Move, Vis, Hazard;
        public BiomeDef(string name, float move, float vis, float hazard)
        { Name = name; Move = move; Vis = vis; Hazard = hazard; }
    }

    /// <summary>
    /// Derived hex data per biome (movement cost, visibility, hazard baseline — §7.5).
    /// Indexed by (int)Biome.
    /// </summary>
    public static class Biomes
    {
        public static readonly BiomeDef[] Defs =
        {
            new BiomeDef("Ebene",              1, 1.2f, 0.05f),
            new BiomeDef("Wald",               2, 0.7f, 0.10f),
            new BiomeDef("Dichter Wald",       3, 0.5f, 0.15f),
            new BiomeDef("Dschungel",          3, 0.4f, 0.25f),
            new BiomeDef("Wüste",              2, 1.4f, 0.25f),
            new BiomeDef("Sumpf",              3, 0.6f, 0.30f),
            new BiomeDef("Berge",              4, 1.3f, 0.40f),
            new BiomeDef("Hochland",           2, 1.1f, 0.15f),
            new BiomeDef("Küste",              1, 1.3f, 0.05f),
            new BiomeDef("Flussland",          1, 1.0f, 0.05f),
            new BiomeDef("Tundra",             2, 1.3f, 0.20f),
            new BiomeDef("Vulkanland",         4, 1.0f, 0.55f),
            new BiomeDef("Ödland",             3, 1.2f, 0.30f),
            new BiomeDef("Überwucherte Ruinen",2, 0.8f, 0.20f),
            new BiomeDef("Heilige Haine",      2, 0.8f, 0.10f),
            new BiomeDef("Tote Zonen",         3, 1.0f, 0.50f),
            new BiomeDef("Ozean",             99, 1.5f, 0.10f),
            new BiomeDef("See",               99, 1.4f, 0.05f),
        };

        public static BiomeDef Def(int b) => Defs[b];
        public static string Name(int b) => Defs[b].Name;
    }
}
