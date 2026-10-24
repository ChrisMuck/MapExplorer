using System.Collections.Generic;

namespace WorldGen
{
    /// <summary>Where a placement group looks for cells. The generator builds the sets; the table only picks one.</summary>
    public enum PlacementCandidates
    {
        /// <summary>Every land cell.</summary>
        Land,
        /// <summary>Land cells on a road, or every land cell when the world has no road cell left.</summary>
        RoadCellsOrLand,
        /// <summary>Free land cells of one biome (see <see cref="PlacementGroup.CandidateBiome"/>); the group is skipped when there are none.</summary>
        BiomeCells,
        /// <summary>Free cells on the border of each faction's territory; the group runs once per faction.</summary>
        FactionBorder,
        /// <summary>Road edges that cross a river or a steep slope; the location sits on the edge.</summary>
        CrossingEdges
    }

    public enum PlacementCountKind
    {
        /// <summary>land cells / 350 x Factor x location density, at least Min.</summary>
        LandDensity,
        /// <summary>Base plus Add for every step whose land size is reached.</summary>
        Steps,
        /// <summary>Round(crossing edges x Factor), clamped to Min and Max.</summary>
        CrossingsShare,
        /// <summary>Per faction: min(border cells, Base plus ExtraWithTaboo when the faction has more than one taboo).</summary>
        PerFaction
    }

    public sealed class PlacementStep
    {
        public PlacementStep(int minLandCells, int add)
        {
            MinLandCells = minLandCells;
            Add = add;
        }

        public int MinLandCells { get; }
        public int Add { get; }
    }

    public sealed class PlacementCount
    {
        public PlacementCountKind Kind { get; set; }
        public double Factor { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int Base { get; set; }
        public int ExtraWithTaboo { get; set; }
        public List<PlacementStep> Steps { get; set; } = new List<PlacementStep>();
    }

    /// <summary>Quantities the generator measures for a cell; a score is a weighted sum of them.</summary>
    public enum PlacementTerm
    {
        Constant,
        /// <summary>How far the cell lies from roads and settlements (0 near, 1 far).</summary>
        Remote,
        /// <summary>Elevation relative to the highest point.</summary>
        Elevation,
        /// <summary>1 on a coast, else 0.</summary>
        Coastal
    }

    public sealed class PlacementScoreTerm
    {
        public PlacementScoreTerm(PlacementTerm term, double weight)
        {
            Term = term;
            Weight = weight;
        }

        public PlacementTerm Term { get; }
        public double Weight { get; }
    }

    /// <summary>Facts about a cell (or an edge) that a rule can ask for.</summary>
    public enum PlacementCondition
    {
        Always,
        NearWater,
        Forest,
        Coastal,
        RiverEdge
    }

    /// <summary>The first tier whose condition holds gives the score of a cell.</summary>
    public sealed class PlacementScoreTier
    {
        public PlacementScoreTier(PlacementCondition when, double score)
        {
            When = when;
            Score = score;
        }

        public PlacementCondition When { get; }
        public double Score { get; }
    }

    /// <summary>The first rule whose condition holds names the variants to draw from; one variant needs no draw.</summary>
    public sealed class PlacementVariantRule
    {
        public PlacementVariantRule(PlacementCondition when, params string[] variants)
        {
            When = when;
            Variants = new List<string>(variants);
        }

        public PlacementCondition When { get; }
        public List<string> Variants { get; }
        /// <summary>Draw weights, one per variant; empty (or all equal) means every variant is equally likely.</summary>
        public List<double> Weights { get; } = new List<double>();
    }

    /// <summary>One kind of special location: how many, where, and which variants. A variant belongs to exactly one group.</summary>
    public sealed class PlacementGroup
    {
        public string Id { get; set; } = "";
        /// <summary>Generator archetype name, for example "Spurenort".</summary>
        public string Archetype { get; set; } = "";
        public PlacementCount Count { get; set; } = new PlacementCount();
        public PlacementCandidates Candidates { get; set; } = PlacementCandidates.Land;
        public Biome CandidateBiome { get; set; }
        /// <summary>A cell scores the sum of its terms, or the first matching tier when there are tiers.</summary>
        public List<PlacementScoreTerm> Score { get; set; } = new List<PlacementScoreTerm>();
        public List<PlacementScoreTier> ScoreTiers { get; set; } = new List<PlacementScoreTier>();
        public List<PlacementVariantRule> VariantRules { get; set; } = new List<PlacementVariantRule>();
        public bool Ancient { get; set; }
        /// <summary>Neutral locations are never claimed by a faction on their own.</summary>
        public bool Neutral { get; set; }
        public AnchorKind Anchor { get; set; } = AnchorKind.Point;
        /// <summary>Biome the location stamps onto its cell or area.</summary>
        public Biome? ForcedBiome { get; set; }
        /// <summary>Chance to stamp the biome; null means always (and no random draw).</summary>
        public double? ForcedBiomeChance { get; set; }
        /// <summary>Area groups: size as a multiple of the world's area size; null when the anchor is an empty area.</summary>
        public double? AreaScale { get; set; }
        /// <summary>True when the group ends as soon as no candidate cell is left, instead of drawing on.</summary>
        public bool StopWhenExhausted { get; set; }
    }

    /// <summary>
    /// The rules for placing special locations. The generator reads them in order. <see cref="Default"/> reproduces the
    /// rules the generator had in code before they became data.
    /// </summary>
    public sealed class PlacementTable
    {
        public List<PlacementGroup> Groups { get; set; } = new List<PlacementGroup>();

