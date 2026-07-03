using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class GameState
{
    public GameState(
        WorldState world,
        KnowledgeState knowledge,
        PlayerNotesState playerNotes,
        ExpeditionState expedition,
        BaseState baseState)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
        PlayerNotes = playerNotes ?? throw new ArgumentNullException(nameof(playerNotes));
        Expedition = expedition ?? throw new ArgumentNullException(nameof(expedition));
        Base = baseState ?? throw new ArgumentNullException(nameof(baseState));
    }

    public WorldState World { get; }

    public KnowledgeState Knowledge { get; }

    public PlayerNotesState PlayerNotes { get; }

    public ExpeditionState Expedition { get; }

    public BaseState Base { get; }
}
}
