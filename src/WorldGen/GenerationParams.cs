namespace WorldGen
{
    /// <summary>
    /// All generator inputs. Defaults match the web prototype's defaults. Two are
    /// "free per campaign" (map size, faction count); the rest are tuning knobs
    /// (design §1). Wind direction is a hex-direction index 0..5 (NE,E,SE,SW,W,NW).
    /// </summary>
    public sealed class GenerationParams
    {
        // --- Map / factions (player-facing) ---
        public uint Seed = 42;
        public int MapWidth = 40;
        public int MapHeight = 30;
        public int FactionCount = 3;
        public string FactionMood = "Gemischt";   // "Friedlich" / "Gemischt" / "Feindselig"

        // --- Sub-seed salts (editor per-layer re-roll, §2.1) ---
        public int FactionSalt = 0;
        public int LocationSalt = 0;

        // --- Landmass & elevation (§4) ---
        public int LandPercentage = 50;
        public float ElevationMaximum = 8f;
        public float NoiseScale = 16f;            // continent scale (feature size)
        public int NoiseOctaves = 5;
        public float WarpStrength = 1.2f;         // coastline irregularity
        public float IslandFalloff = 0.6f;        // radial island mask strength
        public float RidgeStrength = 0.6f;        // mountain-range (ridge) strength
        public float MountainAmount = 0.5f;       // share of land that becomes mountains/highlands (§7.2)
        public int ErosionPercentage = 50;
        public int MapBorderX = 4;
        public int MapBorderZ = 4;
        public int BiomeSmooth = 1;               // biome coherence passes (§7.6)

        // --- Climate (§5) ---
        public float StartingMoisture = 0.1f;
        public float EvaporationFactor = 0.5f;
        public float PrecipitationFactor = 0.25f;
        public float RunoffFactor = 0.25f;
        public float SeepageFactor = 0.125f;
        public int WindDirection = 5;             // NW
        public float WindStrength = 4f;

        // --- Rivers & temperature (§6, Appendix A.5) ---
        public int RiverPercentage = 10;
        public float ExtraLakeProbability = 0.25f;
        public float LowTemperature = 0f;
        public float HighTemperature = 1f;
        public string Hemisphere = "Both";        // "Both" / "North" / "South"
        public float TemperatureJitter = 0.1f;

        // --- Biome bands (§7.1) ---
        public float[] TemperatureBands = { 0.1f, 0.3f, 0.6f };
        public float[] MoistureBands = { 0.12f, 0.28f, 0.85f };

        // --- Factions & territory (§7A/§9) ---
        public float TerritoryReach = 0.5f;       // fraction of each faction's max extent
        public int CellsPerTown = 30;
        public int MaxTowns = 5;
        public int TownSpacing = 2;
        public float RoadDirectness = 3f;
        public float LocationDensity = 2.2f;

        public GenerationParams Clone()
        {
            var c = (GenerationParams)MemberwiseClone();
            c.TemperatureBands = (float[])TemperatureBands.Clone();
            c.MoistureBands = (float[])MoistureBands.Clone();
            return c;
        }
    }
}
