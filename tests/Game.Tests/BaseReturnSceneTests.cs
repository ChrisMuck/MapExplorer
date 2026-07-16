#nullable enable
using System;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class BaseReturnSceneTests
{
    public void RunAll()
    {
        CompleteReturnWithoutFindingsComposesBothFacts();
        PartialReturnUsesLossSceneWithoutEmptyHandedClaim();
    }

    private static void CompleteReturnWithoutFindingsComposesBothFacts()
    {
        var app = CreateApplication();
        var result = Completed("all-returned", returnedFindings: 0);

        var scene = app.GetBaseReturnPresentation(result)!;
        var ids = FragmentIds(scene);

        AssertTrue(ids.Contains("frag-base-return-complete", StringComparer.Ordinal),
            "Complete team outcome selects authored return wording");
        AssertTrue(ids.Contains("frag-base-return-empty-handed", StringComparer.Ordinal),
            "No returned findings selects authored empty-handed wording");
        AssertTrue(scene.Title.Contains("1", StringComparison.Ordinal) && scene.Subtitle!.Contains("4", StringComparison.Ordinal),
            "Localized title and subtitle interpolate delivered completion facts");
    }

    private static void PartialReturnUsesLossSceneWithoutEmptyHandedClaim()
    {
        var app = CreateApplication();
        var scene = app.GetBaseReturnPresentation(Completed("partial-return", returnedFindings: 0))!;
        var ids = FragmentIds(scene);

        AssertTrue(ids.Contains("frag-base-return-losses", StringComparer.Ordinal),
            "Partial team outcome selects loss wording");
        AssertFalse(ids.Contains("frag-base-return-empty-handed", StringComparer.Ordinal),
            "Loss scene is not diluted by the complete-team empty-handed fragment");
    }

    private static CompleteExpeditionResult Completed(string teamOutcome, int returnedFindings) =>
        CompleteExpeditionResult.Completed(HexCoord.Zero, 1, 5, 4, 7, 9, 9,
            returnedFindings, teamOutcome, "Expedition archived.");

    private static GameApplication CreateApplication()
    {
        var catalog = GameDataCatalog.LoadFromDirectory(Path.Combine(Directory.GetCurrentDirectory(),
            "UnityHexMapView", "Assets", "StreamingAssets", "GameData"))!;
        return new GameApplication(catalog);
    }

    private static string[] FragmentIds(SceneDescriptionResult scene) => scene.Paragraphs
        .SelectMany(paragraph => paragraph.FragmentIds).ToArray();

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);
}
