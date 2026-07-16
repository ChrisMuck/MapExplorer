#nullable enable
using System;

namespace Game.Core
{

/// <summary>Persisted objective result of one finding-table roll, including an empty result.</summary>
public sealed class FindingTableRollState
{
    public FindingTableRollState(string key, string tableId, string sourceLocationId, string stateKey, string? findingDefinitionId, int resolvedWorldDay)
    {
        Key = Require(key, nameof(key));
        TableId = Require(tableId, nameof(tableId));
        SourceLocationId = Require(sourceLocationId, nameof(sourceLocationId));
        StateKey = Require(stateKey, nameof(stateKey));
        FindingDefinitionId = string.IsNullOrWhiteSpace(findingDefinitionId) ? null : findingDefinitionId.Trim();
        ResolvedWorldDay = resolvedWorldDay >= 1 ? resolvedWorldDay : throw new ArgumentOutOfRangeException(nameof(resolvedWorldDay));
    }

    public string Key { get; }
    public string TableId { get; }
    public string SourceLocationId { get; }
    public string StateKey { get; }
    public string? FindingDefinitionId { get; }
    public bool IsEmpty => FindingDefinitionId == null;
    public int ResolvedWorldDay { get; }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
}
}
