#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using WorldGen;

namespace Game.App
{

/// <summary>
/// Loads, writes and checks the rules that tell the world generator which special locations to place
/// (document type <c>location-placement</c>). The result is a plain <see cref="PlacementTable"/> the generator reads.
/// </summary>
public static class LocationPlacementLoader
{
    private const int SupportedSchemaVersion = 1;

    /// <summary>Generator archetype names a group may use; the bridge maps them to catalog archetypes.</summary>
    public static readonly IReadOnlyList<string> GeneratorArchetypes = new[]
    {
        "Wegehindernis", "Untersuchungsort", "Verwahrungsort", "Spurenort", "Ressourcenort", "Gefahrenzone", "Landmarke", "Grenzzeichen"
    };

    private static readonly Dictionary<string, PlacementCandidates> CandidateNames = Names(
        ("land", PlacementCandidates.Land), ("roadCellsOrLand", PlacementCandidates.RoadCellsOrLand), ("biomeCells", PlacementCandidates.BiomeCells),
        ("factionBorder", PlacementCandidates.FactionBorder), ("crossingEdges", PlacementCandidates.CrossingEdges));

    private static readonly Dictionary<string, PlacementCountKind> CountNames = Names(
        ("landDensity", PlacementCountKind.LandDensity), ("steps", PlacementCountKind.Steps),
        ("crossingsShare", PlacementCountKind.CrossingsShare), ("perFaction", PlacementCountKind.PerFaction));

    private static readonly Dictionary<string, PlacementTerm> TermNames = Names(
        ("constant", PlacementTerm.Constant), ("remote", PlacementTerm.Remote), ("elevation", PlacementTerm.Elevation), ("coastal", PlacementTerm.Coastal));

    private static readonly Dictionary<string, PlacementCondition> ConditionNames = Names(
        ("always", PlacementCondition.Always), ("nearWater", PlacementCondition.NearWater), ("forest", PlacementCondition.Forest),
        ("coastal", PlacementCondition.Coastal), ("riverEdge", PlacementCondition.RiverEdge));

    private static readonly Dictionary<string, AnchorKind> AnchorNames = Names(
        ("point", AnchorKind.Point), ("edge", AnchorKind.Edge), ("area", AnchorKind.Area));

    private static Dictionary<string, T> Names<T>(params (string Name, T Value)[] entries)
    {
        return entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
    }

    public static PlacementTable LoadFromJson(IEnumerable<string> documents)
    {
        if (documents == null) throw new ArgumentNullException(nameof(documents));
        var table = new PlacementTable();
        foreach (var json in documents)
        {
            JObject document;
            try
            {
                document = JObject.Parse(json);
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonException)
            {
                throw new LocationDataException("Location placement document is not valid JSON: " + ex.Message);
            }

            if ((string?)document["documentType"] != "location-placement" || (int?)document["schemaVersion"] != SupportedSchemaVersion)
            {
                throw new LocationDataException("Location placement documents require documentType 'location-placement' and schemaVersion 1.");
            }

            foreach (var item in Objects(document, "groups"))
            {
                table.Groups.Add(ParseGroup(item));
            }
        }

        Validate(table);
        return table;
    }

