#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class WorldState
{
    private readonly List<WorldPathState> paths;
    private readonly List<SpecialLocationState> locations;
    private readonly List<WorldTriggerState> worldTriggers;
    private readonly List<ScheduledConsequenceState> scheduledConsequences;
    private readonly List<RegionFactionAwarenessState> factionAwareness;

    public WorldState(
        HexMapState map,
        IEnumerable<WorldPathState>? paths = null,
        IEnumerable<SpecialLocationState>? locations = null,
        int worldDay = 1,
        IEnumerable<WorldTriggerState>? worldTriggers = null,
        IEnumerable<ScheduledConsequenceState>? scheduledConsequences = null,
        IEnumerable<RegionFactionAwarenessState>? factionAwareness = null)
    {
        if (worldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(worldDay), worldDay, "World day must be at least 1.");
        }

        Map = map ?? throw new ArgumentNullException(nameof(map));
        this.paths = new List<WorldPathState>(paths ?? Enumerable.Empty<WorldPathState>());
        this.locations = new List<SpecialLocationState>(locations ?? Enumerable.Empty<SpecialLocationState>());
        this.worldTriggers = new List<WorldTriggerState>(worldTriggers ?? Enumerable.Empty<WorldTriggerState>());
        this.scheduledConsequences = new List<ScheduledConsequenceState>(scheduledConsequences ?? Enumerable.Empty<ScheduledConsequenceState>());
        this.factionAwareness = new List<RegionFactionAwarenessState>(factionAwareness ?? Enumerable.Empty<RegionFactionAwarenessState>());
        WorldDay = worldDay;
    }

    public HexMapState Map { get; }

    public IReadOnlyList<WorldPathState> Paths
    {
        get { return paths; }
    }

    public IReadOnlyList<SpecialLocationState> Locations
    {
        get { return locations; }
    }

    public int WorldDay { get; private set; }

    public IReadOnlyList<WorldTriggerState> WorldTriggers
    {
        get { return worldTriggers; }
    }

    public IReadOnlyList<ScheduledConsequenceState> ScheduledConsequences
    {
        get { return scheduledConsequences; }
    }

    public IReadOnlyList<RegionFactionAwarenessState> FactionAwareness => factionAwareness;

    public void AddPath(WorldPathState path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (paths.Any(existing => existing.Id == path.Id))
        {
            return;
        }

        paths.Add(path);
    }

    public void AdvanceDays(int days)
    {
        if (days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Advance must be at least one day.");
        }

        WorldDay += days;
    }

    public void QueueWorldTrigger(WorldTriggerState trigger)
    {
        if (trigger == null)
        {
            throw new ArgumentNullException(nameof(trigger));
        }

        if (worldTriggers.Any(existing => existing.Id == trigger.Id))
        {
            return;
        }

        worldTriggers.Add(trigger);
    }

    public void ScheduleConsequence(ScheduledConsequenceState consequence)
    {
        if (consequence == null)
        {
            throw new ArgumentNullException(nameof(consequence));
        }

        if (scheduledConsequences.Any(existing => existing.Id == consequence.Id))
        {
            return;
        }

        scheduledConsequences.Add(consequence);
    }

    public void EscalateFactionAwareness(string factionId, string regionId)
    {
        var state = factionAwareness.FirstOrDefault(item => item.FactionId == factionId && item.RegionId == regionId);
        if (state == null)
        {
            state = new RegionFactionAwarenessState(factionId, regionId);
            factionAwareness.Add(state);
        }

        state.Escalate();
    }
}
}
