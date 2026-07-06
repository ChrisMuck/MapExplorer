using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexMapPrefabGenerator
{
    private const string GeneratedRoot = "Assets/Prefabs/Generated";
    private const string MaterialRoot = "Assets/Materials/Generated";
    private const string LibraryPath = "Assets/Settings/DefaultHexMapPrefabLibrary.asset";
    private const string MeshLibraryPath = "Assets/Prefabs/Generated/GeneratedMeshes.asset";

    // SaveAsPrefabAsset does not persist procedurally generated meshes (the MeshFilter's
    // m_Mesh becomes null). We collect every generated mesh into this shared asset so the
    // prefabs reference real, saved meshes instead of empty shells.
    private static Mesh s_meshLibrary;

    [InitializeOnLoadMethod]
    private static void GenerateAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneratedRoot}/Forest/ForestCluster_Mixed_Dense.prefab"))
            {
                GenerateDefaultPrefabs();
            }
        };
    }

    [MenuItem("Hex Map/Generate Default Prefabs")]
    public static void GenerateDefaultPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Generated");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Generated");

        PrepareMeshLibrary();

        // Remove prefabs from the previous naming scheme so they do not linger as broken assets.
        DeleteObsoletePrefab($"{GeneratedRoot}/Forest/Tree_Round.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Forest/ForestCluster_Dense.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Forest/ForestCluster_Edge.prefab");

        var materials = CreateMaterials();

        var treePine = SavePrefab($"{GeneratedRoot}/Forest/Tree_Pine.prefab", CreatePineTree("Tree_Pine", materials, 1f));
        var treeBroadleaf = SavePrefab($"{GeneratedRoot}/Forest/Tree_Broadleaf.prefab", CreateBroadleafTree("Tree_Broadleaf", materials, 1f));
        var treeTall = SavePrefab($"{GeneratedRoot}/Forest/Tree_Tall.prefab", CreatePineTree("Tree_Tall", materials, 1.22f));

        var coniferDense = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Conifer_Dense.prefab", CreateForestCluster("ForestCluster_Conifer_Dense", materials, 9, ForestMode.Conifer));
        var coniferEdge = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Conifer_Edge.prefab", CreateForestCluster("ForestCluster_Conifer_Edge", materials, 6, ForestMode.Conifer));
        var deciduousDense = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Deciduous_Dense.prefab", CreateForestCluster("ForestCluster_Deciduous_Dense", materials, 9, ForestMode.Deciduous));
        var deciduousEdge = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Deciduous_Edge.prefab", CreateForestCluster("ForestCluster_Deciduous_Edge", materials, 6, ForestMode.Deciduous));
        var mixedDense = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Mixed_Dense.prefab", CreateForestCluster("ForestCluster_Mixed_Dense", materials, 9, ForestMode.Mixed));
        var mixedEdge = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Mixed_Edge.prefab", CreateForestCluster("ForestCluster_Mixed_Edge", materials, 6, ForestMode.Mixed));

        var mountainPeak = SavePrefab($"{GeneratedRoot}/Mountains/MountainPeak.prefab", CreateMountainPeak("MountainPeak", materials, false, 1f));
        var snowyPeak = SavePrefab($"{GeneratedRoot}/Mountains/MountainPeak_Snowy.prefab", CreateMountainPeak("MountainPeak_Snowy", materials, true, 1.05f));
        var ridge = SavePrefab($"{GeneratedRoot}/Mountains/RockyRidge.prefab", CreateRockyRidge("RockyRidge", materials, false));
        var snowyRidge = SavePrefab($"{GeneratedRoot}/Mountains/RockyRidge_Snowy.prefab", CreateRockyRidge("RockyRidge_Snowy", materials, true));
        var foothills = SavePrefab($"{GeneratedRoot}/Mountains/Foothills.prefab", CreateFoothills("Foothills", materials));
        var foothillRock = SavePrefab($"{GeneratedRoot}/Mountains/FoothillRock.prefab", CreateRock("FoothillRock", materials, 0.28f));
        var rock = SavePrefab($"{GeneratedRoot}/Mountains/Rock.prefab", CreateRock("Rock", materials, 0.36f));

        var settlement = SavePrefab($"{GeneratedRoot}/Settlements/SettlementCluster.prefab", CreateSettlementCluster("SettlementCluster", materials));
        var houseA = SavePrefab($"{GeneratedRoot}/Settlements/House_A.prefab", CreateHouse("House_A", materials, 1f));
        var houseB = SavePrefab($"{GeneratedRoot}/Settlements/House_B.prefab", CreateHouse("House_B", materials, 0.78f));
        var fence = SavePrefab($"{GeneratedRoot}/Settlements/Fence.prefab", CreateFence("Fence", materials));

        var tower = SavePrefab($"{GeneratedRoot}/Landmarks/Watchtower.prefab", CreateWatchtower("Watchtower", materials));
        var mine = SavePrefab($"{GeneratedRoot}/Landmarks/Mine.prefab", CreateMine("Mine", materials));
        var wallSegment = SavePrefab($"{GeneratedRoot}/Landmarks/WallSegment.prefab", CreateWallSegment("WallSegment", materials));
        var wallTower = SavePrefab($"{GeneratedRoot}/Landmarks/WallTower.prefab", CreateWallTower("WallTower", materials));
        var coastMarker = SavePrefab($"{GeneratedRoot}/Landmarks/CoastMarker.prefab", CreateCoastMarker("CoastMarker", materials));

        var library = AssetDatabase.LoadAssetAtPath<HexMapPrefabLibrary>(LibraryPath);
        if (library == null)
        {
            EnsureFolder("Assets", "Settings");
            library = ScriptableObject.CreateInstance<HexMapPrefabLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.forestClusterPrefabs = new[] { mixedDense, mixedEdge };
        library.treePrefabs = new[] { treePine, treeBroadleaf, treeTall };
        library.coniferousForestClusterPrefabs = new[] { coniferDense, coniferEdge };
        library.deciduousForestClusterPrefabs = new[] { deciduousDense, deciduousEdge };
        library.mixedForestClusterPrefabs = new[] { mixedDense, mixedEdge };
        library.pineTreePrefabs = new[] { treePine, treeTall };
        library.broadleafTreePrefabs = new[] { treeBroadleaf };
        library.mountainPeakPrefabs = new[] { mountainPeak };
        library.snowyMountainPeakPrefabs = new[] { snowyPeak };
        library.rockyRidgePrefabs = new[] { ridge };
        library.snowyRockyRidgePrefabs = new[] { snowyRidge };
        library.foothillsPrefabs = new[] { foothills };
        library.foothillRockPrefabs = new[] { foothillRock };
        library.rockPrefabs = new[] { rock, foothillRock };
        library.settlementPrefabs = new[] { settlement };
        library.settlementHousePrefabs = new[] { houseA, houseB };
        library.settlementFencePrefabs = new[] { fence };
        library.towerPrefabs = new[] { tower };
        library.minePrefabs = new[] { mine };
        library.wallSegmentPrefabs = new[] { wallSegment };
        library.wallTowerPrefabs = new[] { wallTower };
        library.coastMarkerPrefabs = new[] { coastMarker };

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated default hex map prefabs and updated DefaultHexMapPrefabLibrary.");
    }

    private static MaterialSet CreateMaterials()
    {
        return new MaterialSet
        {
            Bark = Material("Bark", "4b3326", 0.72f),
            Leaf = Material("Leaf", "2f6a42", 0.82f),
            LeafDark = Material("LeafDark", "1e432f", 0.88f),
            Rock = Material("Rock", "777b71", 0.86f),
            RockDark = Material("RockDark", "3e413a", 0.9f),
            Snow = Material("Snow", "d9dfdc", 0.62f),
            GroundDark = Material("ForestGround", "233f2d", 0.86f),
            Wall = Material("WallStone", "847e6b", 0.82f),
            Roof = Material("RoofRose", "86506a", 0.76f),
            RoofWarm = Material("RoofWarm", "9a6240", 0.76f),
            Wood = Material("Wood", "5a3d29", 0.82f),
            Flag = Material("FlagRed", "bb5148", 0.55f),
            Gold = Material("GoldOre", "c09a38", 0.45f),
            Smoke = TransparentMaterial("Smoke", "c1b8aa", 0.24f)
        };
    }

    private enum ForestMode
    {
        Conifer,
        Deciduous,
        Mixed
    }

    // Pines now taper to real points (topRadius ~0.01) so no flat cap catches the light as a ring.
    private static GameObject CreatePineTree(string name, MaterialSet materials, float heightScale)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Trunk", materials.Bark, new Vector3(0f, 0.13f * heightScale, 0f), new Vector3(0.07f, 0.13f * heightScale, 0.07f), Quaternion.identity);
        AddCone(root.transform, "LowerCrown", materials.LeafDark, 0.32f, 0.012f, 0.40f * heightScale, new Vector3(0f, 0.40f * heightScale, 0f), Quaternion.Euler(-3f, 30f, 3f));
        AddCone(root.transform, "MidCrown", materials.Leaf, 0.22f, 0.01f, 0.34f * heightScale, new Vector3(0f, 0.62f * heightScale, 0f), Quaternion.Euler(2f, 10f, -2f));
        AddCone(root.transform, "TopCrown", materials.LeafDark, 0.14f, 0.008f, 0.26f * heightScale, new Vector3(0f, 0.82f * heightScale, 0f), Quaternion.Euler(-2f, -18f, 2f));
        return root;
    }

    // Broadleaf crown = faceted bipyramids (apex up + apex down), so it reads round with no flat top.
    private static GameObject CreateBroadleafTree(string name, MaterialSet materials, float heightScale)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Trunk", materials.Bark, new Vector3(0f, 0.12f, 0f), new Vector3(0.085f, 0.12f, 0.085f), Quaternion.identity);
        AddBipyramidCrown(root.transform, "Crown", materials.Leaf, 0.32f, 0.42f * heightScale, 0.30f * heightScale, new Vector3(0f, 0.42f, 0f));
        AddBipyramidCrown(root.transform, "CrownPuff", materials.LeafDark, 0.2f, 0.26f * heightScale, 0.2f * heightScale, new Vector3(0.1f, 0.56f, -0.05f));
        return root;
    }

    private static void AddBipyramidCrown(Transform parent, string name, Material material, float radius, float upperHeight, float lowerHeight, Vector3 center)
    {
        var holder = NewChild(parent, name, center, Quaternion.Euler(0f, 20f, 0f), Vector3.one);
        AddCone(holder.transform, "Upper", material, radius, 0.02f, upperHeight, new Vector3(0f, upperHeight * 0.5f, 0f), Quaternion.identity, 7);
        AddCone(holder.transform, "Lower", material, radius, 0.02f, lowerHeight, new Vector3(0f, -lowerHeight * 0.5f, 0f), Quaternion.Euler(180f, 0f, 0f), 7);
    }

    private static GameObject CreateForestCluster(string name, MaterialSet materials, int treeCount, ForestMode mode)
    {
        var root = NewRoot(name);
        // No ground hex here: the cluster prefab is placed with a random Y rotation for variety,
        // and a hex plate would visibly rotate against the grid. The forest terrain tile beneath
        // already provides the grid-aligned forest floor.
        for (var i = 0; i < treeCount; i++)
        {
            var angle = Mathf.PI * 2f * i / treeCount;
            var ring = i == 0 ? 0.05f : Mathf.Lerp(0.22f, 0.66f, (i % 5) / 4f);
            var pine = mode == ForestMode.Conifer || (mode == ForestMode.Mixed && i % 2 == 0);
            var tree = pine
                ? CreatePineTree("Tree", materials, 0.86f + (i % 5) * 0.07f)
                : CreateBroadleafTree("Tree", materials, 0.82f + (i % 4) * 0.08f);
            tree.transform.SetParent(root.transform, false);
            tree.transform.localPosition = new Vector3(Mathf.Cos(angle) * ring, 0.02f, Mathf.Sin(angle) * ring);
            tree.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
        }

        return root;
    }

    private static GameObject CreateMountainPeak(string name, MaterialSet materials, bool snowy, float scale)
    {
        var root = NewRoot(name);
        AddCone(root.transform, "Base", materials.RockDark, 0.68f * scale, 0.42f * scale, 0.22f * scale, new Vector3(0f, 0.11f * scale, 0f), Quaternion.Euler(0f, 24f, 0f), 8);
        AddCone(root.transform, "MainPeak", materials.Rock, 0.5f * scale, 0.035f * scale, 1.05f * scale, new Vector3(-0.05f * scale, 0.63f * scale, 0.02f * scale), Quaternion.Euler(0f, 18f, 0f), 7);
        AddCone(root.transform, "SidePeakA", materials.RockDark, 0.32f * scale, 0.03f * scale, 0.62f * scale, new Vector3(0.28f * scale, 0.42f * scale, 0.22f * scale), Quaternion.Euler(0f, 75f, 0f), 7);
        AddCone(root.transform, "SidePeakB", materials.Rock, 0.24f * scale, 0.025f * scale, 0.48f * scale, new Vector3(-0.32f * scale, 0.34f * scale, -0.22f * scale), Quaternion.Euler(0f, -35f, 0f), 7);
        if (snowy)
        {
            AddCone(root.transform, "SnowCap", materials.Snow, 0.18f * scale, 0.012f * scale, 0.25f * scale, new Vector3(-0.05f * scale, 1.16f * scale, 0.02f * scale), Quaternion.Euler(0f, 18f, 0f), 7);
            AddCone(root.transform, "SideSnowCap", materials.Snow, 0.11f * scale, 0.01f * scale, 0.13f * scale, new Vector3(0.28f * scale, 0.74f * scale, 0.22f * scale), Quaternion.Euler(0f, 75f, 0f), 7);
        }

        return root;
    }

    private static GameObject CreateRockyRidge(string name, MaterialSet materials, bool snowy)
    {
        var root = NewRoot(name);
        AddCone(root.transform, "RidgeBase", materials.RockDark, 0.52f, 0.34f, 0.13f, new Vector3(0f, 0.065f, 0f), Quaternion.Euler(0f, 25f, 0f), 8);
        AddCone(root.transform, "PeakA", materials.Rock, 0.28f, 0.025f, 0.52f, new Vector3(-0.18f, 0.33f, 0.02f), Quaternion.Euler(0f, -20f, 0f), 7);
        AddCone(root.transform, "PeakB", materials.RockDark, 0.24f, 0.02f, 0.42f, new Vector3(0.22f, 0.27f, 0.13f), Quaternion.Euler(0f, 55f, 0f), 7);
        AddCone(root.transform, "PeakC", materials.Rock, 0.2f, 0.018f, 0.32f, new Vector3(0.14f, 0.22f, -0.22f), Quaternion.Euler(0f, 120f, 0f), 7);
        if (snowy)
        {
            AddCone(root.transform, "SnowCapA", materials.Snow, 0.1f, 0.01f, 0.13f, new Vector3(-0.18f, 0.59f, 0.02f), Quaternion.Euler(0f, -20f, 0f), 7);
        }

        return root;
    }

    private static GameObject CreateFoothills(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        for (var i = 0; i < 5; i++)
        {
            var angle = Mathf.PI * 2f * i / 5f;
            AddRock(root.transform, $"Rock_{i}", materials, 0.22f + i * 0.02f, new Vector3(Mathf.Cos(angle) * 0.34f, 0.08f, Mathf.Sin(angle) * 0.26f), Quaternion.Euler(8f, angle * Mathf.Rad2Deg, -4f));
        }

        return root;
    }

    private static GameObject CreateRock(string name, MaterialSet materials, float size)
    {
        var root = NewRoot(name);
        AddRock(root.transform, "Rock", materials, size, Vector3.up * size * 0.22f, Quaternion.Euler(8f, 32f, -5f));
        return root;
    }

    private static GameObject CreateSettlementCluster(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Plaza", materials.Wall, Vector3.zero, new Vector3(0.48f, 0.025f, 0.42f), Quaternion.identity, 6);
        for (var i = 0; i < 5; i++)
        {
            var angle = Mathf.PI * 2f * i / 5f;
            var house = CreateHouse("House", materials, 0.78f + i * 0.07f);
            house.transform.SetParent(root.transform, false);
            house.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.32f, 0.04f, Mathf.Sin(angle) * 0.32f);
            house.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 30f, 0f);
        }

        AddFence(root.transform, "FenceA", materials, new Vector3(-0.32f, 0.08f, -0.44f), Quaternion.Euler(0f, -8f, 0f));
        AddCylinder(root.transform, "Smoke", materials.Smoke, new Vector3(0.1f, 0.48f, -0.08f), new Vector3(0.06f, 0.22f, 0.06f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateHouse(string name, MaterialSet materials, float scale)
    {
        var root = NewRoot(name);
        AddCube(root.transform, "Body", materials.Wall, new Vector3(0f, 0.11f * scale, 0f), new Vector3(0.24f * scale, 0.22f * scale, 0.28f * scale), Quaternion.identity);
        AddCone(root.transform, "Roof", scale > 0.9f ? materials.RoofWarm : materials.Roof, 0.22f * scale, 0.02f * scale, 0.2f * scale, new Vector3(0f, 0.32f * scale, 0f), Quaternion.Euler(0f, 45f, 0f), 4);
        return root;
    }

    private static GameObject CreateFence(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddFence(root.transform, "Fence", materials, Vector3.zero, Quaternion.identity);
        return root;
    }

    private static GameObject CreateWatchtower(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Base", materials.Wall, Vector3.zero, new Vector3(0.34f, 0.06f, 0.3f), Quaternion.identity, 6);
        AddCone(root.transform, "Body", materials.Wall, 0.22f, 0.18f, 1.05f, new Vector3(0f, 0.58f, 0f), Quaternion.identity, 8);
        AddCone(root.transform, "Roof", materials.Roof, 0.28f, 0.02f, 0.34f, new Vector3(0f, 1.28f, 0f), Quaternion.identity, 8);
        AddCube(root.transform, "Banner", materials.Flag, new Vector3(0.18f, 1.05f, -0.04f), new Vector3(0.28f, 0.18f, 0.035f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateMine(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCone(root.transform, "MineHill", materials.RockDark, 0.5f, 0.12f, 0.46f, new Vector3(0f, 0.23f, 0.03f), Quaternion.identity, 8);
        AddCube(root.transform, "Entrance", materials.Wood, new Vector3(0f, 0.18f, -0.28f), new Vector3(0.34f, 0.28f, 0.08f), Quaternion.identity);
        AddCube(root.transform, "RailA", materials.Wood, new Vector3(-0.07f, 0.035f, -0.48f), new Vector3(0.035f, 0.03f, 0.48f), Quaternion.identity);
        AddCube(root.transform, "RailB", materials.Wood, new Vector3(0.07f, 0.035f, -0.48f), new Vector3(0.035f, 0.03f, 0.48f), Quaternion.identity);
        AddRock(root.transform, "OreRockA", materials, 0.22f, new Vector3(-0.34f, 0.09f, 0.26f), Quaternion.Euler(8f, 22f, -4f));
        AddCube(root.transform, "OreHint", materials.Gold, new Vector3(0.32f, 0.12f, -0.18f), new Vector3(0.12f, 0.1f, 0.12f), Quaternion.Euler(15f, 30f, 8f));
        return root;
    }

    private static GameObject CreateWallSegment(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCube(root.transform, "WallBody", materials.Wall, Vector3.up * 0.12f, new Vector3(1f, 1f, 1f), Quaternion.identity);
        AddCube(root.transform, "WallCap", materials.RockDark, Vector3.up * 0.31f, new Vector3(1.18f, 0.22f, 0.78f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateWallTower(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCone(root.transform, "Tower", materials.Wall, 0.22f, 0.18f, 0.46f, new Vector3(0f, 0.23f, 0f), Quaternion.identity, 8);
        return root;
    }

    private static GameObject CreateCoastMarker(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Pole", materials.Bark, new Vector3(0.12f, 0.36f, -0.18f), new Vector3(0.035f, 0.36f, 0.035f), Quaternion.identity);
        AddCube(root.transform, "Flag", materials.Flag, new Vector3(0.28f, 0.58f, -0.18f), new Vector3(0.34f, 0.2f, 0.035f), Quaternion.identity);
        return root;
    }

    private static void AddFence(Transform parent, string name, MaterialSet materials, Vector3 localPosition, Quaternion localRotation)
    {
        var root = NewChild(parent, name, localPosition, localRotation, Vector3.one);
        for (var i = 0; i < 3; i++)
        {
            AddCube(root.transform, $"Plank_{i}", materials.Wood, new Vector3(-0.2f + i * 0.2f, 0f, 0f), new Vector3(0.16f, 0.08f, 0.035f), Quaternion.identity);
        }
    }

    private static void AddRock(Transform parent, string name, MaterialSet materials, float size, Vector3 localPosition, Quaternion localRotation)
    {
        AddCube(parent, name, materials.Rock, localPosition, new Vector3(size * 0.9f, size * 0.6f, size), localRotation);
    }

    private static void AddCube(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SetupPrimitive(cube, parent, name, localPosition, localScale, localRotation, material);
    }

    private static void AddCylinder(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, int sides = 0)
    {
        GameObject cylinder;
        if (sides > 0)
        {
            cylinder = NewChild(parent, name, localPosition, localRotation, localScale);
            var filter = cylinder.AddComponent<MeshFilter>();
            var renderer = cylinder.AddComponent<MeshRenderer>();
            filter.sharedMesh = PersistMesh(CylinderMesh(1f, 1f, 1f, sides));
            renderer.sharedMaterial = material;
            return;
        }

        cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        SetupPrimitive(cylinder, parent, name, localPosition, localScale, localRotation, material);
    }

    private static void AddCone(Transform parent, string name, Material material, float bottomRadius, float topRadius, float height, Vector3 localPosition, Quaternion localRotation, int sides = 6)
    {
        var cone = NewChild(parent, name, localPosition, localRotation, Vector3.one);
        var filter = cone.AddComponent<MeshFilter>();
        var renderer = cone.AddComponent<MeshRenderer>();
        filter.sharedMesh = PersistMesh(CylinderMesh(bottomRadius, topRadius, height, sides));
        renderer.sharedMaterial = material;
    }

    private static void DeleteObsoletePrefab(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void PrepareMeshLibrary()
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshLibraryPath) != null)
        {
            AssetDatabase.DeleteAsset(MeshLibraryPath);
        }

        s_meshLibrary = new Mesh { name = "GeneratedMeshLibrary" };
        AssetDatabase.CreateAsset(s_meshLibrary, MeshLibraryPath);
    }

    // Registers a generated mesh as a sub-asset of the shared mesh library so prefabs that
    // reference it keep a valid, persisted mesh after SaveAsPrefabAsset.
    private static Mesh PersistMesh(Mesh mesh)
    {
        if (s_meshLibrary != null && !AssetDatabase.Contains(mesh))
        {
            AssetDatabase.AddObjectToAsset(mesh, s_meshLibrary);
        }

        return mesh;
    }

    private static GameObject NewRoot(string name)
    {
        return new GameObject(name);
    }

    private static GameObject NewChild(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = localRotation;
        child.transform.localScale = localScale;
        return child;
    }

    private static void SetupPrimitive(GameObject primitive, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localRotation = localRotation;
        primitive.transform.localScale = localScale;
        primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(primitive.GetComponent<Collider>());
    }

    private static GameObject SavePrefab(string path, GameObject root)
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Material Material(string name, string hex, float smoothness)
    {
        var path = $"{MaterialRoot}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader) { name = name, color = ColorFromHex(hex) };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = ColorFromHex(hex);
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", Mathf.Clamp01(1f - smoothness));
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material TransparentMaterial(string name, string hex, float alpha)
    {
        var material = Material(name, hex, 0.35f);
        var color = material.color;
        color.a = alpha;
        material.color = color;
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        return material;
    }

    private static Mesh CylinderMesh(float bottomRadius, float topRadius, float height, int segments)
    {
        var mesh = new Mesh { name = "GeneratedCylinder" };
        var vertices = new Vector3[segments * 4 + segments * 3 + segments * 3];
        var triangles = new int[segments * 12];
        var v = 0;
        var t = 0;
        var half = height * 0.5f;

        for (var i = 0; i < segments; i++)
        {
            var a0 = Mathf.PI * 2f * i / segments;
            var a1 = Mathf.PI * 2f * (i + 1) / segments;
            var b0 = new Vector3(Mathf.Cos(a0) * bottomRadius, -half, Mathf.Sin(a0) * bottomRadius);
            var b1 = new Vector3(Mathf.Cos(a1) * bottomRadius, -half, Mathf.Sin(a1) * bottomRadius);
            var p0 = new Vector3(Mathf.Cos(a0) * topRadius, half, Mathf.Sin(a0) * topRadius);
            var p1 = new Vector3(Mathf.Cos(a1) * topRadius, half, Mathf.Sin(a1) * topRadius);

            vertices[v] = p0;
            vertices[v + 1] = p1;
            vertices[v + 2] = b0;
            vertices[v + 3] = b1;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            triangles[t++] = v + 2;
            triangles[t++] = v + 1;
            triangles[t++] = v + 3;
            v += 4;

            vertices[v] = Vector3.up * half;
            vertices[v + 1] = p1;
            vertices[v + 2] = p0;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            v += 3;

            vertices[v] = Vector3.down * half;
            vertices[v + 1] = b0;
            vertices[v + 2] = b1;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            v += 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Color ColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            return color;
        }

        return Color.magenta;
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var child = Path.GetFileName(path);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private sealed class MaterialSet
    {
        public Material Bark;
        public Material Leaf;
        public Material LeafDark;
        public Material Rock;
        public Material RockDark;
        public Material Snow;
        public Material GroundDark;
        public Material Wall;
        public Material Roof;
        public Material RoofWarm;
        public Material Wood;
        public Material Flag;
        public Material Gold;
        public Material Smoke;
    }
}
