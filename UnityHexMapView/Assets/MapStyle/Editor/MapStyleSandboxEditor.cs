using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MapExplorer.MapStyle.EditorTools
{
    /// <summary>
    /// Creates the V0 sandbox scene and can render reference screenshots in batch mode so the look can be
    /// reviewed without opening the editor. Usage (batch, with graphics):
    /// Unity -batchmode -projectPath UnityHexMapView -executeMethod MapExplorer.MapStyle.EditorTools.MapStyleSandboxEditor.CaptureBatch -captureDir DIR -quit
    /// </summary>
    public static class MapStyleSandboxEditor
    {
        private const string ScenePath = "Assets/MapStyle/Scenes/MapStyleSandbox.unity";
        private const string WorldScenePath = "Assets/MapStyle/Scenes/MapStyleWorld.unity";
        private const string ShaderPath = "Assets/MapStyle/Shaders/MapStyleLit.shader";

        [MenuItem("Map Style/Create Sandbox Scene")]
        public static void CreateScene()
        {
            CreateSceneInternal();
        }

        [MenuItem("Map Style/Create World Scene (real world data)")]
        public static void CreateWorldScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("MapStyleWorldView");
            var view = go.AddComponent<MapStyleWorldView>();
            view.shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!AssetDatabase.IsValidFolder("Assets/MapStyle/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/MapStyle", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, WorldScenePath);
        }

        private static MapStyleSandbox CreateSceneInternal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("MapStyleSandbox");
            var sandbox = go.AddComponent<MapStyleSandbox>();
            sandbox.shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!AssetDatabase.IsValidFolder("Assets/MapStyle/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/MapStyle", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            return sandbox;
        }

        public static void CaptureBatch()
        {
            var directory = ReadArgument("-captureDir") ?? Path.Combine(Path.GetTempPath(), "mapstyle-captures");
            Directory.CreateDirectory(directory);
            try
            {
                AssetDatabase.Refresh();
                var sandbox = CreateSceneInternal();
                if (sandbox.shader == null)
                {
                    throw new InvalidOperationException("Shader not found at " + ShaderPath);
                }

                RunCaptures(sandbox, directory);
                Debug.Log("MapStyle captures written to " + directory);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("MapStyle capture failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RunCaptures(MapStyleSandbox sandbox, string directory)
        {
            sandbox.Build(MapStyleSandbox.ViewMode.Landscape);
            sandbox.SetTimeOfDay(42f);
            sandbox.SetCameraView(new Vector3(-4f, 0f, 1f), 13f, 40f);
            Shot(sandbox, directory, "01-landscape-player-knowledge");

            sandbox.SetCameraView(new Vector3(-4f, 0f, 1f), 13f, 150f);
            Shot(sandbox, directory, "02-landscape-rotated");

            sandbox.SetShowTruth(true);
            sandbox.SetCameraView(new Vector3(-4f, 0f, 1f), 13f, 40f);
            Shot(sandbox, directory, "03-landscape-world-truth");
            sandbox.SetShowTruth(false);

            foreach (var state in new[] { "beschädigt", "repariert", "zerstört" })
            {
                sandbox.SetBridgeState(state, false);
                sandbox.SetCameraView(new Vector3(-6f, 0f, 1f), 5.5f, 20f);
                Shot(sandbox, directory, "04-bridge-" + state);
            }

            sandbox.SetBridgeState("beschädigt", false);
            sandbox.SetTimeOfDay(92f);
            sandbox.SetCameraView(new Vector3(-6f, 0f, 3f), 8f, 40f);
            Shot(sandbox, directory, "05-landscape-night");
            sandbox.SetTimeOfDay(42f);

            sandbox.Build(MapStyleSandbox.ViewMode.Showcase);
            sandbox.SetTimeOfDay(42f);
            sandbox.SetCameraView(Vector3.zero, 15.5f, 40f);
            Shot(sandbox, directory, "10-showcase-first-state");

            for (var round = 1; round <= 2; round++)
            {
                foreach (var location in sandbox.Locations)
                {
                    if (round < location.States.Length)
                    {
                        location.SetState(location.States[round], false);
                    }
                }

                Shot(sandbox, directory, "11-showcase-state-index-" + round);
            }

            foreach (var location in sandbox.Locations)
            {
                location.SetState(location.States[0], false);
                location.SetKnowledge(1);
            }

            Shot(sandbox, directory, "12-showcase-reported");

            foreach (var location in sandbox.Locations)
            {
                location.SetKnowledge(2);
            }

            Shot(sandbox, directory, "13-showcase-unknown");
        }

        private static void Shot(MapStyleSandbox sandbox, string directory, string name)
        {
            const int width = 1600;
            const int height = 900;
            sandbox.SnapAll();
            var camera = sandbox.Cam;
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = texture;
            camera.aspect = width / (float)height;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
