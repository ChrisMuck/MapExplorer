using System.Collections.Generic;
using System.Globalization;
using Game.App;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Turns the visual part definitions (JSON) into low-poly meshes and caches them. Parts are placeholder
    /// geometry made of primitives; a better asset can replace a part later without touching compositions.
    /// </summary>
    public sealed class MapStyleVisualKit
    {
        private readonly Dictionary<string, Mesh> normalMeshes = new();
        private readonly Dictionary<string, Mesh> emissiveMeshes = new();

        public MapStyleVisualKit(VisualDefinitionSet definitions)
        {
            Definitions = definitions;
        }

        public VisualDefinitionSet Definitions { get; }

        /// <summary>Mesh for the normal or the emissive primitives of a part; null when the part has none of that kind.</summary>
        public Mesh GetMesh(string partId, bool emissive)
        {
            var cache = emissive ? emissiveMeshes : normalMeshes;
            if (cache.TryGetValue(partId, out var cached))
            {
                return cached;
            }

            Mesh mesh = null;
            var part = Definitions.FindPart(partId);
            if (part != null)
            {
                var builder = new MapStyleMeshBuilder();
                foreach (var primitive in part.Primitives)
                {
                    if (primitive.Emissive != emissive)
                    {
                        continue;
                    }

                    AddPrimitive(builder, primitive);
                }

                if (builder.TriangleCount > 0)
                {
                    mesh = builder.Build(partId + (emissive ? "-emissive" : string.Empty));
                }
            }

            cache[partId] = mesh;
            return mesh;
        }

        public static Color ParseColor(string hex)
        {
            var value = int.Parse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return MapStyleLocation.C(value);
        }

        public static Vector3 ToVector(VisualVec3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static void AddPrimitive(MapStyleMeshBuilder builder, VisualPrimitiveDefinition primitive)
        {
            var position = ToVector(primitive.Position);
            var size = ToVector(primitive.Size);
            var euler = ToVector(primitive.Euler);
            var color = ParseColor(primitive.ColorHex);
            switch (primitive.Shape)
            {
                case VisualShape.Box:
                    builder.AddBox(position, size, euler, color);
                    break;
                case VisualShape.Prism:
                    builder.AddPrism(position, size, euler, primitive.Sides, primitive.TopScale, color);
                    break;
                case VisualShape.Ico:
                    builder.AddIco(position, size, euler, color);
                    break;
                case VisualShape.Dome:
                    builder.AddDome(position, size, euler, 9, 4, color);
                    break;
            }
        }
    }
}
