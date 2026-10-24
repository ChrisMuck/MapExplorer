using System.Text;
using WorldGen;

/// <summary>
/// Pins the special locations the generator places for a set of seeds. The placement rules moved from fixed code
/// into a table (see docs/world_generator_location_data_concept.md); this test proves that the table reproduces the
/// worlds the code produced before. Set GOLDEN_PRINT=1 to print new digests after an intended change.
/// </summary>
public sealed class WorldGeneratorPlacementTests
{
    private static readonly (uint Seed, int Width, int Height, int Factions, float Density, ulong Digest)[] Cases =
    {
        (1, 30, 22, 2, 1f, 9139863368484710840UL),
        (2, 30, 22, 2, 1f, 9288769582117132201UL),
        (3, 30, 22, 2, 1f, 16822815878788822310UL),
        (4, 30, 22, 2, 2.2f, 1153370282581511743UL),
        (5, 30, 22, 2, 2.2f, 8943433430194035776UL),
        (6, 30, 22, 2, 2.2f, 6418369991646689204UL),
        (7, 30, 22, 2, 3.5f, 7169472613980322445UL),
        (8, 30, 22, 2, 3.5f, 9788931839870662058UL),
        (9, 30, 22, 2, 3.5f, 7286350936037984101UL),
        (10, 30, 22, 3, 1f, 16805859670465637056UL),
        (11, 30, 22, 3, 1f, 5639984472610455092UL),
        (12, 30, 22, 3, 1f, 15707821042070110326UL),
        (13, 30, 22, 3, 2.2f, 17773540256904346859UL),
        (14, 30, 22, 3, 2.2f, 10201870297590528510UL),
        (15, 30, 22, 3, 2.2f, 10449681951242429631UL),
        (16, 30, 22, 3, 3.5f, 5134058508139569689UL),
        (17, 30, 22, 3, 3.5f, 6816541923661004243UL),
        (18, 30, 22, 3, 3.5f, 3087386028471001381UL),
        (19, 30, 22, 4, 1f, 12221480798172070051UL),
        (20, 30, 22, 4, 1f, 13594343307610592917UL),
        (21, 30, 22, 4, 1f, 16234174224597301204UL),
        (22, 30, 22, 4, 2.2f, 14706336356948754295UL),
        (23, 30, 22, 4, 2.2f, 299013353889620368UL),
        (24, 30, 22, 4, 2.2f, 17478693094550713241UL),
        (25, 30, 22, 4, 3.5f, 17225062741014352636UL),
        (26, 30, 22, 4, 3.5f, 17401045813209688639UL),
        (27, 30, 22, 4, 3.5f, 8702892948833663232UL),
        (28, 40, 30, 2, 1f, 2036986735721558645UL),
        (29, 40, 30, 2, 1f, 3758425479306898670UL),
        (30, 40, 30, 2, 1f, 1887761276517521447UL),
        (31, 40, 30, 2, 2.2f, 4030274286651693158UL),
        (32, 40, 30, 2, 2.2f, 17974662550805545794UL),
        (33, 40, 30, 2, 2.2f, 15603737550473451278UL),
        (34, 40, 30, 2, 3.5f, 4745047509317852991UL),
        (35, 40, 30, 2, 3.5f, 14240301005519381700UL),
        (36, 40, 30, 2, 3.5f, 4155164084192326812UL),
        (37, 40, 30, 3, 1f, 17172538987546852372UL),
        (38, 40, 30, 3, 1f, 14255364264531068769UL),
        (39, 40, 30, 3, 1f, 11853765550309206663UL),
        (40, 40, 30, 3, 2.2f, 7289737674142935877UL),
        (41, 40, 30, 3, 2.2f, 8464073902560254020UL),
        (42, 40, 30, 3, 2.2f, 16950728032874093488UL),
        (43, 40, 30, 3, 3.5f, 17968885961768364954UL),
        (44, 40, 30, 3, 3.5f, 17320846225926749540UL),
        (45, 40, 30, 3, 3.5f, 6169593788579558080UL),
        (46, 40, 30, 4, 1f, 541907944870211084UL),
        (47, 40, 30, 4, 1f, 16160185370573692702UL),
        (48, 40, 30, 4, 1f, 4898907134094773512UL),
        (49, 40, 30, 4, 2.2f, 5959648652202703626UL),
        (50, 40, 30, 4, 2.2f, 13798740570421650359UL),
        (51, 40, 30, 4, 2.2f, 5114508678424496134UL),
        (52, 40, 30, 4, 3.5f, 13704470781445485788UL),
        (53, 40, 30, 4, 3.5f, 7017900615385769981UL),
        (54, 40, 30, 4, 3.5f, 10114597161594763414UL),
        (55, 60, 45, 2, 1f, 17699847583990076917UL),
        (56, 60, 45, 2, 1f, 18179127434983150506UL),
        (57, 60, 45, 2, 1f, 17240507704376164854UL),
        (58, 60, 45, 2, 2.2f, 758475371243370661UL),
        (59, 60, 45, 2, 2.2f, 8818661115707967170UL),
        (60, 60, 45, 2, 2.2f, 14449388235479345678UL),
        (61, 60, 45, 2, 3.5f, 16731514418876863137UL),
        (62, 60, 45, 2, 3.5f, 614491068168256841UL),
        (63, 60, 45, 2, 3.5f, 15299438605980767993UL),
        (64, 60, 45, 3, 1f, 10464883563931716237UL),
        (65, 60, 45, 3, 1f, 4802219904895254778UL),
        (66, 60, 45, 3, 1f, 8665685201618031497UL),
        (67, 60, 45, 3, 2.2f, 15320063597934142758UL),
        (68, 60, 45, 3, 2.2f, 9424351320249632816UL),
        (69, 60, 45, 3, 2.2f, 12752887258365666447UL),
        (70, 60, 45, 3, 3.5f, 953626903442434690UL),
        (71, 60, 45, 3, 3.5f, 12702921020949641553UL),
        (72, 60, 45, 3, 3.5f, 2083371619805276421UL),
        (73, 60, 45, 4, 1f, 4915480797519166668UL),
        (74, 60, 45, 4, 1f, 570378468313834151UL),
        (75, 60, 45, 4, 1f, 17288003866645411781UL),
        (76, 60, 45, 4, 2.2f, 6362052906010756094UL),
        (77, 60, 45, 4, 2.2f, 16799496739282193767UL),
        (78, 60, 45, 4, 2.2f, 17713615408424987152UL),
        (79, 60, 45, 4, 3.5f, 1357431928757859107UL),
        (80, 60, 45, 4, 3.5f, 3649094488937203096UL),
        (81, 60, 45, 4, 3.5f, 7743987036146532UL),
        (82, 90, 68, 2, 1f, 15914186214568720534UL),
        (83, 90, 68, 2, 1f, 13031570187065192367UL),
        (84, 90, 68, 2, 1f, 8355605431612092886UL),
        (85, 90, 68, 2, 2.2f, 4901385557459324029UL),
        (86, 90, 68, 2, 2.2f, 5015336401516028148UL),
        (87, 90, 68, 2, 2.2f, 14778603907024799249UL),
        (88, 90, 68, 2, 3.5f, 10937875463489388834UL),
        (89, 90, 68, 2, 3.5f, 8893548640136890005UL),
        (90, 90, 68, 2, 3.5f, 3128450986691878672UL),
        (91, 90, 68, 3, 1f, 13860832523541287061UL),
        (92, 90, 68, 3, 1f, 10834702268230905469UL),
        (93, 90, 68, 3, 1f, 6308787915655819728UL),
        (94, 90, 68, 3, 2.2f, 11084606252762239630UL),
        (95, 90, 68, 3, 2.2f, 9245101749291690520UL),
        (96, 90, 68, 3, 2.2f, 12045819551066453514UL),
        (97, 90, 68, 3, 3.5f, 8670052619727021387UL),
        (98, 90, 68, 3, 3.5f, 3072406339354387408UL),
        (99, 90, 68, 3, 3.5f, 2550375221142903260UL),
        (100, 90, 68, 4, 1f, 4011568217777852135UL),
        (101, 90, 68, 4, 1f, 7392197136679307028UL),
        (102, 90, 68, 4, 1f, 17679070539820758661UL),
        (103, 90, 68, 4, 2.2f, 9484575510784490116UL),
        (104, 90, 68, 4, 2.2f, 3815678271216318002UL),
        (105, 90, 68, 4, 2.2f, 3166219951232570452UL),
        (106, 90, 68, 4, 3.5f, 11315176998585692205UL),
        (107, 90, 68, 4, 3.5f, 8376278969259024305UL),
        (108, 90, 68, 4, 3.5f, 15704071949197913498UL),
    };

