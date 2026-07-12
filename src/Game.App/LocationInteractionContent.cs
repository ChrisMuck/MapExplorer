#nullable enable
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

/// <summary>
/// In-code fallback for the location interaction definitions (concept Section 17.10: "Hardcoded
/// fallback definitions are acceptable during migration"). JSON under StreamingAssets is authoritative
/// when present; this mirrors the same content so tests and the Editor still work without the data
/// files, and so the stable ID constants have a single home the Unity layer can reference.
/// </summary>
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

    public const string ContentBridge = "content-old-trade-road-bridge";
    public const string ContentMarkedGrave = "content-marked-grave";

    public static LocationInteractionDefinitionSet CreateDefinitionSet()
    {
        return new LocationInteractionDefinitionSet(
            CreateArchetypes(),
            CreateVariants(),
            CreateModifiers(),
            CreateActions(),
            CreateOutcomeTables(),
            CreateContentProfiles());
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
                outcomeTableId: "outcome-assess-crossing",
                repeatPolicy: LocationActionRepeatPolicy.OncePerLocation),

            new LocationActionDefinition(
                ActionFindBypass,
                "Umweg suchen",
                "Eine Route um die Schlucht herum suchen. Das kostet Zeit, kann aber den gefaehrlichen Uebergang vermeiden.",
                hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(15, confidence: LocationEstimateConfidence.Guess),
                outcomeTableId: "outcome-find-bypass",
                costs: new[] { new LocationCostDefinition(LocationCostKind.MovementPoints, 1) },
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
                outcomeTableId: "outcome-construct-temporary-passage",
                costs: new[] { new LocationCostDefinition(LocationCostKind.Supplies, 1) },
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
                outcomeTableId: "outcome-attempt-crossing",
                costs: new[] { new LocationCostDefinition(LocationCostKind.MovementPoints, 1) },
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
                outcomeTableId: "outcome-inspect"),
            new LocationActionDefinition(ActionDocument, "Dokumentieren", "Zeichen, Lage und Zustand fuer spaetere Auswertung festhalten.", hardRequirements: Adjacent(),
                outcomeTableId: "outcome-document"),
            new LocationActionDefinition(ActionInvestigate, "Untersuchen", "Den Ort eingehender untersuchen und eine Deutung versuchen.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(25, confidence: LocationEstimateConfidence.Guess),
                outcomeTableId: "outcome-investigate"),
            new LocationActionDefinition(ActionLeaveOffering, "Opfergabe hinterlassen", "Den Ort respektvoll behandeln, bevor weitere Schritte unternommen werden.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(5, confidence: LocationEstimateConfidence.Assessed),
                outcomeTableId: "outcome-leave-offering"),
            new LocationActionDefinition(ActionDisturb, "Stoeren", "Das Grab oder Siegel trotz Warnzeichen oeffnen.", hardRequirements: Adjacent(),
                riskProfile: new LocationRiskProfileDefinition(45, confidence: LocationEstimateConfidence.Assessed),
                outcomeTableId: "outcome-disturb"),
            new LocationActionDefinition(ActionMark, "Markieren", "Eine eigene Kartenmarkierung anlegen.", outcomeTableId: "outcome-mark"),
            new LocationActionDefinition(ActionLeave, "Verlassen", "Die Fundstelle unangetastet lassen.", outcomeTableId: "outcome-leave")
        };
    }

    private static IEnumerable<LocationOutcomeTableDefinition> CreateOutcomeTables()
    {
        return new[]
        {
            Table("outcome-assess-crossing", ArchetypeRouteObstacle, ActionAssessCrossing,
                Bands(Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 100))),
                Bundle(LocationOutcomeTier.Success,
                    ChangeInteraction(LocationStateIds.Interaction.Inspected, "Die Bruecke ist als instabiler, aber reparierbarer Uebergang dokumentiert."),
                    Knowledge(2, "Die Expedition gewinnt verwertbare Routenkenntnis."))),

            Table("outcome-find-bypass", ArchetypeRouteObstacle, ActionFindBypass,
                Bands(
                    Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 80), W(LocationOutcomeTier.Failure, 20)),
                    Row(LocationRiskBand.Moderate, W(LocationOutcomeTier.Success, 60), W(LocationOutcomeTier.Failure, 40))),
                Bundle(LocationOutcomeTier.Success,
                    Knowledge(3, "Ein langsamer, aber nutzbarer Umweg wird notiert."),
                    OpenRoute("Der gefundene Umweg oeffnet eine nutzbare Route um das Hindernis."),
                    Archive("Ein Umweg um die zerstoerte Bruecke wurde als Routenhypothese vermerkt.")),
                Bundle(LocationOutcomeTier.Failure,
                    ConsumeSupplies(1, "Die Suche verbraucht zusaetzliche Vorraete, ohne einen sicheren Weg zu finden."))),

            Table("outcome-construct-temporary-passage", ArchetypeRouteObstacle, ActionConstructTemporaryPassage,
                Bands(
                    Row(LocationRiskBand.Low, W(LocationOutcomeTier.SuccessWithCost, 85), W(LocationOutcomeTier.Failure, 15)),
                    Row(LocationRiskBand.Moderate, W(LocationOutcomeTier.SuccessWithCost, 65), W(LocationOutcomeTier.Failure, 35)),
                    Row(LocationRiskBand.High, W(LocationOutcomeTier.SuccessWithCost, 45), W(LocationOutcomeTier.Failure, 55))),
                Bundle(LocationOutcomeTier.SuccessWithCost,
                    ChangeOperational(LocationStateIds.Operational.RiskyPassage, "Der operative Zustand wechselt zu: Riskante Passage."),
                    ConsumeSupplies(1, "Seile und Material werden abgenutzt."),
                    Archive("Ein provisorischer Seiluebergang wurde an der zerstoerten Bruecke eingerichtet.")),
                Bundle(LocationOutcomeTier.Failure,
                    ConsumeSupplies(1, "Material geht beim Bauversuch verloren."),
                    ChangeMorale(-1, "Die Gruppe verliert Vertrauen in die Querung."))),

            Table("outcome-attempt-crossing", ArchetypeRouteObstacle, ActionAttemptCrossing,
                Bands(
                    Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 80), W(LocationOutcomeTier.SuccessWithCost, 20)),
                    Row(LocationRiskBand.Moderate, W(LocationOutcomeTier.Success, 60), W(LocationOutcomeTier.SuccessWithCost, 30), W(LocationOutcomeTier.Failure, 10)),
                    Row(LocationRiskBand.High, W(LocationOutcomeTier.Success, 30), W(LocationOutcomeTier.SuccessWithCost, 30), W(LocationOutcomeTier.Failure, 25), W(LocationOutcomeTier.SevereFailure, 15)),
                    Row(LocationRiskBand.Extreme, W(LocationOutcomeTier.Success, 15), W(LocationOutcomeTier.SuccessWithCost, 25), W(LocationOutcomeTier.Failure, 35), W(LocationOutcomeTier.SevereFailure, 25))),
                Bundle(LocationOutcomeTier.Success,
                    Knowledge(2, "Die Expedition bestaetigt die Route ueber die Schlucht."),
                    MoveAcrossEdge("Die Expedition erreicht die andere Seite des Hindernisses.")),
                Bundle(LocationOutcomeTier.SuccessWithCost,
                    MoveAcrossEdge("Die Expedition erreicht die andere Seite des Hindernisses."),
                    ConsumeSupplies(1, "Beim Uebergang geht Ausruestung verloren."),
                    ChangeMorale(-1, "Der Uebergang belastet die Gruppe.")),
                Bundle(LocationOutcomeTier.Failure,
                    ChangeMorale(-1, "Der Versuch scheitert und erschuettert die Gruppe.")),
                Bundle(LocationOutcomeTier.SevereFailure,
                    Injure("Ein Expeditionsmitglied stuerzt bei der Querung."))),

            Table("outcome-inspect", ArchetypeInvestigationSite, ActionInspect,
                Bands(Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 100))),
                Bundle(LocationOutcomeTier.Success,
                    ChangeInteraction(LocationStateIds.Interaction.Inspected, "Der Ort wurde inspiziert."),
                    Knowledge(2, "Ein Hinweis wurde gesichert."))),

            Table("outcome-document", ArchetypeInvestigationSite, ActionDocument,
                Bands(Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 100))),
                Bundle(LocationOutcomeTier.Success,
                    Archive("Die Fundstelle wurde dokumentiert."))),

            Table("outcome-investigate", ArchetypeInvestigationSite, ActionInvestigate,
                Bands(
                    Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 40), W(LocationOutcomeTier.PartialResult, 60)),
                    Row(LocationRiskBand.Moderate, W(LocationOutcomeTier.Success, 30), W(LocationOutcomeTier.PartialResult, 55), W(LocationOutcomeTier.Failure, 15))),
                Bundle(LocationOutcomeTier.Success,
                    ChangeInteraction(LocationStateIds.Interaction.Investigated, "Die Untersuchung liefert eine belastbare Deutung."),
                    Knowledge(4, "Gesichertes Fundstellenwissen wurde gewonnen.")),
                Bundle(LocationOutcomeTier.PartialResult,
                    ChangeInteraction(LocationStateIds.Interaction.Investigated, "Die Untersuchung liefert Hinweise, aber keine sichere Deutung."),
                    Knowledge(3, "Unsicheres Fundstellenwissen wurde gewonnen.")),
                Bundle(LocationOutcomeTier.Failure,
                    ChangeMorale(-1, "Die Untersuchung bleibt ergebnislos und zehrt an der Gruppe."))),

            Table("outcome-leave-offering", ArchetypeInvestigationSite, ActionLeaveOffering,
                Bands(Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 100))),
                Bundle(LocationOutcomeTier.Success,
                    FactionMemory("border-wardens", "outsiders respected a marked grave", "Der respektvolle Umgang koennte erinnert werden."))),

            Table("outcome-disturb", ArchetypeInvestigationSite, ActionDisturb,
                Bands(
                    Row(LocationRiskBand.Moderate, W(LocationOutcomeTier.Success, 40), W(LocationOutcomeTier.SuccessWithCost, 40), W(LocationOutcomeTier.Failure, 20)),
                    Row(LocationRiskBand.High, W(LocationOutcomeTier.Success, 15), W(LocationOutcomeTier.SuccessWithCost, 35), W(LocationOutcomeTier.Failure, 30), W(LocationOutcomeTier.SevereFailure, 20)),
                    Row(LocationRiskBand.Extreme, W(LocationOutcomeTier.SuccessWithCost, 30), W(LocationOutcomeTier.Failure, 40), W(LocationOutcomeTier.SevereFailure, 30))),
                Bundle(LocationOutcomeTier.Success,
                    Knowledge(5, "Ein Fund wird geborgen.")),
                Bundle(LocationOutcomeTier.SuccessWithCost,
                    Knowledge(5, "Ein Fund wird geborgen."),
                    ChangeMorale(-1, "Die Gruppe ist sich uneins, ob dies richtig war."),
                    FactionMemory("border-wardens", "outsiders disturbed a marked grave", "Die Grenzwaechter koennten diese Stoerung erinnern.")),
                Bundle(LocationOutcomeTier.Failure,
                    ChangeMorale(-1, "Der Eingriff misslingt und belastet die Gruppe."),
                    FactionMemory("border-wardens", "outsiders disturbed a marked grave", "Die Grenzwaechter koennten diese Stoerung erinnern.")),
                Bundle(LocationOutcomeTier.SevereFailure,
                    Injure("Beim gewaltsamen Oeffnen wird jemand verletzt."),
                    FactionMemory("border-wardens", "outsiders disturbed a marked grave", "Die Grenzwaechter koennten diese Stoerung erinnern."))),

            Table("outcome-mark", ArchetypeInvestigationSite, ActionMark,
                Bands(Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 100))),
                Bundle(LocationOutcomeTier.Success,
                    Archive("Die Fundstelle wurde fuer spaeter markiert."))),

            Table("outcome-leave", ArchetypeInvestigationSite, ActionLeave,
                Bands(Row(LocationRiskBand.Low, W(LocationOutcomeTier.Success, 100))),
                Bundle(LocationOutcomeTier.Success,
                    Archive("Die Expedition liess die Fundstelle unangetastet.")))
        };
    }

    private static IEnumerable<LocationContentProfileDefinition> CreateContentProfiles()
    {
        return new[]
        {
            new LocationContentProfileDefinition(
                ContentBridge,
                "Zerstoerte Bruecke",
                subtitle: "Streckenhindernis",
                shortDescription: "Die alte Handelsbruecke ist eingestuerzt.",
                description: "Balken haengen schraeg ueber der Schlucht; zu instabil, um sie einfach zu betreten.",
                flavorByState: new Dictionary<string, string>
                {
                    [LocationStateIds.Operational.Blocked] = "Die Schlucht trennt die alte Handelsroute.",
                    [LocationStateIds.Operational.RiskyPassage] = "Ein provisorischer Uebergang haengt ueber der Schlucht.",
                    [LocationStateIds.Operational.Repaired] = "Die Bruecke ist wieder passierbar."
                },
                imageId: "placeholder-bridge",
                journalDiscovered: "Eine zerstoerte Bruecke blockiert die Route.",
                journalResolved: "Die Bruecke wurde als Routenproblem dokumentiert."),

            new LocationContentProfileDefinition(
                ContentMarkedGrave,
                "Markiertes Grab",
                subtitle: "Untersuchungsstelle",
                shortDescription: "Ein sorgfaeltig markiertes Grab am Wegesrand.",
                description: "Zeichen und Steine deuten auf eine Grenzwaechter-Bestattung hin.",
                flavorByState: new Dictionary<string, string>
                {
                    [LocationStateIds.Operational.Sealed] = "Das Grab ist verschlossen und mit Warnzeichen versehen.",
                    [LocationStateIds.Operational.Open] = "Das Grab wurde geoeffnet."
                },
                imageId: "placeholder-grave",
                journalDiscovered: "Ein markiertes Grab wurde entdeckt.",
                journalResolved: "Das markierte Grab wurde untersucht.")
        };
    }

    // ---- builders --------------------------------------------------------

    private static LocationOutcomeTableDefinition Table(
        string id,
        string archetypeId,
        string actionId,
        IReadOnlyDictionary<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>> bands,
        params KeyValuePair<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>>[] bundles)
    {
        var effectBundles = new Dictionary<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>>();
        foreach (var bundle in bundles)
        {
            effectBundles[bundle.Key] = bundle.Value;
        }

        return new LocationOutcomeTableDefinition(id, bands, effectBundles, archetypeId, actionId);
    }

    private static IReadOnlyDictionary<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>> Bands(
        params KeyValuePair<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>>[] rows)
    {
        var bands = new Dictionary<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>>();
        foreach (var row in rows)
        {
            bands[row.Key] = row.Value;
        }

        return bands;
    }

    private static KeyValuePair<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>> Row(
        LocationRiskBand band,
        params LocationOutcomeTierWeight[] weights)
    {
        return new KeyValuePair<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>>(band, weights);
    }

    private static LocationOutcomeTierWeight W(LocationOutcomeTier tier, int weight)
    {
        return new LocationOutcomeTierWeight(tier, weight);
    }

    private static KeyValuePair<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>> Bundle(
        LocationOutcomeTier tier,
        params LocationEffectDefinition[] effects)
    {
        return new KeyValuePair<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>>(tier, effects);
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

    private static LocationEffectDefinition MoveAcrossEdge(string text)
    {
        return new LocationEffectDefinition("effect-move-across-edge", LocationEffectKind.MoveExpeditionAcrossEdge, text);
    }

    private static LocationEffectDefinition Injure(string text)
    {
        return new LocationEffectDefinition("effect-injure", LocationEffectKind.InjureMember, text, selection: "randomActiveMember", severity: "Wounded");
    }
}
}
