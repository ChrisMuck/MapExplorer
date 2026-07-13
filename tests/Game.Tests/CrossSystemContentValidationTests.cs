#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Game.App;
using Game.Core;

internal sealed class CrossSystemContentValidationTests
{
    public void RunAll()
    {
        AuthoredGameDataPassesCrossSystemValidation();
        UnknownOutcomeEffectReferencesAreRejected();
        UnknownProjectCompletionReferencesAreRejected();
    }

    private static void AuthoredGameDataPassesCrossSystemValidation()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        var locations = LocationDataLoader.LoadFromDirectory(Path.Combine(root, "Locations"));
        var crossSystem = CrossSystemDataLoader.LoadFromDirectories(new[]
        {
            Path.Combine(root, "World"),
            Path.Combine(root, "Factions"),
            Path.Combine(root, "Scouting")
        });

        CrossSystemContentValidator.Validate(
            locations ?? throw new InvalidOperationException("Location content was not loaded."),
            crossSystem ?? throw new InvalidOperationException("Cross-system content was not loaded."));
    }

    private static void UnknownOutcomeEffectReferencesAreRejected()
    {
        var crossSystem = CreateCrossSystemData();

        AssertThrows(
            () => CrossSystemContentValidator.Validate(
                CreateLocationData(new LocationEffectDefinition("effect-evidence", LocationEffectKind.AddEvidence, "Missing evidence.", referenceId: "evidence-missing")),
                crossSystem),
            "Outcome evidence references must resolve");

        AssertThrows(
            () => CrossSystemContentValidator.Validate(
                CreateLocationData(new LocationEffectDefinition("effect-trigger", LocationEffectKind.RaiseWorldTrigger, "Missing trigger.", referenceId: "trigger-missing")),
                crossSystem),
            "Outcome trigger references must resolve");

        AssertThrows(
            () => CrossSystemContentValidator.Validate(
                CreateLocationData(new LocationEffectDefinition("effect-consequence", LocationEffectKind.ScheduleConsequence, "Missing consequence.", referenceId: "consequence-missing")),
                crossSystem),
            "Outcome consequence references must resolve");
    }

    private static void UnknownProjectCompletionReferencesAreRejected()
    {
        var projectEffect = new LocationEffectDefinition(
            "effect-project-evidence",
            LocationEffectKind.AddEvidence,
            "Missing project evidence.",
            referenceId: "evidence-missing");

        AssertThrows(
            () => CrossSystemContentValidator.Validate(CreateLocationData(projectEffect, projectEffect: true), CreateCrossSystemData()),
            "Project completion evidence references must resolve");
    }

    private static LocationDataBundle CreateLocationData(LocationEffectDefinition effect, bool projectEffect = false)
    {
        var action = new LocationActionDefinition(
            "action-test",
            "Test action",
            "Test description",
            outcomeTableId: projectEffect ? null : "outcome-test",
            startsProject: projectEffect,
            projectDurationDays: projectEffect ? 1 : 0,
            projectCompletionEffects: projectEffect ? new[] { effect } : null,
            actionTags: new[] { "test" });

        var outcomeTables = projectEffect
            ? Array.Empty<LocationOutcomeTableDefinition>()
            : new[]
            {
                new LocationOutcomeTableDefinition(
                    "outcome-test",
                    new Dictionary<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>>
                    {
                        [LocationRiskBand.Low] = new[] { new LocationOutcomeTierWeight(LocationOutcomeTier.Success, 100) }
                    },
                    new Dictionary<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>>
                    {
                        [LocationOutcomeTier.Success] = new[] { effect }
                    })
            };

        return new LocationDataBundle(
            new LocationInteractionDefinitionSet(
                Array.Empty<LocationArchetypeDefinition>(),
                Array.Empty<LocationVariantDefinition>(),
                Array.Empty<LocationModifierDefinition>(),
                new[] { action },
                outcomeTables),
            Array.Empty<SpecialLocationState>());
    }

    private static CrossSystemDataBundle CreateCrossSystemData()
    {
        return new CrossSystemDataBundle(
            new EvidenceDefinitionSet(new[] { new EvidenceDefinition("evidence-known", "Known evidence.") }),
            triggers: new[] { new WorldTriggerDefinition("trigger-known", "consequence-known") },
            consequences: new[]
            {
                new ConsequenceDefinition(
                    "consequence-known",
                    new[] { new ConsequenceStageDefinition("stage-known", 1, null, null, null) })
            });
    }

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (LocationDataException)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: expected LocationDataException.");
    }
}
