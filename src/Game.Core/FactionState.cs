#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class FactionState
{
    private readonly List<HexCoord> warningZones = new();
    private readonly List<string> memories = new();

    public FactionState(
        string id,
        string name,
        FactionContactStatus contactStatus = FactionContactStatus.Unknown,
        int trust = 0,
        int anger = 0,
        int fear = 0,
        IEnumerable<HexCoord>? warningZones = null,
        IEnumerable<string>? memories = null)
    {
        Id = RequireText(id, nameof(id));
        Name = RequireText(name, nameof(name));
        ContactStatus = contactStatus;
        Trust = ClampMetric(trust);
        Anger = ClampMetric(anger);
        Fear = ClampMetric(fear);
        this.warningZones.AddRange(warningZones ?? Enumerable.Empty<HexCoord>());
        this.memories.AddRange(memories ?? Enumerable.Empty<string>());
    }

    public string Id { get; }

    public string Name { get; }

    public FactionContactStatus ContactStatus { get; private set; }

    public int Trust { get; private set; }

    public int Anger { get; private set; }

    public int Fear { get; private set; }

    public IReadOnlyList<HexCoord> WarningZones
    {
        get { return warningZones; }
    }

    public IReadOnlyList<string> Memories
    {
        get { return memories; }
    }

    public void SetContactStatus(FactionContactStatus status)
    {
        ContactStatus = status;
    }

    public void Adjust(int trustDelta = 0, int angerDelta = 0, int fearDelta = 0)
    {
        Trust = ClampMetric(Trust + trustDelta);
        Anger = ClampMetric(Anger + angerDelta);
        Fear = ClampMetric(Fear + fearDelta);
    }

    public bool IsWarningZone(HexCoord coord)
    {
        return warningZones.Contains(coord);
    }

    public void AddMemory(string memory)
    {
        if (string.IsNullOrWhiteSpace(memory) || memories.Contains(memory))
        {
            return;
        }

        memories.Add(memory);
    }

    public bool HasMemory(string memory)
    {
        return memories.Contains(memory);
    }

    private static int ClampMetric(int value)
    {
        return Math.Max(0, Math.Min(100, value));
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}
}
