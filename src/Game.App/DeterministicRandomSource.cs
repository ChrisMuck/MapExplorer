#nullable enable
using System;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Application-boundary random source for simulation decisions. It is backed by persisted
/// WorldState and deliberately has no Unity dependency, so a seed and command sequence replay.
/// </summary>
public interface IDeterministicRandomSource
{
    int NextInt(int exclusiveMaximum);
}

public sealed class WorldDeterministicRandomSource : IDeterministicRandomSource
{
    private readonly DeterministicRandomState state;

    public WorldDeterministicRandomSource(WorldState world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        state = world.Random;
    }

    public int NextInt(int exclusiveMaximum) => state.NextInt(exclusiveMaximum);
}
}