    // The generator names its locations by catalog variant id now; the pinned digests were taken with the old display names.
    private static readonly Dictionary<string, string> LegacyNames = new()
    {
        ["broken-bridge"] = "Zerstörte Brücke",
        ["blocked-pass"] = "Verschütteter Pass",
        ["old-ruin"] = "Alte Ruine",
        ["marked-grave"] = "Vergessenes Grab",
        ["stone-circle"] = "Steinkreis",
        ["ruined-shrine"] = "Verfallener Schrein",
        ["sealed-gate"] = "Versiegeltes Tor",
        ["sealed-cave"] = "Verschlossene Höhle",
        ["warded-threshold"] = "Gebannte Schwelle",
        ["abandoned-camp"] = "Verlassenes Lager",
        ["cold-fireplace"] = "Kalte Feuerstelle",
        ["broken-cart"] = "Zerbrochener Karren",
        ["old-boat-camp"] = "Altes Bootslager",
        ["freshwater-spring"] = "Süßwasserquelle",
        ["fishing-grounds"] = "Fischgrund",
        ["healing-herbs"] = "Heilkräuter",
        ["timber-stand"] = "Holzbestand",
        ["salt-deposit"] = "Salzvorkommen",
        ["berry-grove"] = "Beerenhain",
        ["restless-marsh"] = "Giftiger Sumpf",
        ["volcanic-vent"] = "Vulkanschlund",
        ["dead-zone"] = "Tote Zone",
        ["signal-tower"] = "Signalturm",
        ["wind-carved-peak"] = "Gipfelmal",
        ["colossus-statue"] = "Kolossstatue",
        ["old-lighthouse"] = "Alter Leuchtturm",
        ["border-stones"] = "Grenzsteine",
        ["border-warning-sign"] = "Warnzeichen",
        ["skull-marks"] = "Schädelmarken",
        ["wind-ribbons"] = "Bänder im Wind"
    };

