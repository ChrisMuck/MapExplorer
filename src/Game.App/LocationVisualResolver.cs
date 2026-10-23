#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>How well the player knows a location. Unknown locations are not drawn at all.</summary>
public enum VisualKnowledge
{
    Confirmed,
    Old,
    Reported
}

/// <summary>The state channels a visual is resolved from. A null value means the player does not know it.</summary>
public sealed class LocationVisualContext
{
    public LocationVisualContext(VisualKnowledge knowledge, string? operationalStateId, string? presenceStateId = null, string? interactionStateId = null)
    {
        Knowledge = knowledge;
        OperationalStateId = operationalStateId;
        PresenceStateId = presenceStateId;
        InteractionStateId = interactionStateId;
    }

    public VisualKnowledge Knowledge { get; }
    public string? OperationalStateId { get; }
    public string? PresenceStateId { get; }
    public string? InteractionStateId { get; }
}

/// <summary>One element of a resolved visual: where it sits, whether it is shown and how it is posed right now.</summary>
public sealed class VisualElementState
{
    public VisualElementState(
        string id,
        string partId,
        VisualVec3 position,
        VisualVec3 euler,
        bool shown,
        VisualPoseDefinition pose,
        VisualElementKind kind = VisualElementKind.Part,
        string? colorHex = null,
        float intensity = 1f,
        float range = 5f,
        float size = 1f)
    {
        Kind = kind;
        ColorHex = colorHex;
        Intensity = intensity;
        Range = range;
        Size = size;
        Id = id;
        PartId = partId;
        Position = position;
        Euler = euler;
        Shown = shown;
        Pose = pose;
    }

    public string Id { get; }
    public VisualElementKind Kind { get; }
    public string PartId { get; }
    public VisualVec3 Position { get; }
    public VisualVec3 Euler { get; }
    public string? ColorHex { get; }
    public float Intensity { get; }
    public float Range { get; }
    public float Size { get; }
    public bool Shown { get; }
    public VisualPoseDefinition Pose { get; }
}

/// <summary>
/// Everything a presentation client needs to draw one location. It is a pure function of the location
/// definition and what the player knows, so tests can check it and clients never decide anything.
/// </summary>
public sealed class LocationVisualDescription
{
    public LocationVisualDescription(
        string locationId,
        string? compositionId,
        string archetypeId,
        string markerColorHex,
        int ringGaps,
        float ringRadius,
        string anchorMode,
        VisualKnowledge knowledge,
        string stateKey,
        IReadOnlyList<VisualElementState> elements,
        float scale = 1f)
    {
        Scale = scale;
        LocationId = locationId;
        CompositionId = compositionId;
        ArchetypeId = archetypeId;
        MarkerColorHex = markerColorHex;
        RingGaps = ringGaps;
        RingRadius = ringRadius;
        AnchorMode = anchorMode;
        Knowledge = knowledge;
        StateKey = stateKey;
        Elements = elements;
    }

    public string LocationId { get; }
    /// <summary>Null when no composition matched and only the marker is drawn.</summary>
    public string? CompositionId { get; }
    public string ArchetypeId { get; }
    public string MarkerColorHex { get; }
    public int RingGaps { get; }
    public float RingRadius { get; }
    public string AnchorMode { get; }
    /// <summary>Scale of the whole place on the map.</summary>
    public float Scale { get; }
    public VisualKnowledge Knowledge { get; }
    /// <summary>Compact text of the resolved state, used to skip updates when nothing changed.</summary>
    public string StateKey { get; }
    public IReadOnlyList<VisualElementState> Elements { get; }

    public IEnumerable<VisualElementState> ShownElements => Elements.Where(element => element.Shown);
}

public sealed class LocationVisualResolver
{
    private readonly VisualDefinitionSet definitions;

    public LocationVisualResolver(VisualDefinitionSet definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    public LocationVisualDescription Resolve(SpecialLocationState location, LocationVisualContext context)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var archetypeId = location.ArchetypeId ?? string.Empty;
        var marker = definitions.FindMarker(location.ArchetypeId);
        var composition = definitions.FindComposition(location.ArchetypeId, location.VariantId);
        var states = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["operational"] = context.OperationalStateId ?? VisualDefinitionSet.UnknownState,
            ["presence"] = context.PresenceStateId ?? VisualDefinitionSet.UnknownState,
            ["interaction"] = context.InteractionStateId ?? VisualDefinitionSet.UnknownState
        };

        var elements = new List<VisualElementState>();
        if (composition != null)
        {
            foreach (var element in composition.Elements)
            {
                var shown = element.ShowChannel == null || (element.ShowStates?.Contains(states[element.ShowChannel]) ?? false);
                var pose = element.Poses.TryGetValue(states[element.PoseChannel], out var found) ? found : VisualPoseDefinition.Neutral;
                elements.Add(new VisualElementState(
                    element.Id, element.PartId, element.Position, element.Euler, shown, pose,
                    element.Kind, element.ColorHex, element.Intensity, element.Range, element.Size));
            }
        }

        var stateKey = $"{states["operational"]}|{states["presence"]}|{states["interaction"]}|{context.Knowledge}";
        return new LocationVisualDescription(
            location.Id,
            composition?.Id,
            archetypeId,
            marker.ColorHex,
            marker.RingGaps,
            composition?.RingRadius ?? 3.3f,
            composition?.AnchorMode ?? "point",
            context.Knowledge,
            stateKey,
            elements,
            composition?.Scale ?? 1f);
    }

    /// <summary>
    /// Resolves a location the way the player may see it: nothing for unknown ground, and only the last
    /// observed condition, never the live world state. Returns null when the location must not be drawn.
    /// </summary>
    public LocationVisualDescription? ResolveForPlayer(GameState game, SpecialLocationState location)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (location == null) throw new ArgumentNullException(nameof(location));
        VisualKnowledge knowledge;
        switch (game.Knowledge.GetTileKnowledge(location.Anchor.PrimaryCoord))
        {
            case KnowledgeLevel.Confirmed: knowledge = VisualKnowledge.Confirmed; break;
            case KnowledgeLevel.OldOrDoubtful: knowledge = VisualKnowledge.Old; break;
            case KnowledgeLevel.Reported: knowledge = VisualKnowledge.Reported; break;
            default: return null;
        }

        var known = game.Knowledge.FindLocationCondition(location.Id);
        return Resolve(location, new LocationVisualContext(
            knowledge,
            NullIfEmpty(known?.OperationalStateId),
            NullIfEmpty(known?.PresenceStateId),
            NullIfEmpty(known?.InteractionStateId)));
    }

    /// <summary>Developer view only: resolves from the live world state with full knowledge.</summary>
    public LocationVisualDescription ResolveWorldTruth(SpecialLocationState location)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        return Resolve(location, new LocationVisualContext(VisualKnowledge.Confirmed, location.OperationalStateId, location.PresenceStateId, location.InteractionStateId));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

}
