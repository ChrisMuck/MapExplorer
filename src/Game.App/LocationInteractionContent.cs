#nullable enable
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public static class LocationInteractionContent
{
    public const string ArchetypeRouteObstacle = "route-obstacle";
    public const string ArchetypeInvestigationSite = "investigation-site";
    public const string VariantBrokenBridge = "broken-bridge";
    public const string VariantMarkedGrave = "marked-grave";
    public const string ModifierRepairable = "modifier-repairable";
    public const string ModifierUnstable = "modifier-unstable";
    public const string ModifierWatched = "modifier-watched";
    public const string ModifierSacred = "modifier-sacred";
    public const string ModifierFactionOwned = "modifier-faction-owned";

    public const string ActionAssessCrossing = "action-assess-crossing";
    public const string ActionFindBypass = "action-find-bypass";
    public const string ActionConstructTemporaryPassage = "action-construct-temporary-passage";
    public const string ActionAttemptCrossing = "action-attempt-crossing";
    public const string ActionRebuildBridge = "action-rebuild-bridge";
    public const string ActionInspect = "action-inspect";
    public const string ActionDocument = "action-document";
    public const string ActionInvestigate = "action-investigate";
    public const string ActionLeaveOffering = "action-leave-offering";
    public const string ActionDisturb = "action-disturb";
    public const string ActionMark = "action-mark";
    public const string ActionLeave = "action-leave";

    public static LocationInteractionDefinitionSet CreateDefinitionSet()
    {
        return new LocationInteractionDefinitionSet(
            CreateArchetypes(),
            CreateVariants(),
            CreateModifiers(),
            CreateActions());
    }

    private static IEnumerable<LocationArchetypeDefinition> CreateArchetypes()
    {
        return new[]
        {
            new LocationArchetypeDefinition(ArchetypeRouteObstacle, new[]
            {
                ActionAssessCrossing,
                ActionFindBypass,
                ActionConstructTemporaryPassage,
                ActionAttemptCrossing,
                ActionMark,
                ActionLeave
            }),
            new LocationArchetypeDefinition(ArchetypeInvestigationSite, new[]
            {
                ActionInspect,
                ActionDocument,
                ActionInvestigate,
                ActionMark,
                ActionLeave
            })
        };
    }

    private static IEnumerable<LocationVariantDefinition> CreateVariants()
    {
        return new[]
        {
            new LocationVariantDefinition(VariantBrokenBridge),
            new LocationVariantDefinition(VariantMarkedGrave, addedActionIds: new[] { ActionLeaveOffering, ActionDisturb })
        };
    }

    private static IEnumerable<LocationModifierDefinition> CreateModifiers()
    {
        return new[]
        {
            new LocationModifierDefinition(ModifierRepairable, addedActionIds: new[] { ActionRebuildBridge }),
            new LocationModifierDefinition(
                ModifierUnstable,
                riskAdjustmentsByActionId: new Dictionary<string, int>
                {
                    [ActionConstructTemporaryPassage] = 15,
                    [ActionAttemptCrossing] = 15,
                    [ActionRebuildBridge] = 15
                },
                appliesWhenOperationalStateIds: new[]
                {
                    LocationStateIds.Operational.Blocked,
                    LocationStateIds.Operational.RiskyPassage
                }),
            new LocationModifierDefinition(ModifierWatched),
            new LocationModifierDefinition(
                ModifierSacred,
                riskAdjustmentsByActionId: new Dictionary<string, int>
                {
                    [ActionDisturb] = 35,
                    [ActionInvestigate] = 10
                }),
            new LocationModifierDefinition(ModifierFactionOwned)
        };
    }

    private static IEnumerable<LocationActionDefinition> CreateActions()
    {
        return new[]
        {
            new LocationActionDefinition(
                ActionAssessCrossing,
                "Ueberqueren einschaetzen",
                "Die Statik der Truemmer und die Schlucht beurteilen, ohne die Querung zu versuchen.",
                hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(0, confidence: LocationEstimateConfidence.Assessed),
                outcomes: new[]
                {
                    new LocationOutcomeDefinition("success", "Erfolg", new[]
                    {
                        ChangeInteraction(LocationStateIds.Interaction.Inspected, "Die Bruecke ist als instabiler, aber reparierbarer Uebergang dokumentiert."),
                        Knowledge(2, "Die Expedition gewinnt verwertbare Routenkenntnis.")
                    })
                },
                repeatPolicy: LocationActionRepeatPolicy.OncePerLocation),

            new LocationActionDefinition(
                ActionFindBypass,
                "Umweg suchen",
                "Eine Route um die Schlucht herum suchen. Das kostet Zeit, kann aber den gefaehrlichen Uebergang vermeiden.",
                hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(15, confidence: LocationEstimateConfidence.Guess),
                outcomes: new[]
                {
                    new LocationOutcomeDefinition("success", "Erfolg", new[]
                    {
                        Knowledge(3, "Ein langsamer, aber nutzbarer Umweg wird notiert."),
                        Archive("Ein Umweg um die zerstoerte Bruecke wurde als Routenhypothese vermerkt.")
                    }),
                    new LocationOutcomeDefinition("failure", "Fehlschlag", new[]
                    {
                        ConsumeSupplies(1, "Die Suche verbraucht zusaetzliche Vorraete, ohne einen sicheren Weg zu finden.")
                    })
                },
                repeatPolicy: LocationActionRepeatPolicy.OncePerState),

            new LocationActionDefinition(
                ActionConstructTemporaryPassage,
                "Seiluebergang bauen",
                "Eine provisorische Querung errichten. Der Bau ist riskant, macht die Passage danach aber berechenbarer.",
                hardRequirements: new[]
                {
                    Operational(LocationStateIds.Operational.Blocked, "Nur ein blockierter Uebergang braucht eine provisorische Querung.")
                },
                riskProfile: new LocationRiskProfileDefinition(20, confidence: LocationEstimateConfidence.Assessed),
                outcomes: new[]
                {
                    new LocationOutcomeDefinition("success-with-cost", "Erfolg mit Kosten", new[]
                    {
                        ChangeOperational(LocationStateIds.Operational.RiskyPassage, "Der operative Zustand wechselt zu: Riskante Passage."),
                        ConsumeSupplies(1, "Seile und Material werden abgenutzt."),
                        Archive("Ein provisorischer Seiluebergang wurde an der zerstoerten Bruecke eingerichtet.")
                    }),
                    new LocationOutcomeDefinition("failure", "Fehlschlag", new[]
                    {
                        ConsumeSupplies(1, "Material geht beim Bauversuch verloren."),
                        ChangeMorale(-1, "Die Gruppe verliert Vertrauen in die Querung.")
                    })
                },
                repeatPolicy: LocationActionRepeatPolicy.RepeatableWithCost),

            new LocationActionDefinition(
                ActionAttemptCrossing,
                "Bruecke ueberqueren",
                "Die Truemmer oder die provisorische Querung nutzen und die andere Seite erreichen.",
                hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(
                    45,
                    new Dictionary<string, int>
                    {
                        [LocationStateIds.Operational.Blocked] = 45,
                        [LocationStateIds.Operational.RiskyPassage] = 20,
                        [LocationStateIds.Operational.Repaired] = 5
                    },
                    LocationEstimateConfidence.Assessed),
                outcomes: new[]
                {
                    new LocationOutcomeDefinition("success", "Erfolg", new[]
                    {
                        Knowledge(2, "Die Expedition bestaetigt die Route ueber die Schlucht.")
                    }),
                    new LocationOutcomeDefinition("success-with-cost", "Erfolg mit Kosten", new[]
                    {
                        ConsumeSupplies(1, "Beim Uebergang geht Ausruestung verloren."),
                        ChangeMorale(-1, "Der Uebergang belastet die Gruppe.")
                    })
                },
                repeatPolicy: LocationActionRepeatPolicy.RepeatableWithCost),

            new LocationActionDefinition(
                ActionRebuildBridge,
                "Bruecke wieder aufbauen",
                "Ein mehrtaegiges Projekt, das den Uebergang dauerhaft wiederherstellt.",
                hardRequirements: new[]
                {
                    Operational(LocationStateIds.Operational.Blocked, LocationStateIds.Operational.RiskyPassage, "Die Bruecke muss blockiert oder riskant passierbar sein."),
                    Role(ExpeditionMemberRole.Engineer, "Erfordert eine Ingenieurin oder einen Ingenieur."),
                    Anchor(LocationAnchorKind.Edge, "Nur ein Kantenhindernis kann als Uebergang repariert werden.")
                },
                riskProfile: new LocationRiskProfileDefinition(25, confidence: LocationEstimateConfidence.Assessed),
                repeatPolicy: LocationActionRepeatPolicy.OncePerLocation,
                startsProject: true,
                projectDurationDays: 3,
                projectCompletionEffects: new[]
                {
                    ChangeOperational(LocationStateIds.Operational.Repaired, "Der operative Zustand wechselt zu: Repariert."),
                    OpenRoute("Die Bruecke oeffnet eine dauerhafte Route ueber die Schlucht."),
                    Archive("Die zerstoerte Bruecke wurde dauerhaft repariert.")
                }),

            new LocationActionDefinition(ActionInspect, "Inspizieren", "Den Ort betrachten, ohne etwas zu veraendern.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(5, confidence: LocationEstimateConfidence.Assessed),
                outcomes: new[] { new LocationOutcomeDefinition("success", "Erfolg", new[] { ChangeInteraction(LocationStateIds.Interaction.Inspected, "Der Ort wurde inspiziert."), Knowledge(2, "Ein Hinweis wurde gesichert.") }) }),
            new LocationActionDefinition(ActionDocument, "Dokumentieren", "Zeichen, Lage und Zustand fuer spaetere Auswertung festhalten.", hardRequirements: Adjacent(),
                outcomes: new[] { new LocationOutcomeDefinition("success", "Erfolg", new[] { Archive("Die Fundstelle wurde dokumentiert.") }) }),
            new LocationActionDefinition(ActionInvestigate, "Untersuchen", "Den Ort eingehender untersuchen und eine Deutung versuchen.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(25, confidence: LocationEstimateConfidence.Guess),
                outcomes: new[] { new LocationOutcomeDefinition("partial", "Teilergebnis", new[] { ChangeInteraction(LocationStateIds.Interaction.Investigated, "Die Untersuchung liefert Hinweise, aber keine sichere Deutung."), Knowledge(3, "Unsicheres Fundstellenwissen wurde gewonnen.") }) }),
            new LocationActionDefinition(ActionLeaveOffering, "Opfergabe hinterlassen", "Den Ort respektvoll behandeln, bevor weitere Schritte unternommen werden.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(5, confidence: LocationEstimateConfidence.Assessed),
                outcomes: new[] { new LocationOutcomeDefinition("success", "Erfolg", new[] { FactionMemory("border-wardens", "outsiders respected a marked grave", "Der respektvolle Umgang koennte erinnert werden.") }) }),
            new LocationActionDefinition(ActionDisturb, "Stoeren", "Das Grab oder Siegel trotz Warnzeichen oeffnen.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(45, confidence: LocationEstimateConfidence.Assessed),
                outcomes: new[] { new LocationOutcomeDefinition("success-with-cost", "Erfolg mit Kosten", new[] { Knowledge(5, "Ein Fund wird geborgen."), ChangeMorale(-1, "Die Gruppe ist sich uneins, ob dies richtig war."), FactionMemory("border-wardens", "outsiders disturbed a marked grave", "Die Grenzwaechter koennten diese Stoerung erinnern.") }) }),
            new LocationActionDefinition(ActionMark, "Markieren", "Eine eigene Kartenmarkierung anlegen.", outcomes: new[] { new LocationOutcomeDefinition("success", "Erfolg", new[] { Archive("Die Fundstelle wurde fuer spaeter markiert.") }) }),
            new LocationActionDefinition(ActionLeave, "Verlassen", "Die Fundstelle unangetastet lassen.", outcomes: new[] { new LocationOutcomeDefinition("success", "Erfolg", new[] { Archive("Die Expedition liess die Fundstelle unangetastet.") }) })
        };
    }

    private static IEnumerable<LocationRequirementDefinition> Adjacent()
    {
        return new[] { new LocationRequirementDefinition("req-adjacent", LocationRequirementKind.PositionOnOrAdjacent, unmetReason: "Die Expedition muss am Ort oder angrenzend sein.") };
    }

    private static LocationRequirementDefinition Operational(params string[] stateIdsAndReason)
    {
        var reason = stateIdsAndReason[^1];
        var states = new List<string>();
        for (var i = 0; i < stateIdsAndReason.Length - 1; i++)
        {
            states.Add(stateIdsAndReason[i]);
        }

        return new LocationRequirementDefinition("req-operational", LocationRequirementKind.OperationalStateAny, states, unmetReason: reason);
    }

    private static LocationRequirementDefinition Role(ExpeditionMemberRole role, string reason)
    {
        return new LocationRequirementDefinition($"req-role-{role}", LocationRequirementKind.RolePresent, requiredRole: role, unmetReason: reason);
    }

    private static LocationRequirementDefinition Anchor(LocationAnchorKind kind, string reason)
    {
        return new LocationRequirementDefinition($"req-anchor-{kind}", LocationRequirementKind.AnchorKind, requiredAnchorKind: kind, unmetReason: reason);
    }

    private static LocationEffectDefinition ChangeOperational(string stateId, string text)
    {
        return new LocationEffectDefinition($"effect-operational-{stateId}", LocationEffectKind.ChangeLocationState, text, LocationStateChannels.Operational, stateId);
    }

    private static LocationEffectDefinition ChangeInteraction(string stateId, string text)
    {
        return new LocationEffectDefinition($"effect-interaction-{stateId}", LocationEffectKind.ChangeLocationState, text, LocationStateChannels.Interaction, stateId);
    }

    private static LocationEffectDefinition Knowledge(int amount, string text)
    {
        return new LocationEffectDefinition($"effect-knowledge-{amount}", LocationEffectKind.AddUnsecuredKnowledge, text, amount: amount);
    }

    private static LocationEffectDefinition ConsumeSupplies(int amount, string text)
    {
        return new LocationEffectDefinition($"effect-supplies-{amount}", LocationEffectKind.ConsumeSupplies, text, amount: amount);
    }

    private static LocationEffectDefinition ChangeMorale(int amount, string text)
    {
        return new LocationEffectDefinition($"effect-morale-{amount}", LocationEffectKind.ChangeMorale, text, amount: amount);
    }

    private static LocationEffectDefinition FactionMemory(string factionId, string memory, string text)
    {
        return new LocationEffectDefinition($"effect-memory-{factionId}", LocationEffectKind.AddFactionMemory, text, factionId: factionId, memory: memory);
    }

    private static LocationEffectDefinition Archive(string text)
    {
        return new LocationEffectDefinition("effect-archive", LocationEffectKind.AddArchiveEntry, text);
    }

    private static LocationEffectDefinition OpenRoute(string text)
    {
        return new LocationEffectDefinition("effect-open-route", LocationEffectKind.OpenRoute, text);
    }
}
}
