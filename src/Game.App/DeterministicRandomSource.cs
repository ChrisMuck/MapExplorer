#nullable enable
using System;
using Game.Core;

namespace Game.App
{

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
