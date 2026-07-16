#nullable enable
using System;
using Game.Core;
using Newtonsoft.Json;

namespace Game.App
{
public static class KnowledgeRuntimeSnapshotSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        MissingMemberHandling = MissingMemberHandling.Error
    };

    public static string Serialize(KnowledgeState knowledge) =>
        JsonConvert.SerializeObject(KnowledgeRuntimeSnapshot.Capture(knowledge ?? throw new ArgumentNullException(nameof(knowledge))), Settings);

    public static KnowledgeState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Knowledge snapshot JSON must not be empty.", nameof(json));
        return (JsonConvert.DeserializeObject<KnowledgeRuntimeSnapshot>(json, Settings)
            ?? throw new InvalidOperationException("Knowledge snapshot JSON did not contain a snapshot.")).Restore();
    }
}
}
