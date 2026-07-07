using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>
/// Filters typed archive entries by kind and a free-text search over title and source. Lives in
/// Core so both the UI and the tests share exactly the same filtering rules.
/// </summary>
public static class ArchiveFilter
{
    public static IReadOnlyList<ArchiveEntryState> Filter(
        IReadOnlyList<ArchiveEntryState> entries,
        ArchiveEntryKind? kind,
        string? search)
    {
        if (entries == null)
        {
            return Array.Empty<ArchiveEntryState>();
        }

        IEnumerable<ArchiveEntryState> query = entries;
        if (kind.HasValue)
        {
            query = query.Where(e => e.Kind == kind.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            query = query.Where(e =>
                e.Title.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                e.Source.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        return query.ToList();
    }

    public static int CountOfKind(IReadOnlyList<ArchiveEntryState> entries, ArchiveEntryKind kind)
    {
        return entries == null ? 0 : entries.Count(e => e.Kind == kind);
    }
}
}