        public static PlacementTable Default()
        {
            PlacementCount Density(double factor) => new PlacementCount { Kind = PlacementCountKind.LandDensity, Factor = factor, Min = 1 };

            return new PlacementTable
            {
                Groups =
                {
                    new PlacementGroup
                    {
                        Id = "placement-route-obstacles",
                        Archetype = "Wegehindernis",
                        Count = new PlacementCount { Kind = PlacementCountKind.CrossingsShare, Factor = 0.4, Min = 1, Max = 6 },
                        Candidates = PlacementCandidates.CrossingEdges,
                        Anchor = AnchorKind.Edge,
                        Ancient = true,
                        VariantRules =
                        {
                            new PlacementVariantRule(PlacementCondition.RiverEdge, "Zerstörte Brücke"),
                            new PlacementVariantRule(PlacementCondition.Always, "Verschütteter Pass")
                        }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-investigation-sites",
                        Archetype = "Untersuchungsort",
                        Count = Density(1.1),
                        Score = { new PlacementScoreTerm(PlacementTerm.Remote, 1.3) },
                        Ancient = true,
                        ForcedBiome = Biome.Ruins,
                        ForcedBiomeChance = 0.5,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Alte Ruine", "Vergessenes Grab", "Steinkreis", "Verfallener Schrein") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-containment-sites",
                        Archetype = "Verwahrungsort",
                        Count = Density(0.45),
                        Score = { new PlacementScoreTerm(PlacementTerm.Remote, 0.7), new PlacementScoreTerm(PlacementTerm.Elevation, 0.8) },
                        Ancient = true,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Versiegeltes Tor", "Verschlossene Höhle", "Gebannte Schwelle") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-trace-sites",
                        Archetype = "Spurenort",
                        Count = Density(1.0),
                        Candidates = PlacementCandidates.RoadCellsOrLand,
                        Score = { new PlacementScoreTerm(PlacementTerm.Constant, 1), new PlacementScoreTerm(PlacementTerm.Remote, -0.5) },
                        Neutral = true,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Verlassenes Lager", "Kalte Feuerstelle", "Zerbrochener Karren", "Altes Bootslager") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-resource-sites",
                        Archetype = "Ressourcenort",
                        Count = Density(1.1),
                        StopWhenExhausted = true,
                        ScoreTiers =
                        {
                            new PlacementScoreTier(PlacementCondition.NearWater, 1.0),
                            new PlacementScoreTier(PlacementCondition.Forest, 0.7),
                            new PlacementScoreTier(PlacementCondition.Coastal, 0.6),
                            new PlacementScoreTier(PlacementCondition.Always, 0.1)
                        },
                        VariantRules =
                        {
                            new PlacementVariantRule(PlacementCondition.NearWater, "Süßwasserquelle", "Fischgrund"),
                            new PlacementVariantRule(PlacementCondition.Forest, "Heilkräuter", "Holzbestand"),
                            new PlacementVariantRule(PlacementCondition.Coastal, "Salzvorkommen"),
                            new PlacementVariantRule(PlacementCondition.Always, "Beerenhain")
                        }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-hazard-swamp",
                        Archetype = "Gefahrenzone",
                        Count = new PlacementCount { Kind = PlacementCountKind.Steps, Base = 1 },
                        Candidates = PlacementCandidates.BiomeCells,
                        CandidateBiome = Biome.Swamp,
                        Score = { new PlacementScoreTerm(PlacementTerm.Constant, 1) },
                        Neutral = true,
                        Anchor = AnchorKind.Area,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Giftiger Sumpf") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-hazard-volcano",
                        Archetype = "Gefahrenzone",
                        Count = new PlacementCount { Kind = PlacementCountKind.Steps, Base = 1, Steps = { new PlacementStep(6000, 1) } },
                        Score = { new PlacementScoreTerm(PlacementTerm.Elevation, 0.8), new PlacementScoreTerm(PlacementTerm.Remote, 0.4) },
                        Neutral = true,
                        Anchor = AnchorKind.Area,
                        ForcedBiome = Biome.Volcanic,
                        AreaScale = 1.0,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Vulkanschlund") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-hazard-dead-zone",
                        Archetype = "Gefahrenzone",
                        Count = new PlacementCount { Kind = PlacementCountKind.Steps, Base = 0, Steps = { new PlacementStep(1200, 1), new PlacementStep(7000, 1) } },
                        Score = { new PlacementScoreTerm(PlacementTerm.Remote, 1) },
                        Neutral = true,
                        Anchor = AnchorKind.Area,
                        ForcedBiome = Biome.DeadZones,
                        AreaScale = 0.7,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Tote Zone") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-landmarks",
                        Archetype = "Landmarke",
                        Count = Density(0.7),
                        Score = { new PlacementScoreTerm(PlacementTerm.Elevation, 1), new PlacementScoreTerm(PlacementTerm.Coastal, 0.3) },
                        Ancient = true,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Signalturm", "Gipfelmal", "Kolossstatue", "Alter Leuchtturm") }
                    },
                    new PlacementGroup
                    {
                        Id = "placement-territorial-markers",
                        Archetype = "Grenzzeichen",
                        Count = new PlacementCount { Kind = PlacementCountKind.PerFaction, Base = 1, ExtraWithTaboo = 1 },
                        Candidates = PlacementCandidates.FactionBorder,
                        Score = { new PlacementScoreTerm(PlacementTerm.Constant, 1) },
                        StopWhenExhausted = true,
                        VariantRules = { new PlacementVariantRule(PlacementCondition.Always, "Grenzsteine", "Warnzeichen", "Schädelmarken", "Bänder im Wind") }
                    }
                }
            };
        }
    }
}
