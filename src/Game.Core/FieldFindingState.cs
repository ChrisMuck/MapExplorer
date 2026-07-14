#nullable enable
using System;

namespace Game.Core
{

/// <summary>
/// A concrete record, sample or sketch currently carried by the active expedition. It is not yet
/// understood and therefore cannot award Knowledge Points until it has reached the base and been
/// analysed there.
/// </summary>
public sealed class FieldFindingState
{
    public FieldFindingState(string id, string definitionId, string sourceLocationId, int foundWorldDay)
    {
        if (foundWorldDay < 1) throw new ArgumentOutOfRangeException(nameof(foundWorldDay), "Finding day must be at least 1.");
        Id = RequireText(id, nameof(id));
        DefinitionId = RequireText(definitionId, nameof(definitionId));
        SourceLocationId = RequireText(sourceLocationId, nameof(sourceLocationId));
        FoundWorldDay = foundWorldDay;
    }

    public string Id { get; }
    public string DefinitionId { get; }
    public string SourceLocationId { get; }
    public int FoundWorldDay { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}
}
