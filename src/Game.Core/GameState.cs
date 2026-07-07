#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class GameState
{
    private readonly List<FactionState> factions;

    public GameState(
        WorldState world,
        KnowledgeState knowledge,
        PlayerNotesState playerNotes,
        ExpeditionState expedition,
        BaseState baseState,
        EventQueueState? eventQueue = null,
        IEnumerable<FactionState>? factions = null,
        LeverageInventoryState? leverageItems = null,
        BaseRosterState? roster = null)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
        PlayerNotes = playerNotes ?? throw new ArgumentNullException(nameof(playerNotes));
        Expedition = expedition ?? throw new ArgumentNullException(nameof(expedition));
        Base = baseState ?? throw new ArgumentNullException(nameof(baseState));
        Events = eventQueue ?? new EventQueueState();
        this.factions = new List<FactionState>(factions ?? Enumerable.Empty<FactionState>());
        LeverageItems = leverageItems ?? new LeverageInventoryState();
        Roster = roster ?? new BaseRosterState();
    }

    public WorldState World { get; }

    public KnowledgeState Knowledge { get; }

    public PlayerNotesState PlayerNotes { get; }

    public ExpeditionState Expedition { get; private set; }

    public BaseState Base { get; }

    public EventQueueState Events { get; }

    public LeverageInventoryState LeverageItems { get; }

    public BaseRosterState Roster { get; }

    public FactionInteractionState? ActiveFactionInteraction { get; private set; }

    public IReadOnlyList<FactionState> Factions
    {
        get { return factions; }
    }

    public FactionState? FindFaction(string factionId)
    {
        foreach (var faction in factions)
        {
            if (faction.Id == factionId)
            {
                return faction;
            }
        }

        return null;
    }

    public void SetExpedition(ExpeditionState expedition)
    {
        Expedition = expedition ?? throw new ArgumentNullException(nameof(expedition));
    }

    public void SetActiveFactionInteraction(FactionInteractionState interaction)
    {
        ActiveFactionInteraction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    public void ClearActiveFactionInteraction()
    {
        ActiveFactionInteraction = null;
    }
}
}