    public void RunAll()
    {
        var print = Environment.GetEnvironmentVariable("GOLDEN_PRINT") == "1";
        var index = 0;
        foreach (var testCase in BuildCases())
        {
            var digest = DigestFor(testCase.Seed, testCase.Width, testCase.Height, testCase.Factions, testCase.Density);
            if (print)
            {
                Console.WriteLine($"        ({testCase.Seed}, {testCase.Width}, {testCase.Height}, {testCase.Factions}, {testCase.Density.ToString(System.Globalization.CultureInfo.InvariantCulture)}f, {digest}UL),");
            }
            else if (index < Cases.Length && Cases[index].Digest != digest)
            {
                throw new InvalidOperationException($"Generator placement changed for seed {testCase.Seed} ({testCase.Width}x{testCase.Height}, {testCase.Factions} factions, density {testCase.Density}): expected {Cases[index].Digest}, got {digest}.");
            }

            index++;
        }

        if (!print && index != Cases.Length)
        {
            throw new InvalidOperationException($"Expected {Cases.Length} generator placement cases but ran {index}.");
        }
    }

    public static IEnumerable<(uint Seed, int Width, int Height, int Factions, float Density)> BuildCases()
    {
        var sizes = new[] { (30, 22), (40, 30), (60, 45), (90, 68) };
        var factionCounts = new[] { 2, 3, 4 };
        var densities = new[] { 1.0f, 2.2f, 3.5f };
        uint seed = 1;
        foreach (var size in sizes)
        {
            foreach (var factions in factionCounts)
            {
                foreach (var density in densities)
                {
                    for (var repeat = 0; repeat < 3; repeat++)
                    {
                        yield return (seed++, size.Item1, size.Item2, factions, density);
                    }
                }
            }
        }
    }

    public static ulong DigestFor(uint seed, int width, int height, int factions, float density, PlacementTable? placement = null)
    {
        var parameters = new GenerationParams
        {
            Seed = seed,
            MapWidth = width,
            MapHeight = height,
            FactionCount = factions,
            LocationDensity = density,
            Placement = placement
        };
        var world = new WorldGenerator().Generate(parameters);
        var text = new StringBuilder();
        foreach (var special in world.Specials)
        {
            text.Append(special.Cell.I).Append('|').Append(special.Archetype).Append('|').Append(LegacyNames.TryGetValue(special.Variant, out var legacy) ? legacy : special.Variant)
                .Append('|').Append(special.Owner).Append('|').Append(special.WatchedBy).Append('|').Append(special.Ancient)
                .Append('|').Append(string.Join(",", special.Modifiers)).Append('|').Append(string.Join(",", special.ContextTags))
                .Append('|').Append(string.Join(",", special.EvidenceSeedIds.Where(id => !id.StartsWith("evidence-site-", StringComparison.Ordinal)))).Append('|').Append(special.Anchor.Kind);
            foreach (var cell in special.Anchor.Cells)
            {
                text.Append(':').Append(cell.I).Append('b').Append(cell.Biome);
            }

            text.Append('\n');
        }

        text.Append("cells:");
        foreach (var cell in world.Grid.Cells)
        {
            text.Append(cell.Biome).Append(cell.Special == null ? "-" : LegacyNames.TryGetValue(cell.Special, out var legacy) ? legacy : cell.Special).Append(';');
        }

        ulong hash = 14695981039346656037UL;
        foreach (var b in Encoding.UTF8.GetBytes(text.ToString()))
        {
            hash = (hash ^ b) * 1099511628211UL;
        }

        return hash;
    }
}
