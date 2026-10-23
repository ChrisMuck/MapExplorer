using System.Collections.Generic;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Part recipes for the style prototype. States use German display names; in the game they map to
    /// the state ids of the location state profiles (for example operational: blocked, open,
    /// repaired, destroyed). Everything here is placeholder geometry built from primitives.
    /// </summary>
    public static class MapStyleLocations
    {
        private const int Stone = 0x8d8a80;
        private const int Wood = 0x7a5a38;
        private const int Flame = 0xffa030;
        private static readonly Vector3 NoRot = Vector3.zero;

        public static readonly Color ArchetypeInvestigation = MapStyleLocation.C(0x0072B2);
        public static readonly Color ArchetypeObstacle = MapStyleLocation.C(0xE69F00);
        public static readonly Color ArchetypeContact = MapStyleLocation.C(0x009E73);
        public static readonly Color ArchetypeTrace = MapStyleLocation.C(0xC8B88A);
        public static readonly Color ArchetypeLandmark = MapStyleLocation.C(0xF2EFE6);

        private static string[] S(params string[] states) => states;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static float Deg(float radians) => radians * Mathf.Rad2Deg;

        public static MapStyleLocation Grave(Transform parent, Vector3 position, Shader shader)
        {
            var l = MapStyleLocation.Create("Grab", S("unberührt", "geöffnet", "versiegelt"), parent, position, 0f, shader, ArchetypeInvestigation, 4);
            var stone = MapStyleLocation.C(Stone);
            l.Dome(null, V(0f, 0f, -0.2f), V(3.4f, 1.87f, 3.4f), MapStyleLocation.C(0x6f9a50));
            l.Box(null, V(-0.62f, 0.55f, 1.05f), V(0.32f, 1.1f, 0.32f), stone);
            l.Box(null, V(0.62f, 0.55f, 1.05f), V(0.32f, 1.1f, 0.32f), stone);
            l.Box(null, V(0f, 1.2f, 1.05f), V(1.6f, 0.28f, 0.4f), stone);
            l.Box(S("geöffnet"), V(0f, 0.48f, 1f), V(0.9f, 0.95f, 0.2f), MapStyleLocation.C(0x050403));
            l.Box(null, V(0f, 0.5f, 1.12f), V(0.9f, 1f, 0.16f), MapStyleLocation.C(0x9a977c),
                at: MapStyleLocation.At("geöffnet", MapStyleLocation.Pose(V(1f, -0.05f, 0.55f), V(0f, 34f, 17f))));
            l.Box(S("versiegelt"), V(0f, 0.5f, 1.24f), V(1.1f, 0.1f, 0.06f), MapStyleLocation.C(0xb23a2a), V(0f, 0f, 34f));
            l.Box(S("versiegelt"), V(0f, 0.5f, 1.25f), V(1.1f, 0.1f, 0.06f), MapStyleLocation.C(0xb23a2a), V(0f, 0f, -34f));
            l.Box(null, V(-2f, 0.35f, 0.6f), V(0.35f, 0.7f, 0.1f), stone, V(0f, 17f, 6f));
            l.Box(null, V(2f, 0.25f, 0.3f), V(0.35f, 0.5f, 0.1f), stone, V(0f, -17f, -7f));
            l.Box(null, V(1.3f, 0.45f, 1.9f), V(0.1f, 0.9f, 0.1f), MapStyleLocation.C(Wood));
            l.Box(null, V(1.3f, 0.7f, 1.9f), V(0.5f, 0.1f, 0.1f), MapStyleLocation.C(Wood));
            l.Box(S("geöffnet"), V(1.7f, 0.5f, 0.9f), V(0.06f, 1f, 0.06f), MapStyleLocation.C(Wood), V(0f, 0f, -17f));
            l.Box(S("geöffnet"), V(1.55f, 0.1f, 0.9f), V(0.22f, 0.28f, 0.04f), MapStyleLocation.C(0x777770));
            l.Ico(S("geöffnet"), V(1.4f, 0.15f, 0.5f), 0.45f, MapStyleLocation.C(0x6b4a2f), V(1.4f, 0.6f, 1f));
            l.Light(V(0f, 0.6f, 1.5f), S("geöffnet"), MapStyleLocation.C(0x7aff9a), 0.9f, 5f);
            return l;
        }

        public static MapStyleLocation Cave(Transform parent, Vector3 position, Shader shader)
        {
            var l = MapStyleLocation.Create("Höhle", S("offen", "besetzt", "blockiert", "eingestürzt"), parent, position, 0f, shader, ArchetypeInvestigation, 4);
            l.Ico(null, V(0f, 0.9f, -0.5f), 1.9f, MapStyleLocation.C(0x8a877c), V(1.1f, 0.85f, 1f));
            l.Ico(null, V(-1.7f, 0.6f, 0.3f), 1.3f, MapStyleLocation.C(0x7d7a70), V(1f, 0.8f, 1f));
            l.Ico(null, V(1.7f, 0.55f, 0.2f), 1.2f, MapStyleLocation.C(0x948f84), V(1f, 0.8f, 1f));
            l.Ico(null, V(-0.85f, 0.6f, 1.1f), 0.6f, MapStyleLocation.C(0x8a877c), V(0.7f, 1.1f, 0.7f));
            l.Ico(null, V(0.85f, 0.6f, 1.1f), 0.6f, MapStyleLocation.C(0x8a877c), V(0.7f, 1.1f, 0.7f));
            l.Ico(null, V(0f, 1.2f, 1.1f), 0.55f, MapStyleLocation.C(0x948f84), V(1.6f, 0.5f, 0.7f));
            l.Box(S("offen", "besetzt"), V(0f, 0.48f, 1f), V(1.2f, 0.95f, 0.4f), MapStyleLocation.C(0x0d0b09));
            var rubble = new[] { V(-0.6f, 0.2f, 1.7f), V(0.5f, 0.25f, 1.6f), V(0f, 0.2f, 1.9f), V(-0.2f, 0.5f, 1.5f), V(0.7f, 0.15f, 2f), V(-0.9f, 0.15f, 1.9f) };
            for (var i = 0; i < rubble.Length; i++)
            {
                l.Ico(S("blockiert", "eingestürzt"), rubble[i], 0.28f + (i % 3) * 0.06f, MapStyleLocation.C(0x8f8a7e), V(1f, 0.8f, 1f));
            }

            l.Ico(S("eingestürzt"), V(0f, 0.7f, 1.2f), 1f, MapStyleLocation.C(0x8a877c), V(1.5f, 1f, 0.9f));
            l.Cone(S("besetzt"), V(0.5f, 0.25f, 1.9f), 0.18f, 0.45f, 5, MapStyleLocation.C(Flame), emissive: true);
            l.Box(S("besetzt"), V(0.5f, 0.05f, 1.9f), V(0.5f, 0.08f, 0.08f), MapStyleLocation.C(Wood), V(0f, 29f, 0f));
            l.Light(V(0.5f, 0.9f, 1.9f), S("besetzt"), MapStyleLocation.C(Flame), 1.3f, 6f);
            l.Smoke(V(0.5f, 0.6f, 1.9f), S("besetzt"), MapStyleLocation.C(0xb8b2a4), 1f);
            l.Smoke(V(0f, 1.8f, 0.8f), S("eingestürzt"), MapStyleLocation.C(0xa8a294), 1.2f);
            return l;
        }

        private static void House(MapStyleLocation l, float x, float z, float yawDeg, string[] roofShows, Dictionary<string, Pz> sag)
        {
            l.Box(null, V(x, 0.35f, z), V(1f, 0.7f, 0.8f), MapStyleLocation.C(0xd4c4a0), V(0f, yawDeg, 0f));
            l.Cone(roofShows, V(x, 0.97f, z), 0.85f, 0.55f, 4, MapStyleLocation.C(0xb5683c), V(0f, yawDeg + 45f, 0f), at: sag);
            var rad = yawDeg * Mathf.Deg2Rad;
            l.Box(S("besetzt"), V(x + Mathf.Sin(rad) * 0.41f, 0.4f, z + Mathf.Cos(rad) * 0.41f), V(0.22f, 0.3f, 0.05f), MapStyleLocation.C(0xffd27a), V(0f, yawDeg, 0f), emissive: true);
        }

        private static void Person(MapStyleLocation l, string[] shows, float x, float z, int coat)
        {
            l.Cylinder(shows, V(x, 0.23f, z), 0.1f, 0.14f, 0.45f, 6, MapStyleLocation.C(coat));
            l.Ico(shows, V(x, 0.55f, z), 0.11f, MapStyleLocation.C(0xe0b890));
        }

        public static MapStyleLocation Village(Transform parent, Vector3 position, Shader shader)
        {
            var l = MapStyleLocation.Create("Dorf", S("besetzt", "verlassen", "umkämpft"), parent, position, 0f, shader, ArchetypeContact, 5);
            House(l, -1.2f, -0.6f, 23f, null, MapStyleLocation.At("verlassen", MapStyleLocation.Pose(V(0f, -0.12f, 0f), V(4.6f, 0f, 12.6f))));
            House(l, 1.1f, -0.9f, -17f, S("besetzt", "verlassen"), MapStyleLocation.At("verlassen", MapStyleLocation.Pose(V(0f, -0.1f, 0f), V(-5.7f, 0f, 10.3f))));
            House(l, 0.2f, 1f, 189f, null, MapStyleLocation.At("verlassen", MapStyleLocation.Pose(V(0f, -0.12f, 0f), V(5.7f, 0f, -11.5f)), "umkämpft", MapStyleLocation.Pose(V(0f, -0.05f, 0f), V(5.7f, 0f, 5.7f))));
            l.Box(S("umkämpft"), V(1.1f, 0.55f, -0.9f), V(1.02f, 0.5f, 0.82f), MapStyleLocation.C(0x2a2622));

            const float radius = 3f;
            const int posts = 16;
            for (var i = 0; i < posts; i++)
            {
                var removedAbandoned = i == 3 || i == 4 || i == 10;
                var removedContested = i == 3 || i == 4 || i == 5 || i == 10 || i == 11 || i == 12;
                var shows = new List<string> { "besetzt" };
                if (!removedAbandoned) shows.Add("verlassen");
                if (!removedContested) shows.Add("umkämpft");
                var showArray = shows.ToArray();
                var angle = i / (float)posts * Mathf.PI * 2f;
                var tall = i == 0 || i == 1;
                var height = tall ? 1.1f : 0.75f;
                var lean = i == 6 || i == 13 ? MapStyleLocation.At("verlassen", MapStyleLocation.Pose(rot: V(0f, 0f, 23f))) : null;
                l.Box(showArray, V(radius * Mathf.Cos(angle), height * 0.5f, radius * Mathf.Sin(angle)), V(0.14f, height, 0.14f), MapStyleLocation.C(Wood), at: lean);
                if (i != 0)
                {
                    var mid = (i - 0.5f) / posts * Mathf.PI * 2f;
                    var r = radius * Mathf.Cos(Mathf.PI / posts);
                    var yaw = Deg(Mathf.Atan2(-Mathf.Cos(mid), -Mathf.Sin(mid)));
                    l.Box(showArray, V(r * Mathf.Cos(mid), 0.5f, r * Mathf.Sin(mid)), V(0.6f, 0.08f, 0.05f), MapStyleLocation.C(0x8a6a42), V(0f, yaw, 0f));
                }
            }

            var bushes = new[] { V(-1.9f, 0.2f, 0.2f), V(1.9f, 0.2f, 0.3f), V(0f, 0.2f, -1.9f), V(1.2f, 0.2f, 2f), V(-1f, 0.2f, 1.6f), V(2.4f, 0.2f, -1.4f) };
            for (var i = 0; i < bushes.Length; i++)
            {
                l.Ico(S("verlassen"), bushes[i], 0.34f + (i % 2) * 0.1f, MapStyleLocation.C(0x4f7d3c), V(1f, 0.7f, 1f));
            }

            l.Box(S("umkämpft"), V(0f, 0.9f, 0.1f), V(0.06f, 1.8f, 0.06f), MapStyleLocation.C(Wood));
            l.Box(S("umkämpft"), V(0.33f, 1.5f, 0.1f), V(0.6f, 0.35f, 0.04f), MapStyleLocation.C(0xb23a2a));
            Person(l, S("besetzt"), -0.4f, 0.2f, 0x3a6ea5);
            Person(l, S("besetzt"), 0.5f, 0.3f, 0x3a6ea5);
            foreach (var index in new[] { 1, 9 })
            {
                var angle = index / (float)posts * Mathf.PI * 2f;
                var x = radius * Mathf.Cos(angle);
                var z = radius * Mathf.Sin(angle);
                l.Cone(S("umkämpft"), V(x, 0.97f, z), 0.14f, 0.4f, 5, MapStyleLocation.C(Flame), emissive: true);
                l.Light(V(x, 1.4f, z), S("umkämpft"), MapStyleLocation.C(0xff5a2a), 0.9f, 5f);
            }

            l.Cone(S("umkämpft"), V(1.1f, 1.2f, -0.9f), 0.22f, 0.6f, 5, MapStyleLocation.C(0xff6a20), emissive: true);
            l.Light(V(1.1f, 1.6f, -0.9f), S("umkämpft"), MapStyleLocation.C(0xff5a2a), 1.2f, 6f);
            l.Light(V(0f, 1.4f, 0f), S("besetzt"), MapStyleLocation.C(0xffc070), 0.8f, 7f);
            l.Smoke(V(-1.1f, 1.3f, -0.6f), S("besetzt"), MapStyleLocation.C(0xb8b2a4), 0.9f);
            l.Smoke(V(1.1f, 1.5f, -0.9f), S("umkämpft"), MapStyleLocation.C(0x3a3632), 1.4f);
            return l;
        }

        public static MapStyleLocation Tower(Transform parent, Vector3 position, Shader shader)
        {
            var l = MapStyleLocation.Create("Wachturm", S("besetzt", "verlassen", "zerstört"), parent, position, 0f, shader, ArchetypeLandmark, 0);
            var standing = S("besetzt", "verlassen");
            l.Cylinder(standing, V(0f, 1.3f, 0f), 0.85f, 1.1f, 2.6f, 6, MapStyleLocation.C(0x9a977c));
            l.Cylinder(standing, V(0f, 2.75f, 0f), 1.3f, 1f, 0.3f, 6, MapStyleLocation.C(Wood),
                at: MapStyleLocation.At("verlassen", MapStyleLocation.Pose(rot: V(2.9f, 0f, 4f))));
            l.Cone(standing, V(0f, 3.55f, 0f), 1.5f, 1f, 6, MapStyleLocation.C(0xb5683c),
                at: MapStyleLocation.At("verlassen", MapStyleLocation.Pose(V(0f, -0.1f, 0f), V(3.4f, 0f, 8f))));
            l.Box(standing, V(0f, 0.35f, 1f), V(0.4f, 0.7f, 0.12f), MapStyleLocation.C(0x2a1f14));
            l.Box(S("besetzt"), V(0f, 4.5f, 0f), V(0.05f, 1f, 0.05f), MapStyleLocation.C(Wood));
            l.Box(S("besetzt"), V(0.28f, 4.75f, 0f), V(0.5f, 0.3f, 0.04f), MapStyleLocation.C(0x3a6ea5));
            l.Cone(S("besetzt"), V(1.1f, 3.15f, 0f), 0.15f, 0.4f, 5, MapStyleLocation.C(Flame), emissive: true);
            l.Light(V(1.1f, 3.6f, 0f), S("besetzt"), MapStyleLocation.C(Flame), 1.2f, 7f);
            l.Smoke(V(1.1f, 3.5f, 0f), S("besetzt"), MapStyleLocation.C(0xb8b2a4), 0.8f);
            l.Ico(S("verlassen"), V(0.9f, 0.3f, 0.6f), 0.4f, MapStyleLocation.C(0x4f7d3c), V(1f, 0.8f, 1f));
            l.Ico(S("verlassen"), V(-0.8f, 0.45f, -0.7f), 0.45f, MapStyleLocation.C(0x4f7d3c));
            l.Cylinder(S("zerstört"), V(0f, 0.35f, 0f), 1f, 1.2f, 0.7f, 6, MapStyleLocation.C(Stone));
            var rubble = new[] { V(1.3f, 0.2f, 0.6f), V(-1.2f, 0.25f, 0.9f), V(0.3f, 0.2f, -1.5f), V(-0.4f, 0.3f, 1.6f), V(1.7f, 0.15f, -0.9f), V(-1.6f, 0.2f, -0.6f), V(0.2f, 0.75f, 0.1f) };
            for (var i = 0; i < rubble.Length; i++)
            {
                l.Ico(S("zerstört"), rubble[i], 0.3f + (i % 3) * 0.08f, MapStyleLocation.C(Stone), V(1f, 0.8f, 1f));
            }

            l.Smoke(V(0f, 0.9f, 0f), S("zerstört"), MapStyleLocation.C(0xa8a294), 1.2f);
            return l;
        }

        public static MapStyleLocation Camp(Transform parent, Vector3 position, Shader shader)
        {
            var l = MapStyleLocation.Create("Lager", S("besetzt", "verlassen"), parent, position, 0f, shader, ArchetypeTrace, 6);
            l.Cone(null, V(-1.1f, 0.55f, -0.3f), 0.9f, 1.1f, 4, MapStyleLocation.C(0xc9b27a), V(0f, 46f, 0f),
                at: MapStyleLocation.At("verlassen", MapStyleLocation.Pose(V(0f, -0.27f, 0f), V(0f, 0f, 14f), V(1f, 0.5f, 1f))));
            l.Cone(null, V(1f, 0.55f, -0.9f), 0.9f, 1.1f, 4, MapStyleLocation.C(0xb5683c), V(0f, 17f, 0f),
                at: MapStyleLocation.At("verlassen", MapStyleLocation.Pose(V(0f, -0.25f, 0f), V(11f, 0f, -11f), V(1f, 0.45f, 1f))));
            for (var i = 0; i < 5; i++)
            {
                var angle = i / 5f * Mathf.PI * 2f;
                l.Box(null, V(0.2f + Mathf.Cos(angle) * 0.45f, 0.07f, 1f + Mathf.Sin(angle) * 0.45f), V(0.5f, 0.1f, 0.1f), MapStyleLocation.C(Wood), V(0f, Deg(-angle + 1.57f), 0f));
            }

            l.Cone(S("besetzt"), V(0.2f, 0.3f, 1f), 0.2f, 0.55f, 5, MapStyleLocation.C(Flame), emissive: true);
            l.Cylinder(S("verlassen"), V(0.2f, 0.04f, 1f), 0.4f, 0.4f, 0.04f, 7, MapStyleLocation.C(0x2a2622));
            l.Box(S("besetzt"), V(-1.6f, 0.2f, 1.3f), V(0.5f, 0.4f, 0.5f), MapStyleLocation.C(Wood));
            l.Box(S("verlassen"), V(-1.5f, 0.15f, 1.6f), V(0.5f, 0.4f, 0.5f), MapStyleLocation.C(Wood), V(29f, 23f, 34f));
            l.Box(null, V(1.9f, 0.75f, 0.7f), V(0.06f, 1.5f, 0.06f), MapStyleLocation.C(Wood));
            l.Box(S("besetzt"), V(2.15f, 1.3f, 0.7f), V(0.5f, 0.3f, 0.04f), MapStyleLocation.C(0xb23a2a));
            l.Box(S("verlassen"), V(2.1f, 1f, 0.7f), V(0.4f, 0.22f, 0.04f), MapStyleLocation.C(Stone), V(0f, 0f, 29f));
            Person(l, S("besetzt"), -0.3f, 0.2f, 0x3a6ea5);
            Person(l, S("besetzt"), 0.9f, 0.5f, 0x3a6ea5);
            l.Light(V(0.2f, 0.9f, 1f), S("besetzt"), MapStyleLocation.C(Flame), 1.2f, 6f);
            l.Smoke(V(0.2f, 0.7f, 1f), S("besetzt"), MapStyleLocation.C(0xb8b2a4), 1f);
            return l;
        }

        /// <summary>
        /// Bridge over a ditch. With banks (showcase pedestal) it brings its own ditch; without banks
        /// (landscape) it sits in the terrain ditch, with the deck at local height 0.
        /// </summary>
        public static MapStyleLocation Bridge(Transform parent, Vector3 position, Shader shader, bool withBanks)
        {
            var l = MapStyleLocation.Create("Brücke", S("intakt", "beschädigt", "repariert", "zerstört"), parent, position, 0f, shader, ArchetypeObstacle, 3);
            var shift = withBanks ? 0f : -0.6f;
            float Y(float y) => y + shift;
            var wood = MapStyleLocation.C(Wood);
            if (withBanks)
            {
                foreach (var z in new[] { -1.95f, 1.95f })
                {
                    l.Box(null, V(0f, 0.275f, z), V(4.2f, 0.55f, 1.7f), MapStyleLocation.C(0x8a6b4a));
                    l.Box(null, V(0f, 0.58f, z), V(4.2f, 0.06f, 1.7f), MapStyleLocation.C(0x7fa85a));
                }

                l.Box(null, V(0f, 0.06f, 0f), V(4.2f, 0.12f, 2.2f), MapStyleLocation.C(0x2f5d7a));
                l.Box(null, V(0f, 0.62f, -2.4f), V(1.4f, 0.02f, 1.3f), MapStyleLocation.C(0xc2a878));
                l.Box(null, V(0f, 0.62f, 2.4f), V(1.4f, 0.02f, 1.3f), MapStyleLocation.C(0xc2a878));
            }

            for (var i = 0; i < 12; i++)
            {
                var z = -2.2f + i * 0.4f;
                var middle = i >= 4 && i <= 7;
                var shows = middle ? S("intakt") : S("intakt", "beschädigt");
                Dictionary<string, Pz> at = null;
                if (i == 3) at = MapStyleLocation.At("beschädigt", MapStyleLocation.Pose(V(0f, -0.1f, 0.05f), V(29f, 0f, 0f)));
                if (i == 8) at = MapStyleLocation.At("beschädigt", MapStyleLocation.Pose(V(0f, -0.1f, -0.05f), V(-29f, 0f, 0f)));
                l.Box(shows, V(0f, Y(0.6f), z), V(1.5f, 0.1f, 0.36f), MapStyleLocation.C(i % 2 == 1 ? 0x9a7a4a : 0xa88556), at: at);
                l.Box(S("repariert"), V(0f, Y(0.6f), z), V(1.5f, 0.1f, 0.36f), MapStyleLocation.C(i % 2 == 1 ? 0xd2a96a : 0xdcb478));
            }

            foreach (var x in new[] { -0.62f, 0.62f })
            {
                l.Box(S("intakt", "repariert"), V(x, Y(0.46f), 0f), V(0.14f, 0.16f, 4.6f), wood);
                l.Box(S("beschädigt"), V(x, Y(0.46f), -1.5f), V(0.14f, 0.16f, 1.7f), wood);
                l.Box(S("beschädigt"), V(x, Y(0.46f), 1.5f), V(0.14f, 0.16f, 1.7f), wood);
                l.Box(S("intakt", "repariert"), V(x * 1.3f, Y(1.15f), 0f), V(0.07f, 0.07f, 4.4f), MapStyleLocation.C(0x6b5230));
                foreach (var z in new[] { -2f, 2f })
                {
                    l.Box(null, V(x * 1.3f, Y(0.9f), z), V(0.14f, 0.7f, 0.14f), wood,
                        at: MapStyleLocation.At("zerstört", MapStyleLocation.Pose(V(0f, -0.28f, 0f), V(0f, 0f, 7f), V(1f, 0.4f, 1f))));
                }
            }

            var debrisY = withBanks ? 0.1f : -1.1f;
            l.Box(S("beschädigt", "zerstört"), V(0.45f, debrisY, -0.5f), V(0.16f, 0.2f, 1.6f), wood, V(52f, 0f, 6f));
            l.Box(S("beschädigt", "zerstört"), V(-0.5f, debrisY + 0.05f, 0.4f), V(0.4f, 0.08f, 1.1f), MapStyleLocation.C(0x9a7a4a), V(6f, 29f, 6f));
            l.Box(S("repariert"), V(-1.2f, Y(0.9f), -1.7f), V(0.1f, 1.4f, 0.1f), wood, V(0f, 0f, 23f));
            l.Box(S("repariert"), V(-1.2f, Y(0.9f), -1.7f), V(0.1f, 1.4f, 0.1f), wood, V(0f, 0f, -23f));
            l.Box(S("repariert"), V(-1f, Y(1.4f), -1.7f), V(1.2f, 0.08f, 0.08f), wood);
            l.Box(S("beschädigt", "zerstört"), V(1.7f, Y(1.05f), -1.7f), V(0.06f, 1f, 0.06f), wood);
            l.Box(S("beschädigt", "zerstört"), V(1.95f, Y(1.35f), -1.7f), V(0.45f, 0.3f, 0.04f), MapStyleLocation.C(0xb23a2a));
            return l;
        }

        /// <summary>Expedition shown as one group, as decided: the composition is shown in the UI.</summary>
        public static MapStyleLocation ExpeditionGroup(Transform parent, Vector3 position, Shader shader, float yaw)
        {
            var l = MapStyleLocation.Create("Expedition", S("unterwegs"), parent, position, yaw, shader, ArchetypeTrace, -1);
            var offsets = new[] { V(0f, 0f, 0f), V(0.5f, 0f, 0.45f), V(-0.5f, 0f, 0.55f), V(0.1f, 0f, 1.1f), V(-0.4f, 0f, -0.6f) };
            for (var i = 0; i < offsets.Length; i++)
            {
                var o = offsets[i];
                var coat = i == 0 ? 0xd24a2a : 0x3a6ea5;
                l.Cylinder(null, o + V(0f, 0.3f, 0f), 0.12f, 0.17f, 0.55f, 6, MapStyleLocation.C(coat));
                l.Ico(null, o + V(0f, 0.7f, 0f), 0.14f, MapStyleLocation.C(0xe0b890));
                l.Box(null, o + V(0f, 0.35f, -0.15f), V(0.22f, 0.3f, 0.14f), MapStyleLocation.C(0xc9863a));
            }

            l.Box(null, V(0.22f, 0.5f, 0.05f), V(0.1f, 0.14f, 0.1f), MapStyleLocation.C(0xffd27a), emissive: true);
            l.Light(V(0.2f, 0.7f, 0.1f), null, MapStyleLocation.C(0xffd080), 0.8f, 5f);
            return l;
        }

        /// <summary>Merges many small low-poly items into one scenery mesh that obeys knowledge like a location.</summary>
        public static MapStyleLocation Forest(Transform parent, Shader shader, string name, IList<Vector3> positions, IList<float> sizes)
        {
            var l = MapStyleLocation.Create(name, S("steht"), parent, Vector3.zero, 0f, shader, Color.clear, -1);
            l.Part("trees", null, Vector3.zero, Vector3.zero, b =>
            {
                for (var i = 0; i < positions.Count; i++)
                {
                    var p = positions[i];
                    var s = sizes[i];
                    var green = Color.Lerp(MapStyleLocation.C(0x4f7d3c), MapStyleLocation.C(0x5a8a42), TerrainHash(i));
                    b.AddPrism(p + V(0f, 0.25f * s, 0f), V(0.2f * s, 0.5f * s, 0.2f * s), NoRot, 5, 0.75f, MapStyleLocation.C(Wood));
                    b.AddPrism(p + V(0f, 0.9f * s, 0f), V(1.1f * s, 1.1f * s, 1.1f * s), NoRot, 6, 0f, green);
                    b.AddPrism(p + V(0f, 1.5f * s, 0f), V(0.76f * s, 0.9f * s, 0.76f * s), NoRot, 6, 0f, Color.Lerp(green, Color.white, 0.06f));
                }
            });
            return l;
        }

        public static MapStyleLocation Route(Transform parent, Shader shader, IList<Vector3> points)
        {
            var l = MapStyleLocation.Create("Route", S("geplant"), parent, Vector3.zero, 0f, shader, Color.clear, -1);
            l.Part("dots", null, Vector3.zero, Vector3.zero, b =>
            {
                foreach (var p in points)
                {
                    b.AddIco(p, Vector3.one * 0.2f, NoRot, MapStyleLocation.C(0x3d8bd9));
                }
            }, emissive: true);
            return l;
        }

        private static float TerrainHash(int i) => MapStyleTerrain.Hash(i, 11f);
    }
}
