#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.App
{

/// <summary>A plain 3-component vector so presentation data does not depend on Unity types.</summary>
public readonly struct VisualVec3 : IEquatable<VisualVec3>
{
    public VisualVec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public static VisualVec3 Zero => new(0f, 0f, 0f);
    public static VisualVec3 One => new(1f, 1f, 1f);

    public bool Equals(VisualVec3 other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is VisualVec3 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.###}, {1:0.###}, {2:0.###})", X, Y, Z);
}

public enum VisualShape
{
    Box,
    Prism,
    Ico,
    Dome
}

/// <summary>One low-poly primitive of a part, positioned relative to the part origin.</summary>
public sealed class VisualPrimitiveDefinition
{
    public VisualPrimitiveDefinition(VisualShape shape, VisualVec3 position, VisualVec3 size, VisualVec3 euler, string colorHex, int sides, float topScale, bool emissive)
    {
        Shape = shape;
        Position = position;
        Size = size;
        Euler = euler;
        ColorHex = colorHex;
        Sides = sides;
        TopScale = topScale;
        Emissive = emissive;
    }

    public VisualShape Shape { get; }
    public VisualVec3 Position { get; }
    public VisualVec3 Size { get; }
    public VisualVec3 Euler { get; }
    public string ColorHex { get; }
    /// <summary>Side count of a prism; ignored for other shapes.</summary>
    public int Sides { get; }
    /// <summary>Top radius relative to the base for a prism (0 gives a cone).</summary>
    public float TopScale { get; }
    public bool Emissive { get; }
}

/// <summary>A reusable building block of the visual kit. Geometry is placeholder-quality until a better asset replaces it.</summary>
public sealed class VisualPartDefinition
{
    public VisualPartDefinition(string id, string category, string sizeClass, string quality, IEnumerable<VisualPrimitiveDefinition> primitives)
    {
        Id = id;
        Category = category;
        SizeClass = sizeClass;
        Quality = quality;
        Primitives = primitives.ToList();
    }

    public string Id { get; }
    public string Category { get; }
    public string SizeClass { get; }
    public string Quality { get; }
    public IReadOnlyList<VisualPrimitiveDefinition> Primitives { get; }
}

/// <summary>Pose override of an element while a state channel holds a given value.</summary>
public sealed class VisualPoseDefinition
{
    public VisualPoseDefinition(VisualVec3 position, VisualVec3 euler, VisualVec3 scale)
    {
        Position = position;
        Euler = euler;
        Scale = scale;
    }

    public static VisualPoseDefinition Neutral { get; } = new(VisualVec3.Zero, VisualVec3.Zero, VisualVec3.One);

    public VisualVec3 Position { get; }
    public VisualVec3 Euler { get; }
    public VisualVec3 Scale { get; }
}

/// <summary>A part placed in a composition, visible in some states of one state channel and posed per state.</summary>
public sealed class VisualElementDefinition
{
    public VisualElementDefinition(
        string id,
        string partId,
        VisualVec3 position,
        VisualVec3 euler,
        string? showChannel,
        IEnumerable<string>? showStates,
        string poseChannel,
        IReadOnlyDictionary<string, VisualPoseDefinition> poses)
    {
        Id = id;
        PartId = partId;
        Position = position;
        Euler = euler;
        ShowChannel = showChannel;
        ShowStates = showStates?.ToList();
        PoseChannel = poseChannel;
        Poses = poses;
    }

    public string Id { get; }
    public string PartId { get; }
    public VisualVec3 Position { get; }
    public VisualVec3 Euler { get; }
    /// <summary>State channel that decides visibility; null means always visible.</summary>
    public string? ShowChannel { get; }
    public IReadOnlyList<string>? ShowStates { get; }
    public string PoseChannel { get; }
    public IReadOnlyDictionary<string, VisualPoseDefinition> Poses { get; }
}

/// <summary>How a location looks: chosen by archetype and optionally variant, most specific first.</summary>
public sealed class VisualCompositionDefinition
{
    public VisualCompositionDefinition(string id, string archetypeId, string? variantId, string anchorMode, float ringRadius, IEnumerable<VisualElementDefinition> elements)
    {
        Id = id;
        ArchetypeId = archetypeId;
        VariantId = variantId;
        AnchorMode = anchorMode;
        RingRadius = ringRadius;
        Elements = elements.ToList();
    }

    public string Id { get; }
    public string ArchetypeId { get; }
    public string? VariantId { get; }
    public string AnchorMode { get; }
    public float RingRadius { get; }
    public IReadOnlyList<VisualElementDefinition> Elements { get; }
}

/// <summary>Archetype marker ring: a color plus a dash pattern, so color is never the only cue.</summary>
public sealed class VisualMarkerDefinition
{
    public VisualMarkerDefinition(string archetypeId, string colorHex, int ringGaps)
    {
        ArchetypeId = archetypeId;
        ColorHex = colorHex;
        RingGaps = ringGaps;
    }

    public string ArchetypeId { get; }
    public string ColorHex { get; }
    public int RingGaps { get; }
}

public sealed class VisualDefinitionSet
{
    public const string DefaultMarkerId = "*";
    public const string UnknownState = "unknown";

    private readonly Dictionary<string, VisualPartDefinition> parts;
    private readonly Dictionary<string, VisualMarkerDefinition> markers;

    public VisualDefinitionSet(
        IEnumerable<VisualPartDefinition> parts,
        IEnumerable<VisualCompositionDefinition> compositions,
        IEnumerable<VisualMarkerDefinition> markers)
    {
        try
        {
            this.parts = parts.ToDictionary(part => part.Id, StringComparer.Ordinal);
            Compositions = compositions.ToList();
            this.markers = markers.ToDictionary(marker => marker.ArchetypeId, StringComparer.Ordinal);
        }
        catch (ArgumentException)
        {
            throw new LocationDataException("Visual part ids and marker archetype ids must be unique.");
        }

        if (Compositions.Select(composition => composition.Id).Distinct(StringComparer.Ordinal).Count() != Compositions.Count)
        {
            throw new LocationDataException("Visual composition ids must be unique.");
        }

        foreach (var composition in Compositions)
        {
            if (composition.Elements.Select(element => element.Id).Distinct(StringComparer.Ordinal).Count() != composition.Elements.Count)
            {
                throw new LocationDataException($"Visual composition '{composition.Id}' has duplicate element ids.");
            }

            foreach (var element in composition.Elements)
            {
                if (!this.parts.ContainsKey(element.PartId))
                {
                    throw new LocationDataException($"Visual composition '{composition.Id}' element '{element.Id}' uses unknown part '{element.PartId}'.");
                }
            }
        }
    }

    public static VisualDefinitionSet Empty { get; } = new(
        Array.Empty<VisualPartDefinition>(),
        Array.Empty<VisualCompositionDefinition>(),
        Array.Empty<VisualMarkerDefinition>());

    public IReadOnlyCollection<VisualPartDefinition> Parts => parts.Values;
    public IReadOnlyList<VisualCompositionDefinition> Compositions { get; }
    public IReadOnlyCollection<VisualMarkerDefinition> Markers => markers.Values;

    public VisualPartDefinition? FindPart(string id) => parts.TryGetValue(id, out var part) ? part : null;

    /// <summary>Marker for the archetype, else the default marker, else a neutral built-in one.</summary>
    public VisualMarkerDefinition FindMarker(string? archetypeId)
    {
        if (archetypeId != null && markers.TryGetValue(archetypeId, out var exact)) return exact;
        return markers.TryGetValue(DefaultMarkerId, out var fallback) ? fallback : new VisualMarkerDefinition(DefaultMarkerId, "#C8B88A", 6);
    }

    /// <summary>Most specific match first: variant composition, then archetype composition, else null.</summary>
    public VisualCompositionDefinition? FindComposition(string? archetypeId, string? variantId)
    {
        if (archetypeId == null) return null;
        VisualCompositionDefinition? archetypeMatch = null;
        foreach (var composition in Compositions)
        {
            if (composition.ArchetypeId != archetypeId) continue;
            if (composition.VariantId == null)
            {
                archetypeMatch ??= composition;
            }
            else if (composition.VariantId == variantId)
            {
                return composition;
            }
        }

        return archetypeMatch;
    }
}

public static class VisualDefinitionLoader
{
    private const int SupportedSchemaVersion = 1;

    public static VisualDefinitionSet LoadFromJson(IEnumerable<string> documents)
    {
        if (documents == null) throw new ArgumentNullException(nameof(documents));
        var parts = new List<VisualPartDefinition>();
        var compositions = new List<VisualCompositionDefinition>();
        var markers = new List<VisualMarkerDefinition>();
        foreach (var json in documents)
        {
            JObject document;
            try
            {
                document = JObject.Parse(json);
            }
            catch (Exception ex) when (ex is Newtonsoft.Json.JsonException)
            {
                throw new LocationDataException("Visual definition document is not valid JSON: " + ex.Message);
            }

            if ((string?)document["documentType"] != "visual-definitions" || (int?)document["schemaVersion"] != SupportedSchemaVersion)
            {
                throw new LocationDataException("Visual definition documents require documentType 'visual-definitions' and schemaVersion 1.");
            }

            foreach (var item in Objects(document, "markers"))
            {
                markers.Add(new VisualMarkerDefinition(Required(item, "archetypeId"), Color(item, "color"), (int?)item["ringGaps"] ?? 0));
            }

            foreach (var item in Objects(document, "parts"))
            {
                parts.Add(ParsePart(item));
            }

            foreach (var item in Objects(document, "compositions"))
            {
                compositions.Add(ParseComposition(item));
            }
        }

        return new VisualDefinitionSet(parts, compositions, markers);
    }

    private static VisualPartDefinition ParsePart(JObject item)
    {
        var id = Required(item, "id");
        var primitives = Objects(item, "primitives").Select(primitive => ParsePrimitive(id, primitive)).ToList();
        if (primitives.Count == 0) throw new LocationDataException($"Visual part '{id}' needs at least one primitive.");
        return new VisualPartDefinition(id, (string?)item["category"] ?? "structure", (string?)item["sizeClass"] ?? "M", (string?)item["quality"] ?? "generated", primitives);
    }

    private static VisualPrimitiveDefinition ParsePrimitive(string partId, JObject item)
    {
        if (!Enum.TryParse<VisualShape>(Required(item, "shape"), ignoreCase: true, out var shape))
        {
            throw new LocationDataException($"Visual part '{partId}' has an unknown primitive shape.");
        }

        var size = Vec(item, "size", VisualVec3.One);
        if (size.X <= 0f || size.Y <= 0f || size.Z <= 0f)
        {
            throw new LocationDataException($"Visual part '{partId}' has a primitive with a non-positive size.");
        }

        return new VisualPrimitiveDefinition(
            shape,
            Vec(item, "position", VisualVec3.Zero),
            size,
            Vec(item, "euler", VisualVec3.Zero),
            Color(item, "color"),
            (int?)item["sides"] ?? 6,
            (float?)item["topScale"] ?? 0f,
            (bool?)item["emissive"] ?? false);
    }

    private static VisualCompositionDefinition ParseComposition(JObject item)
    {
        var id = Required(item, "id");
        var match = item["match"] as JObject ?? throw new LocationDataException($"Visual composition '{id}' requires a match object.");
        var elements = Objects(item, "elements").Select(element => ParseElement(id, element)).ToList();
        return new VisualCompositionDefinition(
            id,
            Required(match, "archetypeId"),
            (string?)match["variantId"],
            (string?)item["anchorMode"] ?? "point",
            (float?)item["ringRadius"] ?? 3.3f,
            elements);
    }

    private static VisualElementDefinition ParseElement(string compositionId, JObject item)
    {
        var id = Required(item, "id");
        string? channel = null;
        List<string>? states = null;
        if (item["shows"] is JObject shows)
        {
            channel = Required(shows, "channel");
            states = (shows["in"] as JArray)?.Values<string>().Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToList();
            if (states == null || states.Count == 0)
            {
                throw new LocationDataException($"Visual composition '{compositionId}' element '{id}' lists no states in 'shows.in'.");
            }
        }

        var poses = new Dictionary<string, VisualPoseDefinition>(StringComparer.Ordinal);
        if (item["poses"] is JObject poseObject)
        {
            foreach (var pose in poseObject.Properties())
            {
                var value = pose.Value as JObject ?? throw new LocationDataException($"Visual composition '{compositionId}' element '{id}' has an invalid pose.");
                poses[pose.Name] = new VisualPoseDefinition(Vec(value, "position", VisualVec3.Zero), Vec(value, "euler", VisualVec3.Zero), Vec(value, "scale", VisualVec3.One));
            }
        }

        return new VisualElementDefinition(
            id,
            Required(item, "part"),
            Vec(item, "position", VisualVec3.Zero),
            Vec(item, "euler", VisualVec3.Zero),
            channel,
            states,
            (string?)item["poseChannel"] ?? channel ?? "operational",
            poses);
    }

    private static IEnumerable<JObject> Objects(JObject parent, string field)
    {
        return (parent[field] as JArray ?? new JArray()).OfType<JObject>();
    }

    private static string Required(JObject item, string field)
    {
        var value = (string?)item[field];
        if (string.IsNullOrWhiteSpace(value)) throw new LocationDataException($"Visual definition field '{field}' is required.");
        return value!.Trim();
    }

    private static string Color(JObject item, string field)
    {
        var value = Required(item, field);
        if (value.Length != 7 || value[0] != '#' || !int.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            throw new LocationDataException($"Visual definition color '{value}' must look like #RRGGBB.");
        }

        return value.ToUpperInvariant();
    }

    private static VisualVec3 Vec(JObject item, string field, VisualVec3 fallback)
    {
        if (item[field] == null) return fallback;
        if (item[field] is not JArray array || array.Count != 3 || array.Any(token => token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
        {
            throw new LocationDataException($"Visual definition field '{field}' must be an array of three numbers.");
        }

        return new VisualVec3((float)array[0], (float)array[1], (float)array[2]);
    }
}

/// <summary>Checks visual definitions against the location data they refer to.</summary>
public static class VisualDefinitionValidator
{
    private static readonly HashSet<string> KnownChannels = new(StringComparer.Ordinal) { "interaction", "operational", "presence" };

    public static void Validate(VisualDefinitionSet visuals, LocationDataBundle locations, CrossSystemAuthoringBundle authoring)
    {
        if (visuals == null) throw new ArgumentNullException(nameof(visuals));
        foreach (var composition in visuals.Compositions)
        {
            if (!locations.Definitions.Archetypes.ContainsKey(composition.ArchetypeId))
            {
                throw new LocationDataException($"Visual composition '{composition.Id}' references unknown archetype '{composition.ArchetypeId}'.");
            }

            if (composition.VariantId != null && !locations.Definitions.Variants.ContainsKey(composition.VariantId))
            {
                throw new LocationDataException($"Visual composition '{composition.Id}' references unknown variant '{composition.VariantId}'.");
            }

            var profiles = StateProfilesFor(composition, authoring);
            var used = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var element in composition.Elements)
            {
                if (element.ShowChannel != null)
                {
                    CheckStates(composition, element, element.ShowChannel, element.ShowStates!, profiles, used);
                }

                foreach (var pose in element.Poses)
                {
                    CheckStates(composition, element, element.PoseChannel, new[] { pose.Key }, profiles, used);
                }
            }

            // A channel the composition reacts to must cover every value its state profiles allow.
            foreach (var channel in used)
            {
                var allowed = profiles.SelectMany(profile => profile.Channels.TryGetValue(channel.Key, out var definition) ? definition.Values : Array.Empty<string>()).Distinct(StringComparer.Ordinal);
                var missing = allowed.Where(value => !channel.Value.Contains(value)).ToList();
                if (missing.Count > 0)
                {
                    throw new LocationDataException($"Visual composition '{composition.Id}' does not cover {channel.Key} state(s): {string.Join(", ", missing)}.");
                }
            }
        }
    }

    private static List<LocationStateProfileDefinition> StateProfilesFor(VisualCompositionDefinition composition, CrossSystemAuthoringBundle authoring)
    {
        if (composition.VariantId == null)
        {
            return authoring.StateProfiles.Values.Where(profile => profile.ArchetypeId == composition.ArchetypeId).ToList();
        }

        return authoring.ScenarioProfiles.Values
            .Where(scenario => scenario.ArchetypeId == composition.ArchetypeId && scenario.VariantId == composition.VariantId)
            .Select(scenario => authoring.StateProfiles.TryGetValue(scenario.StateProfileId, out var profile) ? profile : null)
            .Where(profile => profile != null)
            .Select(profile => profile!)
            .ToList();
    }

    private static void CheckStates(
        VisualCompositionDefinition composition,
        VisualElementDefinition element,
        string channel,
        IEnumerable<string> states,
        IReadOnlyList<LocationStateProfileDefinition> profiles,
        Dictionary<string, HashSet<string>> used)
    {
        if (!KnownChannels.Contains(channel))
        {
            throw new LocationDataException($"Visual composition '{composition.Id}' element '{element.Id}' uses unknown state channel '{channel}'.");
        }

        if (!used.TryGetValue(channel, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal) { VisualDefinitionSet.UnknownState };
            used[channel] = set;
        }

        var allowed = profiles.SelectMany(profile => profile.Channels.TryGetValue(channel, out var definition) ? definition.Values : Array.Empty<string>()).ToHashSet(StringComparer.Ordinal);
        foreach (var state in states)
        {
            if (state != VisualDefinitionSet.UnknownState && !allowed.Contains(state))
            {
                throw new LocationDataException($"Visual composition '{composition.Id}' element '{element.Id}' refers to {channel} state '{state}' that its state profile does not define.");
            }

            set.Add(state);
        }
    }
}

}
