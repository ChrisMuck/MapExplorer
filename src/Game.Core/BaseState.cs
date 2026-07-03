using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class BaseState
{
    private readonly List<string> archiveEntries = new();

    public BaseState(HexCoord location)
    {
        Location = location;
    }

    public HexCoord Location { get; }

    public IReadOnlyList<string> ArchiveEntries
    {
        get { return archiveEntries; }
    }

    public void AddArchiveEntry(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            throw new ArgumentException("Archive entry must not be empty.", nameof(entry));
        }

        archiveEntries.Add(entry);
    }
}
}
