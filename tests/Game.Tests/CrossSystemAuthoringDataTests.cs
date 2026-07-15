#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.App;

internal sealed class CrossSystemAuthoringDataTests
{
    public void RunAll()
    {
        TargetDefinitionsLoadAndValidateAgainstCurrentContent();
        UnknownScenarioReferencesFailWithAnActionableError();
        ScenarioStateTransitionOutsideProfileFails();
        MaterialEconomyOfferIsRejected();
        AuthoredOffersResolveForTheGeneratedFactionProfile();
    }

    private static void TargetDefinitionsLoadAndValidateAgainstCurrentContent()
    {
        var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"location-state-profiles", "schemaVersion":2, "items":[{ "id":"state-test-crossing", "archetypeId":"route-obstacle", "channels":{ "interaction":{ "initial":"untouched", "values":["untouched","inspected"] }, "operational":{ "initial":"blocked", "values":["blocked","risky-passage","open"] } } }] }""",
            """{ "documentType":"findings", "schemaVersion":2, "items":[{ "id":"finding-test-sample", "category":"sample", "fieldDescription":"A test sample.", "requiresPhysicalReturn":true, "analysis":{ "durationDays":{ "min":1, "max":2 }, "knowledgePoints":{ "min":2, "max":3 }, "archiveKind":"insight", "explanation":"The base can study it." } }] }""",
            """{ "documentType":"context-definitions", "schemaVersion":2, "items":[{ "id":"context-test-repair", "applicableArchetypeIds":["route-obstacle"], "contextTags":["structural-failure"], "candidateEvidenceIds":["evidence-old-structure"] }] }""",
            """{ "documentType":"situation-definitions", "schemaVersion":2, "items":[{ "id":"situation-test-request", "kind":"request", "possibleSourceKinds":["individual"], "urgencyLabels":["soon"], "responseOptions":[{ "id":"accept", "actionTag":"assist" }], "resolutionTags":["help-provided"] }, { "id":"situation-investigate-disturbance", "kind":"warning", "possibleSourceKinds":["world"], "urgencyLabels":["soon"], "responseOptions":[{ "id":"investigate", "actionTag":"investigate" }], "resolutionTags":["resolved"] }, { "id":"situation-investigate-opened-seal", "kind":"warning", "possibleSourceKinds":["world"], "urgencyLabels":["soon"], "responseOptions":[{ "id":"contain", "actionTag":"contain" }], "resolutionTags":["resolved"] }, { "id":"situation-route-use-changed", "kind":"notice", "possibleSourceKinds":["world"], "urgencyLabels":["soon"], "responseOptions":[{ "id":"coordinate", "actionTag":"organize-help" }], "resolutionTags":["coordinated"] }] }""",
            """{ "documentType":"location-scenario-profiles", "schemaVersion":2, "items":[{ "id":"scenario-test-crossing", "archetypeId":"route-obstacle", "variantId":"broken-bridge", "stateProfileId":"state-test-crossing", "contentProfileId":"content-old-trade-road-bridge", "worldgen":{ "anchorKinds":["Edge"], "claimEligibility":"if-inside-current-territory" }, "actionSet":{ "sharedActionIds":["action-assess-crossing"], "initialAdditionalActionIds":[], "maximumVisibleAdditionalActions":3, "contextActionRules":[{ "whenKnownContextTagsAny":["structural-failure"], "addActionIds":["action-construct-temporary-passage"], "knownRequirementDisclosure":"show-disabled-when-missing" }] }, "evidencePoolIds":["evidence-old-structure"], "findingPoolIds":["finding-test-sample"], "consequencePoolIds":["consequence-route-restored"], "connectionTags":["crossing"] }] }"""
        });

        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");
        CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, authoring);

        AssertEqual("blocked", authoring.StateProfiles["state-test-crossing"].Channels["operational"].InitialValue, "State profile initial value");
        AssertEqual(2, authoring.Findings["finding-test-sample"].Analysis.MinKnowledgePoints, "Finding analysis Knowledge minimum");
        AssertEqual("assist", authoring.Situations["situation-test-request"].ResponseActionTags[0], "Situation action tag");
    }

    private static void UnknownScenarioReferencesFailWithAnActionableError()
    {
        var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"location-state-profiles", "schemaVersion":2, "items":[{ "id":"state-test-crossing", "archetypeId":"route-obstacle", "channels":{ "operational":{ "initial":"blocked", "values":["blocked"] } } }] }""",
            """{ "documentType":"location-scenario-profiles", "schemaVersion":2, "items":[{ "id":"scenario-invalid", "archetypeId":"route-obstacle", "variantId":"broken-bridge", "stateProfileId":"state-test-crossing", "contentProfileId":"content-old-trade-road-bridge", "worldgen":{ "anchorKinds":["Edge"], "claimEligibility":"if-inside-current-territory" }, "actionSet":{ "sharedActionIds":["action-not-authored"], "initialAdditionalActionIds":[], "maximumVisibleAdditionalActions":3 } }] }"""
        });
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");

        AssertThrows(() => CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, authoring), "Unknown scenario action must be rejected");
    }

    private static void MaterialEconomyOfferIsRejected()
    {
        var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"faction-offers", "schemaVersion":2, "items":[{ "id":"offer-invalid-material", "title":"Invalid", "description":"Invalid", "eligibleProfileTags":["trade"], "knowledgePointCost":1, "effects":[{ "kind":"grant-material-stockpile" }], "repeatPolicy":"once-per-faction" }] }"""
        });
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");

        AssertThrows(() => CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, authoring), "Material economy offer must be rejected");
    }

    private static void ScenarioStateTransitionOutsideProfileFails()
    {
        var authoring = CrossSystemAuthoringDataLoader.LoadFromJson(new[]
        {
            """{ "documentType":"location-state-profiles", "schemaVersion":2, "items":[{ "id":"state-invalid-transition", "archetypeId":"route-obstacle", "channels":{ "operational":{ "initial":"blocked", "values":["blocked"] } } }] }""",
            """{ "documentType":"location-scenario-profiles", "schemaVersion":2, "items":[{ "id":"scenario-invalid-transition", "archetypeId":"route-obstacle", "variantId":"broken-bridge", "stateProfileId":"state-invalid-transition", "contentProfileId":"content-old-trade-road-bridge", "worldgen":{ "anchorKinds":["Edge"], "claimEligibility":"if-inside-current-territory" }, "actionSet":{ "sharedActionIds":["action-assess-crossing"], "initialAdditionalActionIds":[], "maximumVisibleAdditionalActions":3 } }] }"""
        });
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");

        AssertThrows(() => CrossSystemContentValidator.Validate(catalog.Locations, catalog.CrossSystem, authoring),
            "A scenario action may not write a state outside its profile");
    }

    private static void AuthoredOffersResolveForTheGeneratedFactionProfile()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(GameDataRoot()) ?? throw new InvalidOperationException("Game data was not loaded.");
        var application = new GameApplication(catalog);
        var game = application.CreateTutorialGame();
        game.Base.AddKnowledgePoints(15);
        var medicineBefore = game.Expedition.Medicine;

        var opened = application.OpenFactionInteraction(game, "coastal-people", game.Expedition.Position);
        var supplies = application.PurchaseFactionOffer(game, "offer-knowledge-for-supplies");
        var medicine = application.PurchaseFactionOffer(game, "offer-knowledge-for-medicine");

        AssertTrue(opened.Success, "Profile-based faction interaction opens");
        AssertTrue(game.ActiveFactionInteraction!.Offers.Any(offer => offer.Id == "offer-knowledge-for-supplies"), "Trade profile receives supply offer");
        AssertTrue(game.ActiveFactionInteraction.Offers.Any(offer => offer.Id == "offer-knowledge-for-medicine"), "Help profile receives medicine offer");
        AssertTrue(supplies.Success, "Knowledge buys authored supplies");
        AssertTrue(medicine.Success, "Knowledge buys authored medicine");
        AssertEqual(medicineBefore + 1, game.Expedition.Medicine, "Medicine offer changes only expedition medicine");
        AssertEqual(4, game.Base.KnowledgePoints, "Both authored offers spend Knowledge Points");
    }

    private static string GameDataRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        if (!Directory.Exists(root)) throw new InvalidOperationException("Game-data root was not found for tests.");
        return root;
    }

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (LocationDataException) { return; }
        throw new InvalidOperationException($"{message}: expected {nameof(LocationDataException)}.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"{message}: expected true.");
    }
}
