#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;
using Newtonsoft.Json;

namespace Game.App
{

/// <summary>
/// JSON codec for the persistent cross-system runtime state. It deliberately does not define
/// player save slots or bypass the base-camp save policy; callers provide the campaign topology
/// that belongs to their wider save format.
/// </summary>
public static class WorldRuntimeSnapshotSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        MissingMemberHandling = MissingMemberHandling.Error
    };

    public static string Serialize(WorldState world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        return JsonConvert.SerializeObject(WorldRuntimeSnapshot.Capture(world), Settings);
    }

    public static WorldState Deserialize(
        string json,
        HexMapState map,
        IEnumerable<WorldPathState>? paths = null,
        IEnumerable<SpecialLocationState>? locations = null)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Runtime snapshot JSON must not be empty.", nameof(json));
        var snapshot = JsonConvert.DeserializeObject<WorldRuntimeSnapshot>(json, Settings)
            ?? throw new InvalidOperationException("Runtime snapshot JSON did not contain a snapshot.");
        return snapshot.Restore(map, paths, locations);
    }
}
}
