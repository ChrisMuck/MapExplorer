using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Helpers for test aids that put a location into a chosen state. They are meant for the editor and development
    /// builds: in the real game these states change through commands, world turns and consequences.
    /// </summary>
    public static class MapStyleDebug
    {
        /// <summary>The allowed values of a state channel for a location, from its scenario's state profile.</summary>
        public static IReadOnlyList<string> StateValues(GameDataCatalog catalog, SpecialLocationState location, string channel)
        {
            var scenario = catalog.Authoring.ScenarioProfiles.Values
                .FirstOrDefault(profile => profile.ArchetypeId == location.ArchetypeId && profile.VariantId == location.VariantId);
            if (scenario == null
                || !catalog.Authoring.StateProfiles.TryGetValue(scenario.StateProfileId, out var profile)
                || !profile.Channels.TryGetValue(channel, out var definition))
            {
                return new List<string>();
            }

            return definition.Values;
        }

        public static string CurrentState(SpecialLocationState location, string channel)
        {
            switch (channel)
            {
                case LocationStateChannels.Interaction: return location.InteractionStateId;
                case LocationStateChannels.Operational: return location.OperationalStateId;
                default: return location.PresenceStateId;
            }
        }

        /// <summary>Sets the world state of a channel and lets the player observe it, so the map shows it at once.</summary>
        public static void SetAndObserve(GameState game, SpecialLocationState location, string channel, string stateId)
        {
            location.SetState(channel, stateId);
            game.Knowledge.ObserveLocationCondition(location, game.World.WorldDay);
        }

        /// <summary>The next value of a channel in the order of its state profile.</summary>
        public static string NextState(GameDataCatalog catalog, SpecialLocationState location, string channel)
        {
            var values = StateValues(catalog, location, channel);
            if (values.Count == 0)
            {
                return CurrentState(location, channel);
            }

            var index = values.ToList().IndexOf(CurrentState(location, channel));
            return values[(index + 1) % values.Count];
        }
    }
}
