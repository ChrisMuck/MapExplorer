#nullable enable
using System;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>Applies authored territory-entry rules without embedding faction IDs in movement logic.</summary>
public sealed class FactionTerritoryEntryResolver
{
    private readonly CrossSystemDataBundle? content;

    public FactionTerritoryEntryResolver(CrossSystemDataBundle? content)
    {
        this.content = content;
    }

    public bool TryResolve(GameState game, FactionState faction, HexCoord destination, bool isWarningZone)
    {
        if (content == null) return false;
        var entryKind = isWarningZone ? "warning" : "territory";
        var rule = content.FactionTerritoryEntryRules
            .Where(item => item.Matches(faction, entryKind))
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (rule == null) return false;

        var memoryKey = isWarningZone
            ? $"territory-entry:{rule.Id}:{destination.Q}:{destination.R}"
            : $"territory-entry:{rule.Id}:expedition-{game.Expedition.ExpeditionNumber}";
        if (faction.HasMemory(memoryKey)) return true;

        faction.AddMemory(memoryKey);
        faction.Adjust(rule.TrustDelta, rule.AngerDelta, rule.FearDelta);
        if (faction.ContactStatus == FactionContactStatus.Unknown)
        {
            faction.SetContactStatus(FactionContactStatus.Rumored);
        }
        if (rule.PromotesContact && faction.ContactStatus == FactionContactStatus.Contacted)
        {
            faction.SetContactStatus(FactionContactStatus.Open);
        }

        if (rule.EventTitle != null && rule.EventBody != null)
        {
            var options = isWarningZone
                ? new[]
                {
                    new EventOptionState("mark", "Warnung markieren", "Die Expedition hält das Zeichen auf der Karte fest.", EventOptionEffectKind.AddWarningMarker),
                    new EventOptionState("archive", "Beobachtung archivieren", "Die Expedition hält die Beobachtung fest.", EventOptionEffectKind.Archive),
                    new EventOptionState("continue", "Vorsichtig weiter", "Die Expedition achtet auf weitere Zeichen.", EventOptionEffectKind.None)
                }
                : BuildTerritoryOptions(faction, rule);
            game.Events.Enqueue(new EventState(
                $"faction-territory-{game.Events.Events.Count + 1}",
                isWarningZone ? EventKind.WarningSign : EventKind.FactionReaction,
                rule.EventTitle,
                rule.RevealsFaction ? faction.Name : "Unbekannte Beobachter",
                rule.EventBody,
                options,
                destination,
                rule.RevealsFaction ? faction.Id : null));
        }

        return true;
    }

    private static EventOptionState[] BuildTerritoryOptions(FactionState faction, FactionTerritoryEntryRuleDefinition rule)
    {
        var options = new System.Collections.Generic.List<EventOptionState>();
        if (rule.AllowsInteraction && rule.RevealsFaction)
        {
            options.Add(new EventOptionState("contact", "Kontakt aufnehmen", $"Die Expedition nähert sich den Vertretern von {faction.Name}.", EventOptionEffectKind.OpenFactionInteraction));
        }

        options.Add(new EventOptionState("archive", "Beobachtung archivieren", "Die Expedition hält die Reaktion fest.", EventOptionEffectKind.Archive));
        options.Add(new EventOptionState("continue", "Vorsichtig weiter", "Die Expedition beobachtet die Umgebung weiter.", EventOptionEffectKind.None));
        return options.ToArray();
    }
}
}
