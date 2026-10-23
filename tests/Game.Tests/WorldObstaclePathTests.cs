#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class WorldObstaclePathTests
{
    public void RunAll()
    {
        TutorialRavineIsAChasmLineThatSupportsItsBlockingEdge();
        EdgeWithoutAnyLineIsNotSupported();
        CrossingLineRunsAcrossTheMiddleOfTheEdge();
        EdgeOnARiverIsSupportedWithoutAChasm();
        GeneratedWorldsDrawALineForEveryBlockingEdge();
        GeneratedBrokenBridgesKeepTheirCatalogVariant();
    }

    private static void TutorialRavineIsAChasmLineThatSupportsItsBlockingEdge()
    {
        var game = TutorialGameFactory.Create();
        var chasms = game.World.Paths.Where(path => path.Kind == WorldPathKind.Chasm).ToList();

        AssertEqual(1, chasms.Count, "Tutorial has exactly one ravine line");
        AssertEqual("chasm-gray", chasms[0].Id, "Tutorial ravine keeps a traceable id");
        AssertEqual(0, WorldObstaclePathRules.Validate(game.World).Count, "Every tutorial blocking edge has a river or chasm line");
        AssertEqual(0, WorldObstaclePathRules.FindUnusedChasms(game.World).Count, "The tutorial ravine line is used by its obstacle");
        AssertTrue(game.World.Locations.Any(WorldObstaclePathRules.IsRouteObstacleEdge), "Tutorial still has a blocking edge location");
    }

    private static void EdgeWithoutAnyLineIsNotSupported()
    {
        var a = new HexCoord(5, 5);
        var b = new HexCoord(6, 5);
        var road = new WorldPathState("road-1", WorldPathKind.Road, new[] { new HexCoord(0, 0), new HexCoord(1, 0) });
        var wall = new WorldPathState("wall-1", WorldPathKind.Wall, new[] { a, b });

        AssertTrue(!WorldObstaclePathRules.PathSupportsEdge(Array.Empty<WorldPathState>(), a, b), "No paths means no line");
        AssertTrue(!WorldObstaclePathRules.PathSupportsEdge(new[] { road, wall }, a, b), "Roads and walls do not explain a blocked crossing");
    }

    private static void CrossingLineRunsAcrossTheMiddleOfTheEdge()
    {
        var a = new HexCoord(10, 10);
        var b = new HexCoord(11, 10);
        var bounds = new HexMapBounds(30, 30);
        var line = WorldObstaclePathRules.CrossingLine(a, b, bounds);

        AssertEqual(2, line.Count, "Crossing line has two end hexes");
        AssertTrue(line.All(coord => coord.DistanceTo(a) == 1 && coord.DistanceTo(b) == 1), "Both ends neighbor both sides of the edge");
        AssertEqual(2, line[0].DistanceTo(line[1]), "The line crosses the edge instead of following it");

        var chasm = new WorldPathState("chasm-1", WorldPathKind.Chasm, line);
        AssertTrue(WorldObstaclePathRules.PathSupportsEdge(new[] { chasm }, a, b), "A crossing line supports its edge");

        var corner = WorldObstaclePathRules.CrossingLine(new HexCoord(0, 0), new HexCoord(1, 0), bounds);
        AssertTrue(corner.Count == 2, "A crossing at the map border still yields a usable line");
    }

    private static void EdgeOnARiverIsSupportedWithoutAChasm()
    {
        var a = new HexCoord(4, 4);
        var b = new HexCoord(5, 4);
        var river = new WorldPathState("river-1", WorldPathKind.River, new[] { new HexCoord(3, 4), a, b, new HexCoord(6, 4) });

        AssertTrue(WorldObstaclePathRules.PathSupportsEdge(new[] { river }, a, b), "A crossing along a river is already drawn");
    }

    private static void GeneratedWorldsDrawALineForEveryBlockingEdge()
    {
        var edgeObstacles = 0;
        var chasms = 0;
        foreach (var seed in new uint[] { 1, 7, 42, 99, 2026, 20260712, 424242, 777 })
        {
            var request = new WorldGenerationRequest { Seed = seed, Width = 40, Height = 30, FactionCount = 3 };
            var result = new WorldGenBridge().Generate(request);
            var issues = WorldObstaclePathRules.Validate(result.World);

            AssertEqual(0, issues.Count, $"Seed {seed}: every blocking edge has a line ({string.Join("; ", issues)})");
            AssertEqual(0, WorldObstaclePathRules.FindUnusedChasms(result.World).Count, $"Seed {seed}: no chasm line without an obstacle");
            edgeObstacles += result.World.Locations.Count(WorldObstaclePathRules.IsRouteObstacleEdge);
            chasms += result.World.Paths.Count(path => path.Kind == WorldPathKind.Chasm);
        }

        AssertTrue(edgeObstacles > 0, "The tested seeds produce route obstacles");
        AssertTrue(chasms <= edgeObstacles, "At most one chasm line per blocking edge");
    }

    private static void GeneratedBrokenBridgesKeepTheirCatalogVariant()
    {
        var bridges = 0;
        foreach (var seed in new uint[] { 1, 7, 42, 99, 2026, 20260712, 424242, 777, 31, 52, 63, 84 })
        {
            var request = new WorldGenerationRequest { Seed = seed, Width = 40, Height = 30, FactionCount = 3 };
            var result = new WorldGenBridge().Generate(request);
            foreach (var location in result.World.Locations.Where(location => location.Name == "Zerstörte Brücke"))
            {
                bridges++;
                AssertEqual("broken-bridge", location.VariantId, $"Seed {seed}: generated bridge maps to the catalog variant");
            }
        }

        AssertTrue(bridges > 0, "The tested seeds include at least one generated broken bridge");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message}: expected true.");
        }
    }
}
