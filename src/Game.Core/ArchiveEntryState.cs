using System;

namespace Game.Core
{

/// <summary>
/// A typed archive entry. Replaces the old flat archive string while staying backward compatible:
/// plain-string writes become <see cref="ArchiveEntryKind.Notiz"/> entries and <see cref="Text"/>
/// projects back to the original string for existing string consumers.
/// </summary>
public sealed class ArchiveEntryState
{
    public ArchiveEntryState(
        string title,
        ArchiveEntryKind kind,
        string source,
        int worldDay,
        ArchiveReliability reliability)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Archive entry title must not be empty.", nameof(title));
        }

        if (worldDay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldDay), worldDay, "World day must not be negative.");
        }

        Title = title;
        Kind = kind;
        Source = string.IsNullOrWhiteSpace(source) ? "Basis" : source;
        WorldDay = worldDay;
        Reliability = reliability;
    }

    public string Title { get; }

    public ArchiveEntryKind Kind { get; }

    public string Source { get; }

    /// <summary>World day the entry was recorded, or 0 when unknown (legacy plain-string writes).</summary>
    public int WorldDay { get; }

    public ArchiveReliability Reliability { get; }

    /// <summary>Back-compat projection to the flat archive string that existing consumers expect.</summary>
    public string Text => Title;

    /// <summary>Wraps a legacy plain-string archive line as an untyped note.</summary>
    public static ArchiveEntryState Note(string text)
    {
        return new ArchiveEntryState(text, ArchiveEntryKind.Notiz, "Basis", 0, ArchiveReliability.Unbekannt);
    }
}
}