    public static string ToJson(PlacementTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        var groups = new JArray();
        foreach (var group in table.Groups)
        {
            var item = new JObject
            {
                ["id"] = group.Id,
                ["archetype"] = group.Archetype,
                ["count"] = CountToJson(group.Count),
                ["candidates"] = NameOf(CandidateNames, group.Candidates),
                ["anchor"] = NameOf(AnchorNames, group.Anchor)
            };
            if (group.Candidates == PlacementCandidates.BiomeCells) item["candidateBiome"] = group.CandidateBiome.ToString();
            if (group.Score.Count > 0)
            {
                item["score"] = new JArray(group.Score.Select(term => new JObject { ["term"] = NameOf(TermNames, term.Term), ["weight"] = term.Weight }));
            }

            if (group.ScoreTiers.Count > 0)
            {
                item["scoreTiers"] = new JArray(group.ScoreTiers.Select(tier => new JObject { ["when"] = NameOf(ConditionNames, tier.When), ["score"] = tier.Score }));
            }

            item["variantRules"] = new JArray(group.VariantRules.Select(RuleToJson));
            if (group.Ancient) item["ancient"] = true;
            if (group.Neutral) item["neutral"] = true;
            if (group.ForcedBiome.HasValue) item["forcedBiome"] = group.ForcedBiome.Value.ToString();
            if (group.ForcedBiomeChance.HasValue) item["forcedBiomeChance"] = group.ForcedBiomeChance.Value;
            if (group.AreaScale.HasValue) item["areaScale"] = group.AreaScale.Value;
            if (group.StopWhenExhausted) item["stopWhenExhausted"] = true;
            groups.Add(item);
        }

        var document = new JObject
        {
            ["documentType"] = "location-placement",
            ["schemaVersion"] = SupportedSchemaVersion,
            ["groups"] = groups
        };
        return document.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    private static JObject CountToJson(PlacementCount count)
    {
        var item = new JObject { ["kind"] = NameOf(CountNames, count.Kind) };
        switch (count.Kind)
        {
            case PlacementCountKind.LandDensity:
                item["factor"] = count.Factor;
                item["min"] = count.Min;
                break;
            case PlacementCountKind.CrossingsShare:
                item["factor"] = count.Factor;
                item["min"] = count.Min;
                item["max"] = count.Max;
                break;
            case PlacementCountKind.PerFaction:
                item["base"] = count.Base;
                item["extraWithTaboo"] = count.ExtraWithTaboo;
                break;
            default:
                item["base"] = count.Base;
                item["steps"] = new JArray(count.Steps.Select(step => new JObject { ["minLandCells"] = step.MinLandCells, ["add"] = step.Add }));
                break;
        }

        return item;
    }

    private static JObject RuleToJson(PlacementVariantRule rule)
    {
        var variants = new JArray();
        for (var i = 0; i < rule.Variants.Count; i++)
        {
            var variant = new JObject { ["name"] = rule.Variants[i] };
            if (i < rule.Weights.Count) variant["weight"] = rule.Weights[i];
            variants.Add(variant);
        }

        return new JObject { ["when"] = NameOf(ConditionNames, rule.When), ["variants"] = variants };
    }

    private static string NameOf<T>(Dictionary<string, T> names, T value)
    {
        return names.First(entry => EqualityComparer<T>.Default.Equals(entry.Value, value)).Key;
    }

    private static PlacementGroup ParseGroup(JObject item)
    {
        var group = new PlacementGroup
        {
            Id = Required(item, "id"),
            Archetype = Required(item, "archetype"),
            Count = ParseCount(item["count"] as JObject ?? throw new LocationDataException("Placement group '" + (string?)item["id"] + "' requires a count.")),
            Candidates = Lookup(CandidateNames, Required(item, "candidates"), "candidates"),
            Anchor = item["anchor"] == null ? AnchorKind.Point : Lookup(AnchorNames, Required(item, "anchor"), "anchor"),
            Ancient = (bool?)item["ancient"] ?? false,
            Neutral = (bool?)item["neutral"] ?? false,
            StopWhenExhausted = (bool?)item["stopWhenExhausted"] ?? false,
            ForcedBiomeChance = (double?)item["forcedBiomeChance"],
            AreaScale = (double?)item["areaScale"]
        };
        if (item["candidateBiome"] != null) group.CandidateBiome = ParseBiome(Required(item, "candidateBiome"));
        if (item["forcedBiome"] != null) group.ForcedBiome = ParseBiome(Required(item, "forcedBiome"));
        foreach (var term in Objects(item, "score"))
        {
            group.Score.Add(new PlacementScoreTerm(Lookup(TermNames, Required(term, "term"), "score term"), Number(term, "weight")));
        }

        foreach (var tier in Objects(item, "scoreTiers"))
        {
            group.ScoreTiers.Add(new PlacementScoreTier(Lookup(ConditionNames, Required(tier, "when"), "score tier condition"), Number(tier, "score")));
        }

        foreach (var rule in Objects(item, "variantRules"))
        {
            var names = new List<string>();
            var weights = new List<double>();
            var anyWeight = false;
            foreach (var variant in Objects(rule, "variants"))
            {
                names.Add(Required(variant, "name"));
                anyWeight |= variant["weight"] != null;
                weights.Add((double?)variant["weight"] ?? 1d);
            }

            var parsed = new PlacementVariantRule(Lookup(ConditionNames, Required(rule, "when"), "variant rule condition"), names.ToArray());
            if (anyWeight) parsed.Weights.AddRange(weights);
            group.VariantRules.Add(parsed);
        }

        return group;
    }

    private static PlacementCount ParseCount(JObject item)
    {
        var count = new PlacementCount { Kind = Lookup(CountNames, Required(item, "kind"), "count kind") };
        count.Factor = (double?)item["factor"] ?? 0d;
        count.Min = (int?)item["min"] ?? 0;
        count.Max = (int?)item["max"] ?? 0;
        count.Base = (int?)item["base"] ?? 0;
        count.ExtraWithTaboo = (int?)item["extraWithTaboo"] ?? 0;
        foreach (var step in Objects(item, "steps"))
        {
            count.Steps.Add(new PlacementStep((int?)step["minLandCells"] ?? throw new LocationDataException("A count step requires minLandCells."), (int?)step["add"] ?? 0));
        }

        return count;
    }

    private static Biome ParseBiome(string name)
    {
        if (!Enum.TryParse<Biome>(name, false, out var biome) || !Enum.IsDefined(typeof(Biome), biome))
        {
            throw new LocationDataException($"Location placement references unknown biome '{name}'.");
        }

        return biome;
    }

    private static T Lookup<T>(Dictionary<string, T> names, string name, string what)
    {
        if (!names.TryGetValue(name, out var value))
        {
            throw new LocationDataException($"Location placement has unknown {what} '{name}'. Known: {string.Join(", ", names.Keys)}.");
        }

        return value;
    }

    private static IEnumerable<JObject> Objects(JObject parent, string field)
    {
        return (parent[field] as JArray ?? new JArray()).OfType<JObject>();
    }

    private static string Required(JObject item, string field)
    {
        var value = (string?)item[field];
        if (string.IsNullOrWhiteSpace(value)) throw new LocationDataException($"Location placement field '{field}' is required.");
        return value!.Trim();
    }

    private static double Number(JObject item, string field)
    {
        var value = (double?)item[field];
        if (value == null) throw new LocationDataException($"Location placement field '{field}' is required.");
        return value.Value;
    }

    /// <summary>Checks that the table is complete and consistent; the generator trusts a validated table.</summary>
    public static void Validate(PlacementTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        var variantOwner = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var group in table.Groups)
        {
            string Fail(string message) => $"Placement group '{group.Id}' {message}";
            if (string.IsNullOrWhiteSpace(group.Id) || !groupIds.Add(group.Id)) throw new LocationDataException($"Placement group id '{group.Id}' is empty or used twice.");
            if (!GeneratorArchetypes.Contains(group.Archetype)) throw new LocationDataException(Fail($"uses unknown archetype '{group.Archetype}'."));
            ValidateCount(group, Fail);

            var isEdge = group.Candidates == PlacementCandidates.CrossingEdges;
            if (isEdge != (group.Anchor == AnchorKind.Edge)) throw new LocationDataException(Fail("must use the edge anchor exactly when it looks at crossing edges."));
            if (group.Candidates == PlacementCandidates.BiomeCells && !Enum.IsDefined(typeof(Biome), group.CandidateBiome)) throw new LocationDataException(Fail("needs a candidate biome."));
            if ((group.Candidates == PlacementCandidates.FactionBorder) != (group.Count.Kind == PlacementCountKind.PerFaction)) throw new LocationDataException(Fail("must count per faction exactly when it looks at faction borders."));
            if ((group.Count.Kind == PlacementCountKind.CrossingsShare) != isEdge) throw new LocationDataException(Fail("must count crossings exactly when it looks at crossing edges."));
            if (group.AreaScale.HasValue && (group.Anchor != AnchorKind.Area || group.AreaScale.Value <= 0 || double.IsNaN(group.AreaScale.Value))) throw new LocationDataException(Fail("sets an area scale without an area anchor or with a value that is not positive."));
            if (group.ForcedBiomeChance.HasValue && (!group.ForcedBiome.HasValue || group.ForcedBiomeChance.Value < 0 || group.ForcedBiomeChance.Value > 1)) throw new LocationDataException(Fail("sets a biome chance without a biome or outside 0 to 1."));
            if (!isEdge && group.Score.Count == 0 && group.ScoreTiers.Count == 0) throw new LocationDataException(Fail("needs a score or score tiers."));
            if (group.Score.Count > 0 && group.ScoreTiers.Count > 0) throw new LocationDataException(Fail("uses both a score and score tiers."));
            if (group.Score.Any(term => double.IsNaN(term.Weight) || double.IsInfinity(term.Weight)) || group.ScoreTiers.Any(tier => double.IsNaN(tier.Score) || double.IsInfinity(tier.Score)))
            {
                throw new LocationDataException(Fail("has a score value that is not a finite number."));
            }

            if (group.ScoreTiers.Count > 0 && group.ScoreTiers[group.ScoreTiers.Count - 1].When != PlacementCondition.Always) throw new LocationDataException(Fail("must end its score tiers with 'always'."));
            if (group.VariantRules.Count == 0) throw new LocationDataException(Fail("needs at least one variant rule."));
            if (group.VariantRules[group.VariantRules.Count - 1].When != PlacementCondition.Always) throw new LocationDataException(Fail("must end its variant rules with 'always', so every place gets a variant."));
            foreach (var rule in group.VariantRules)
            {
                if (rule.When == PlacementCondition.RiverEdge && !isEdge) throw new LocationDataException(Fail("uses 'riverEdge' outside an edge group."));
                if (rule.When != PlacementCondition.Always && rule.When != PlacementCondition.RiverEdge && isEdge) throw new LocationDataException(Fail("uses a cell condition in an edge group."));
                if (rule.Variants.Count == 0) throw new LocationDataException(Fail("has a variant rule without variants."));
                if (rule.Weights.Count != 0 && rule.Weights.Count != rule.Variants.Count) throw new LocationDataException(Fail("gives weights for only some variants of a rule."));
                if (rule.Weights.Any(weight => !(weight > 0) || double.IsInfinity(weight))) throw new LocationDataException(Fail("has a variant weight that is not a positive number."));
                foreach (var name in rule.Variants)
                {
                    if (string.IsNullOrWhiteSpace(name)) throw new LocationDataException(Fail("has an empty variant name."));
                    if (variantOwner.TryGetValue(name, out var owner) && owner != group.Id)
                    {
                        throw new LocationDataException($"Variant '{name}' belongs to two placement groups ('{owner}' and '{group.Id}'); a variant has exactly one group.");
                    }

                    variantOwner[name] = group.Id;
                }
            }
        }
    }

    private static void ValidateCount(PlacementGroup group, Func<string, string> fail)
    {
        var count = group.Count;
        bool Bad(double value) => double.IsNaN(value) || double.IsInfinity(value) || value < 0;
        switch (count.Kind)
        {
            case PlacementCountKind.LandDensity:
                if (Bad(count.Factor) || count.Factor == 0 || count.Min < 0) throw new LocationDataException(fail("needs a positive factor and a minimum of 0 or more."));
                break;
            case PlacementCountKind.CrossingsShare:
                if (Bad(count.Factor) || count.Factor == 0 || count.Min < 0 || count.Max < count.Min) throw new LocationDataException(fail("needs a positive factor and 0 <= min <= max."));
                break;
            case PlacementCountKind.PerFaction:
                if (count.Base < 0 || count.ExtraWithTaboo < 0) throw new LocationDataException(fail("needs base and extra counts of 0 or more."));
                break;
            default:
                if (count.Base < 0 || count.Steps.Any(step => step.MinLandCells < 0 || step.Add < 0)) throw new LocationDataException(fail("needs a base and steps of 0 or more."));
                break;
        }
    }
}

}
