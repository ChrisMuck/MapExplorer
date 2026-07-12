using System;
using System.Collections.Generic;

namespace WorldGen
{
    /// <summary>
    /// The full procedural world-generation pipeline (design stages 1–11), ported
    /// from the web prototype. Pure logic: no meshes, no rendering, no UnityEngine.
    /// Call <see cref="Generate"/> with a <see cref="GenerationParams"/> and read
    /// the resulting <see cref="GeneratedWorld"/>. Deterministic per seed + salts.
    /// </summary>
    public sealed class WorldGenerator
    {
        private static readonly string[] FCOL =
            { "#e6605a", "#5aa9e6", "#e6c05a", "#a05ae6", "#5ae6a0", "#e68a5a", "#5a6ae6", "#e65ab0" };

        private GenerationParams _p;
        private HexGrid _g;
        private Rng _rng;
        private List<Faction> _factions;
        private HexCell _base, _mysteryCore;
        private List<List<HexCell>> _roads;
        private HashSet<string> _roadEdges;
        private List<River> _rivers;
        private List<SpecialLocation> _specials;
        private float _elevMax, _elevMin;
        private float _minSpacing;
        private int _landCells;
        private WorldStats _stats;

        public GeneratedWorld Generate(GenerationParams p)
        {
            _p = p;
            _g = new HexGrid(p.MapWidth, p.MapHeight);

            // --- Terrain phase (depends only on the master seed) ---
            _rng = new Rng(Hashing.SubSeed(p.Seed, "terrain"));
            CreateLand();
            ThermalErosion();
            EnforceSingleLandmass();
            ComputeElevExtent();
            MarkOcean();
            CreateClimate();
            CreateRivers();
            RiverCarve();
            AssignBiomes();
            SmoothBiomes(p.BiomeSmooth);
            DeriveHexData();

            // --- Faction phase (+ factionSalt) ---
            _rng = new Rng(Hashing.SubSeed(p.Seed, p.FactionSalt, "factions"));
            _factions = BuildFactions(p.FactionCount);
            _rng = new Rng(Hashing.SubSeed(p.Seed, p.FactionSalt, "anchors"));
            PlaceAnchors();
            _rng = new Rng(Hashing.SubSeed(p.Seed, p.FactionSalt, "world"));
            GrowTerritory();
            PlaceSettlements();
            PlaceMysteryCore();
            BuildRoads();
            AssignNames();

            // --- Location phase (+ locationSalt) ---
            _rng = new Rng(Hashing.SubSeed(p.Seed, p.FactionSalt, p.LocationSalt, "locations"));
            PlaceSpecials();

            ComputeStats();

            return new GeneratedWorld
            {
                Params = _p, Grid = _g, Factions = _factions, Base = _base, MysteryCore = _mysteryCore,
                Roads = _roads, RoadEdges = _roadEdges, Rivers = _rivers, Specials = _specials,
                ElevMax = _elevMax, ElevMin = _elevMin, MinSpacing = _minSpacing, Stats = _stats
            };
        }

        private static float[] FillInf(int n) { var a = new float[n]; for (int i = 0; i < n; i++) a[i] = float.PositiveInfinity; return a; }

        // ===================== Stage 1–2: Landmass, Elevation, Erosion =====================

        private void CreateLand()
        {
            var p = _p; var g = _g;
            var noise = new Noise(p.Seed);
            double sc = Math.Max(1, p.NoiseScale); int oct = p.NoiseOctaves;
            var H = new float[g.N];
            double maxH = double.NegativeInfinity, minH = double.PositiveInfinity;
            foreach (var c in g.Cells)
            {
                double nx = c.Col / sc, nz = c.Row / sc;
                double wx = noise.Fbm(nx + 5.2, nz + 1.3, oct), wy = noise.Fbm(nx + 9.1, nz + 4.7, oct);
                double h = noise.Fbm(nx + p.WarpStrength * (wx - 0.5), nz + p.WarpStrength * (wy - 0.5), oct);
                double dx = ((double)c.Col / (g.W - 1) - 0.5) * 2, dz = ((double)c.Row / (g.H - 1) - 0.5) * 2;
                double r = Math.Min(1, Math.Sqrt(dx * dx + dz * dz) / Math.Sqrt(2));
                h = h * (1 - p.IslandFalloff) + h * (1 - r) * p.IslandFalloff;
                double rg = noise.Ridged(nx * 0.7 + 2.5, nz * 0.7 + 7.5, oct);
                h += p.RidgeStrength * rg * MathUtil.Smoothstep(0.45, 0.75, h);
                double eb = Math.Min(1, Math.Min(
                    (double)Math.Min(c.Col, g.W - 1 - c.Col) / Math.Max(1, p.MapBorderX),
                    (double)Math.Min(c.Row, g.H - 1 - c.Row) / Math.Max(1, p.MapBorderZ)));
                h *= eb;
                H[c.I] = (float)h; if (h > maxH) maxH = h; if (h < minH) minH = h;
            }
            int targetLand = Math.Max(1, (int)Math.Round(g.N * p.LandPercentage / 100.0));
            var sorted = (float[])H.Clone(); Array.Sort(sorted);
            double SeaFor(double frac) => sorted[Math.Min(g.N - 1, Math.Max(0, (int)Math.Floor(frac * g.N)))];
            void Apply(double sea)
            {
                double span = Math.Max(1e-4, maxH - sea), dspan = Math.Max(1e-4, sea - minH);
                foreach (var c in g.Cells)
                {
                    double h = H[c.I];
                    c.Elevation = (float)(h >= sea ? (h - sea) / span * p.ElevationMaximum : (h - sea) / dspan * 3);
                    c.WaterLevel = 0; c.IsLake = false; c.IsOcean = false; c.RiverIn = -1; c.RiverOut = -1;
                }
            }
            double seaLvl = SeaFor(1 - p.LandPercentage / 100.0);
            Apply(seaLvl);
            for (int guard = 0; guard < 14 && LargestLandSize() < targetLand * 0.9; guard++)
            { seaLvl -= (maxH - minH) * 0.03; Apply(seaLvl); }
        }

        private int LargestLandSize()
        {
            var g = _g; var comp = new bool[g.N]; int best = 0;
            foreach (var c in g.Cells)
            {
                if (c.IsUnder || comp[c.I]) continue;
                var stack = new Stack<HexCell>(); stack.Push(c); comp[c.I] = true; int sz = 0;
                while (stack.Count > 0)
                {
                    var cur = stack.Pop(); sz++;
                    for (int d = 0; d < 6; d++) { var n = g.Step(cur, d); if (n != null && !n.IsUnder && !comp[n.I]) { comp[n.I] = true; stack.Push(n); } }
                }
                if (sz > best) best = sz;
            }
            return best;
        }

        private void ThermalErosion()
        {
            var g = _g; var p = _p;
            int iters = (int)Math.Round(p.ErosionPercentage / 100.0 * 8);
            double talus = Math.Max(0.001, p.ElevationMaximum * 0.06);
            for (int it = 0; it < iters; it++)
            {
                var delta = new float[g.N];
                foreach (var c in g.Cells)
                {
                    if (c.IsUnder) continue;
                    HexCell low = null; double ld = 0;
                    for (int d = 0; d < 6; d++) { var nb = g.Step(c, d); if (nb == null || nb.IsUnder) continue; double diff = c.Elevation - nb.Elevation; if (diff > ld) { ld = diff; low = nb; } }
                    if (low != null && ld > talus) { double m = (ld - talus) * 0.5; delta[c.I] -= (float)m; delta[low.I] += (float)m; }
                }
                foreach (var c in g.Cells) { if (c.IsUnder) continue; c.Elevation = (float)Math.Max(0.02, c.Elevation + delta[c.I]); }
            }
        }

        private void EnforceSingleLandmass()
        {
            var g = _g; var comp = new int[g.N]; for (int i = 0; i < g.N; i++) comp[i] = -1;
            int best = -1, bestSize = 0, cid = 0;
            foreach (var c in g.Cells)
            {
                if (c.IsUnder || comp[c.I] >= 0) continue;
                var stack = new Stack<HexCell>(); stack.Push(c); comp[c.I] = cid; int sz = 0;
                while (stack.Count > 0)
                {
                    var cur = stack.Pop(); sz++;
                    for (int d = 0; d < 6; d++) { var n = g.Step(cur, d); if (n != null && !n.IsUnder && comp[n.I] < 0) { comp[n.I] = cid; stack.Push(n); } }
                }
                if (sz > bestSize) { bestSize = sz; best = cid; }
                cid++;
            }
            foreach (var c in g.Cells) if (!c.IsUnder && comp[c.I] != best) c.Elevation = c.WaterLevel - 1;
            _landCells = bestSize;
        }

        private void ComputeElevExtent()
        {
            double mx = 1e-4, mn = 0;
            foreach (var c in _g.Cells) { if (c.Elevation > mx) mx = c.Elevation; if (c.Elevation < mn) mn = c.Elevation; }
            _elevMax = (float)mx; _elevMin = (float)mn;
        }

        private void MarkOcean()
        {
            var g = _g; var seen = new bool[g.N]; var q = new Queue<HexCell>();
            void TryAdd(HexCell c) { if (c != null && c.IsUnder && !seen[c.I]) { seen[c.I] = true; c.IsOcean = true; q.Enqueue(c); } }
            for (int x = 0; x < g.W; x++) { TryAdd(g.Get(x, 0)); TryAdd(g.Get(x, g.H - 1)); }
            for (int z = 0; z < g.H; z++) { TryAdd(g.Get(0, z)); TryAdd(g.Get(g.W - 1, z)); }
            while (q.Count > 0) { var c = q.Dequeue(); for (int d = 0; d < 6; d++) TryAdd(g.Step(c, d)); }
            foreach (var c in g.Cells) if (c.IsUnder && !c.IsOcean) c.IsLake = true;
        }

        // ===================== Stage 3: Climate =====================

        private void CreateClimate()
        {
            var g = _g; var p = _p;
            foreach (var c in g.Cells) { c.Moisture = p.StartingMoisture; c.Clouds = 0; }
            var nm = new float[g.N]; var nc = new float[g.N];
            int dispDir = HexGrid.Opp(p.WindDirection);
            for (int cycle = 0; cycle < 40; cycle++)
            {
                Array.Clear(nm, 0, nm.Length); Array.Clear(nc, 0, nc.Length);
                foreach (var c in g.Cells)
                {
                    double clouds = c.Clouds, moisture = c.Moisture;
                    if (c.IsUnder) { moisture = 1; clouds += p.EvaporationFactor; }
                    else { double ev = moisture * p.EvaporationFactor; moisture -= ev; clouds += ev; }
                    double precip = clouds * p.PrecipitationFactor; clouds -= precip; moisture += precip;
                    double cloudMax = 1 - c.ViewElev / (_elevMax + 1);
                    if (clouds > cloudMax) { moisture += clouds - cloudMax; clouds = cloudMax; }
                    double cloudDisp = clouds * (1.0 / (5 + p.WindStrength));
                    double runoff = moisture * p.RunoffFactor * (1.0 / 6);
                    double seepage = moisture * p.SeepageFactor * (1.0 / 6);
                    for (int d = 0; d < 6; d++)
                    {
                        var nb = g.Step(c, d); if (nb == null) continue;
                        nc[nb.I] += (float)((d == dispDir) ? cloudDisp * p.WindStrength : cloudDisp);
                        double delta = nb.ViewElev - c.ViewElev;
                        if (delta < 0) { moisture -= runoff; nm[nb.I] += (float)runoff; }
                        else if (delta == 0) { moisture -= seepage; nm[nb.I] += (float)seepage; }
                    }
                    nm[c.I] += (float)moisture; if (nm[c.I] > 1) nm[c.I] = 1;
                }
                foreach (var c in g.Cells) { c.Clouds = nc[c.I]; c.Moisture = nm[c.I]; }
            }
        }

        // ===================== Stage 4: Rivers & Lakes =====================

        private void CreateRivers()
        {
            var g = _g; var p = _p;
            var origins = new List<HexCell>();
            foreach (var c in g.Cells)
            {
                if (c.IsUnder) continue;
                double w = c.Moisture * c.Elevation / _elevMax;
                if (w > 0.75) { origins.Add(c); origins.Add(c); }
                if (w > 0.5) origins.Add(c);
                if (w > 0.25) origins.Add(c);
            }
            int budget = (int)Math.Round(_landCells * p.RiverPercentage / 100.0);
            while (budget > 0 && origins.Count > 0)
            {
                int idx = _rng.Range(0, origins.Count);
                var origin = origins[idx]; origins[idx] = origins[origins.Count - 1]; origins.RemoveAt(origins.Count - 1);
                if (origin.HasRiver) continue;
                bool valid = true;
                for (int d = 0; d < 6; d++) { var n = g.Step(origin, d); if (n != null && (n.HasRiver || n.IsUnder)) { valid = false; break; } }
                if (valid) budget -= TraceRiver(origin);
            }
        }

        private int TraceRiver(HexCell origin)
        {
            var g = _g; var p = _p;
            int length = 1; var c = origin; int direction = 0; var flow = new List<int>();
            while (!c.IsUnder)
            {
                double minNbElev = double.PositiveInfinity; flow.Clear();
                for (int d = 0; d < 6; d++)
                {
                    var nb = g.Step(c, d); if (nb == null) continue;
                    if (nb.Elevation < minNbElev) minNbElev = nb.Elevation;
                    if (nb == origin || nb.RiverIn >= 0) continue;
                    double delta = nb.Elevation - c.Elevation;
                    if (delta > 0) continue;
                    if (nb.RiverOut >= 0) { c.RiverOut = d; nb.RiverIn = HexGrid.Opp(d); return length; }
                    if (delta < 0) { flow.Add(d); flow.Add(d); flow.Add(d); }
                    if (length == 1 || (d != (direction + 2) % 6 && d != (direction + 4) % 6)) flow.Add(d);
                    flow.Add(d);
                }
                if (flow.Count == 0)
                {
                    if (length == 1) return 0;
                    if (minNbElev >= c.Elevation)
                    {
                        c.WaterLevel = (float)minNbElev;
                        if (minNbElev == c.Elevation) c.Elevation = (float)(minNbElev - 1);
                        c.IsLake = true;
                    }
                    break;
                }
                direction = _rng.Pick(flow);
                c.RiverOut = direction;
                var outCell = g.Step(c, direction); outCell.RiverIn = HexGrid.Opp(direction);
                length++;
                if (minNbElev >= c.Elevation && _rng.NextDouble() < p.ExtraLakeProbability)
                { c.WaterLevel = c.Elevation; c.Elevation = c.Elevation - 1; c.IsLake = true; }
                c = outCell;
            }
            return length;
        }

        private void RiverCarve()
        {
            var g = _g; var p = _p;
            double amt = p.ErosionPercentage / 100.0 * _elevMax * 0.12;
            if (amt <= 0) return;
            foreach (var c in g.Cells) { if (c.IsUnder || !c.HasRiver) continue; c.Elevation = (float)Math.Max(0.02, c.Elevation - amt); }
        }

        // ===================== Stage 5: Biomes =====================

        private void AssignBiomes()
        {
            var g = _g; var p = _p;
            var elevs = new List<float>();
            foreach (var c in g.Cells) if (!c.IsUnder) elevs.Add(c.Elevation);
            elevs.Sort();
            double ma = MathUtil.Clamp01(p.MountainAmount);
            double mtnFrac = 0.02 + 0.16 * ma, highFrac = 0.04 + 0.14 * ma;
            double Q(double f) => elevs.Count > 0 ? elevs[Math.Min(elevs.Count - 1, Math.Max(0, (int)Math.Floor(f * elevs.Count)))] : 0;
            double mtnThr = Q(1 - mtnFrac), highThr = Q(1 - mtnFrac - highFrac);
            var tb = p.TemperatureBands; var mb = p.MoistureBands;
            int[] table =
            {
                (int)Biome.Tundra,(int)Biome.Tundra,(int)Biome.Forest,(int)Biome.DeepForest,
                (int)Biome.Plains,(int)Biome.Plains,(int)Biome.Forest,(int)Biome.Swamp,
                (int)Biome.Desert,(int)Biome.Plains,(int)Biome.Forest,(int)Biome.Swamp,
                (int)Biome.Desert,(int)Biome.Desert,(int)Biome.Jungle,(int)Biome.Jungle
            };
            foreach (var c in g.Cells)
            {
                if (c.IsUnder) { c.Biome = c.IsLake ? (int)Biome.Lake : (int)Biome.Ocean; continue; }
                c.Temperature = (float)Temperature(c);
                int t = 0; while (t < tb.Length && c.Temperature >= tb[t]) t++;
                int m = 0; while (m < mb.Length && c.Moisture >= mb[m]) m++;
                int b = table[t * 4 + m];
                if (c.Elevation >= mtnThr) b = (int)Biome.Mountains;
                else if (c.Elevation >= highThr) b = (int)Biome.Highlands;
                else
                {
                    bool coastal = false;
                    for (int d = 0; d < 6; d++) { var n = g.Step(c, d); if (n != null && n.IsUnder) { coastal = true; break; } }
                    if (coastal) b = (int)Biome.Coast;
                    else if (c.HasRiver && (b == (int)Biome.Desert || b == (int)Biome.Tundra)) b = (int)Biome.Riverlands;
                }
                c.Biome = b;
            }
        }

        private double Temperature(HexCell c)
        {
            var p = _p; var g = _g;
            double lat = (double)c.Row / g.H;
            if (p.Hemisphere == "Both") { lat *= 2; if (lat > 1) lat = 2 - lat; }
            else if (p.Hemisphere == "North") lat = 1 - lat;
            double temp = p.LowTemperature + (p.HighTemperature - p.LowTemperature) * lat;
            temp *= 1 - c.ViewElev / (_elevMax + 1);
            double j = Hashing.CellNoise(c.Col, c.Row, p.Seed);
            temp += (j * 2 - 1) * p.TemperatureJitter;
            return temp;
        }

        private void SmoothBiomes(int passes)
        {
            var g = _g;
            bool Keep(int b) => b == (int)Biome.Mountains || b == (int)Biome.Coast || b == (int)Biome.Ocean || b == (int)Biome.Lake;
            for (int it = 0; it < passes; it++)
            {
                var next = new int[g.N]; for (int i = 0; i < g.N; i++) next[i] = -1; int changed = 0;
                foreach (var c in g.Cells)
                {
                    if (c.IsUnder || Keep(c.Biome)) continue;
                    var counts = new Dictionary<int, int>(); int same = 0, total = 0, bestB = -1, bestN = 0;
                    for (int d = 0; d < 6; d++)
                    {
                        var n = g.Step(c, d); if (n == null || n.IsUnder) continue; total++;
                        int nb = n.Biome; counts.TryGetValue(nb, out int cc); counts[nb] = cc + 1;
                        if (nb == c.Biome) same++;
                        if (!Keep(nb) && counts[nb] > bestN) { bestN = counts[nb]; bestB = nb; }
                    }
                    if (total >= 4 && same == 0 && bestB >= 0) { next[c.I] = bestB; changed++; }
                }
                foreach (var c in g.Cells) if (next[c.I] >= 0) c.Biome = next[c.I];
                if (changed == 0) break;
            }
        }

        private void DeriveHexData()
        {
            foreach (var c in _g.Cells)
            {
                var bd = Biomes.Def(c.Biome);
                c.MoveCost = bd.Move;
                c.Visibility = bd.Vis + (float)Math.Max(0, c.Elevation - c.WaterLevel) * 0.05f;
                c.Hazard = bd.Hazard;
            }
        }

        // ===================== Stage 5B: Faction archetypes =====================

        private List<Faction> BuildFactions(int N)
        {
            var g = _g;
            var counts = new Dictionary<int, int>();
            foreach (var c in g.Cells) if (!c.IsUnder) { counts.TryGetValue(c.Biome, out int cc); counts[c.Biome] = cc + 1; }
            var landBiomes = new List<int>();
            foreach (var kv in counts) if (kv.Key != (int)Biome.Mountains && kv.Key != (int)Biome.Ocean && kv.Key != (int)Biome.Lake) landBiomes.Add(kv.Key);
            int WeightedBiome()
            {
                if (landBiomes.Count == 0) return (int)Biome.Plains;
                int tot = 0; foreach (var b in landBiomes) tot += counts[b];
                double rr = _rng.NextDouble() * tot;
                foreach (var b in landBiomes) { rr -= counts[b]; if (rr <= 0) return b; }
                return landBiomes[0];
            }

            string mood = _p.FactionMood ?? "Gemischt";
            double wf = 0.2, hf = 0.2; int wMin = 1, hMin = 1;
            if (mood == "Friedlich") { wf = 0.5; hf = 0.1; wMin = 1; hMin = 0; }
            else if (mood == "Feindselig") { wf = 0.1; hf = 0.5; wMin = 0; hMin = 1; }
            int wc = Math.Max(wMin, (int)Math.Round(N * wf)), hc = Math.Max(hMin, (int)Math.Round(N * hf));
            while (wc + hc > N) { if (hc > wc) hc--; else wc--; }
            var attitudes = new List<string>();
            for (int i = 0; i < N; i++) attitudes.Add(i < wc ? "welcoming" : (i < wc + hc ? "hostile" : "neutral-cautious"));
            for (int i = attitudes.Count - 1; i > 0; i--) { int jj = _rng.Range(0, i + 1); (attitudes[i], attitudes[jj]) = (attitudes[jj], attitudes[i]); }

            var axStrict = new[] { "open land", "warned borders", "absolute borders" };
            var axAggr = new[] { "avoids conflict", "warns then escalates", "strikes without warning" };
            var axComm = new[] { "direct speech", "signs & symbols only", "intermediaries only", "avoids contact entirely" };
            var axValues = new[] { "survival", "tradition", "secrecy", "purity", "order/hierarchy", "freedom", "knowledge", "faith" };
            var axTaboo = new[] { "disturbing graves", "entering sacred sites", "carrying weapons", "specific numbers/colors", "mapmaking", "forbidden words" };
            var axLead = new[] { "single leader", "council", "decentralized", "hidden leader" };
            var axOrigin = new[] { "human", "non-human", "altered-human", "unclear-unknown" };
            var tabooBiomes = new[] { (int)Biome.DeadZones, (int)Biome.Wasteland, (int)Biome.Swamp, (int)Biome.Volcanic };

            var outList = new List<Faction>();
            for (int i = 0; i < N; i++)
            {
                Faction f = null;
                for (int tries = 0; tries < 40; tries++)
                {
                    f = new Faction
                    {
                        Id = i, Attitude = attitudes[i],
                        Strictness = _rng.Pick(axStrict), Aggression = _rng.Pick(axAggr), Communication = _rng.Pick(axComm),
                        Leadership = _rng.Pick(axLead), Origin = _rng.Pick(axOrigin),
                        PreferredBiome = WeightedBiome(),
                        TabooBiome = _rng.NextDouble() < 0.5 ? _rng.Pick(tabooBiomes) : -1,
                        Color = FCOL[i % FCOL.Length]
                    };
                    f.Values.Add(_rng.Pick(axValues));
                    f.Taboo.Add(_rng.Pick(axTaboo));
                    if (_rng.NextDouble() < 0.5) f.Values.Add(_rng.Pick(axValues));

                    if (f.Attitude == "welcoming" && f.Aggression == "strikes without warning") continue;
                    if (f.Values.Contains("purity") && (f.PreferredBiome == (int)Biome.DeadZones || f.PreferredBiome == (int)Biome.Wasteland)) continue;
                    if (f.Origin == "unclear-unknown" && f.Communication == "direct speech") continue;

                    bool clash = false;
                    foreach (var o in outList)
                    {
                        int same = 0;
                        if (o.Strictness == f.Strictness) same++;
                        if (o.Aggression == f.Aggression) same++;
                        if (o.Communication == f.Communication) same++;
                        if (o.Leadership == f.Leadership) same++;
                        if (o.PreferredBiome == f.PreferredBiome) same++;
                        if (same >= 3) { clash = true; break; }
                    }
                    if (clash) continue;
                    break;
                }
                outList.Add(f);
            }
            return outList;
        }

        private int UsableLand()
        {
            int n = 0; foreach (var c in _g.Cells) if (!c.IsUnder && c.Biome != (int)Biome.Mountains) n++; return n;
        }

        // ===================== Stage 6: Anchor placement =====================

        private void PlaceAnchors()
        {
            var g = _g;
            int BorderDist(HexCell c) => Math.Min(Math.Min(c.Col, g.W - 1 - c.Col), Math.Min(c.Row, g.H - 1 - c.Row));

            HexCell best = null; double bestScore = double.NegativeInfinity;
            foreach (var c in g.Cells)
            {
                if (c.IsUnder) continue;
                bool coast = false, fresh = false; int rough = 0;
                for (int d = 0; d < 6; d++)
                {
                    var n = g.Step(c, d); if (n == null) continue;
                    if (n.IsOcean) coast = true;
                    if (n.IsLake || n.HasRiver) fresh = true;
                    if (n.Elevation - c.Elevation >= 2) rough++;
                }
                if (!coast) continue;
                double score = 10 - rough * 2 + (fresh ? 4 : 0) - c.Hazard * 3 + Math.Min(BorderDist(c), 6) * 0.8 + _rng.NextDouble() * 0.5;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            _base = best ?? FirstLand();

            int N = _factions.Count;
            int usable = UsableLand();
            _minSpacing = (float)(0.9 * Math.Sqrt((double)usable / Math.Max(1, N)));
            var landList = new List<HexCell>(); foreach (var c in g.Cells) if (!c.IsUnder && c.Biome != (int)Biome.Mountains) landList.Add(c);
            var placed = new List<HexCell> { _base };
            foreach (var f in _factions)
            {
                HexCell bestC = null; double bs = double.NegativeInfinity;
                int tries = Math.Min(landList.Count, 400);
                for (int k = 0; k < tries; k++)
                {
                    var cand = _rng.Pick(landList);
                    if (cand.Biome == f.TabooBiome) continue;
                    double minD = double.PositiveInfinity;
                    foreach (var qq in placed) { int dd = g.Dist(cand, qq); if (dd < minD) minD = dd; }
                    int bd = BorderDist(cand);
                    double sscore = minD + (cand.Biome == f.PreferredBiome ? 3 : 0) - cand.Hazard * 2 + Math.Min(bd, 5) * 0.8 - (bd < 2 ? 8 : 0) + _rng.NextDouble() * 0.5;
                    if (sscore > bs) { bs = sscore; bestC = cand; }
                }
                f.Seed = bestC ?? _rng.Pick(landList);
                placed.Add(f.Seed);
            }
        }

        private HexCell FirstLand()
        {
            foreach (var c in _g.Cells) if (!c.IsUnder) return c; return _g.Cells[0];
        }

        private float[] PathCostField(HexCell src)
        {
            var g = _g; var dist = FillInf(g.N); var fr = new Heap<HexCell>();
            dist[src.I] = 0; fr.Push(src, 0);
            while (fr.Size > 0)
            {
                var c = fr.Pop();
                for (int d = 0; d < 6; d++)
                {
                    var nb = g.Step(c, d); if (nb == null || nb.IsUnder) continue;
                    double nc = dist[c.I] + Biomes.Def(nb.Biome).Move + Math.Abs(nb.Elevation - c.Elevation) * 0.5;
                    if (nc < dist[nb.I]) { dist[nb.I] = (float)nc; fr.Push(nb, nc); }
                }
            }
            return dist;
        }

        // ===================== Stage 7: Territory (cost-Voronoi, independent fields) =====================

        private void GrowTerritory()
        {
            var g = _g; int N = _factions.Count; if (N == 0) return;
            double Enter(HexCell from, HexCell to, Faction f)
            {
                if (to.IsUnder) return double.PositiveInfinity;
                double cost = Biomes.Def(to.Biome).Move;
                if (to.Biome == (int)Biome.Mountains) cost += 6;
                if (from != null)
                {
                    bool crossing = (from.RiverOut >= 0 && g.Step(from, from.RiverOut) == to) || (to.RiverOut >= 0 && g.Step(to, to.RiverOut) == from);
                    if (crossing) cost += 6;
                    cost += Math.Abs(to.Elevation - from.Elevation) * 0.6;
                }
                if (to.Biome == f.PreferredBiome) cost *= 0.5;
                if (to.Biome == f.TabooBiome) cost *= 3;
                return cost;
            }

            var fields = new float[N][];
            for (int fi = 0; fi < N; fi++)
            {
                var f = _factions[fi]; var bestF = FillInf(g.N); fields[fi] = bestF;
                if (f.Seed == null) continue;
                var fr = new Heap<HexCell>(); bestF[f.Seed.I] = 0; fr.Push(f.Seed, 0);
                while (fr.Size > 0)
                {
                    var c = fr.Pop();
                    for (int d = 0; d < 6; d++)
                    {
                        var nb = g.Step(c, d); if (nb == null) continue;
                        double nc = bestF[c.I] + Enter(c, nb, f);
                        if (nc < bestF[nb.I]) { bestF[nb.I] = (float)nc; fr.Push(nb, nc); }
                    }
                }
            }

            var owner = new int[g.N]; for (int i = 0; i < g.N; i++) owner[i] = -1;
            var bestCost = FillInf(g.N); var second = FillInf(g.N);
            for (int i = 0; i < g.N; i++)
            {
                double b1 = double.PositiveInfinity, b2 = double.PositiveInfinity; int of = -1;
                for (int f = 0; f < N; f++) { double v = fields[f][i]; if (v < b1) { b2 = b1; b1 = v; of = f; } else if (v < b2) { b2 = v; } }
                owner[i] = of; bestCost[i] = (float)b1; second[i] = (float)b2;
            }
            var maxExtent = new double[N];
            for (int i = 0; i < g.N; i++) { int o = owner[i]; if (o >= 0 && !float.IsInfinity(bestCost[i]) && !g.Cells[i].IsUnder && bestCost[i] > maxExtent[o]) maxExtent[o] = bestCost[i]; }
            double frac = _p.TerritoryReach;
            foreach (var c in g.Cells)
            {
                int i = c.I; int o = owner[i];
                if (o >= 0 && !c.IsUnder && !float.IsInfinity(bestCost[i]) && bestCost[i] <= frac * maxExtent[o] + 1e-6)
                {
                    c.Faction = o; c.Influence = (float)(1.0 / (1 + bestCost[i] * 0.05));
                    c.Contested = !float.IsInfinity(second[i]) && (second[i] - bestCost[i]) < 4;
                }
                else { c.Faction = -1; c.Influence = 0; c.Contested = false; }
            }
        }

        private void PlaceSettlements()
        {
            var g = _g; var p = _p;
            foreach (var f in _factions)
            {
                f.Towns = new List<HexCell>();
                if (f.Seed == null) continue;
                f.Seed.Settlement = f.Id; f.Seed.IsCapital = true;
                var terr = new List<HexCell>();
                foreach (var c in g.Cells) if (c.Faction == f.Id && !c.IsUnder && c.Biome != (int)Biome.Mountains && c != f.Seed) terr.Add(c);
                int nTowns = Math.Min(p.MaxTowns, terr.Count / Math.Max(1, p.CellsPerTown));
                var placed = new List<HexCell> { f.Seed };
                for (int t = 0; t < nTowns; t++)
                {
                    HexCell bestC = null; double bestS = double.NegativeInfinity;
                    foreach (var c in terr)
                    {
                        if (c.Settlement >= 0) continue;
                        double minD = double.PositiveInfinity; foreach (var qq in placed) { int dd = g.Dist(c, qq); if (dd < minD) minD = dd; }
                        if (minD < p.TownSpacing) continue;
                        double s = minD - c.Hazard * 2 + _rng.NextDouble() * 0.4;
                        if (s > bestS) { bestS = s; bestC = c; }
                    }
                    if (bestC == null) break;
                    bestC.Settlement = f.Id; f.Towns.Add(bestC); placed.Add(bestC);
                }
            }
        }

        private void PlaceMysteryCore()
        {
            var g = _g;
            var cost = PathCostField(_base);
            double maxCost = 0; foreach (var c in g.Cells) if (!float.IsInfinity(cost[c.I]) && cost[c.I] > maxCost) maxCost = cost[c.I];
            HexCell bestCore = null; double bestScore = double.NegativeInfinity;
            foreach (var c in g.Cells)
            {
                if (c.IsUnder || float.IsInfinity(cost[c.I])) continue;
                double far = cost[c.I] / maxCost; if (far < 0.6) continue;
                int neutral = (c.Faction < 0 || c.Contested) ? 1 : 0;
                double score = far + neutral * 0.5 + _rng.NextDouble() * 0.1;
                if (score > bestScore) { bestScore = score; bestCore = c; }
            }
            _mysteryCore = bestCore ?? _base;
        }

        // ===================== Stage 8: Roads =====================

        private void BuildRoads()
        {
            var g = _g;
            _roads = new List<List<HexCell>>(); _roadEdges = new HashSet<string>();
            var fs = new List<Faction>(); foreach (var f in _factions) if (f.Seed != null) fs.Add(f);
            for (int i = 0; i < fs.Count; i++)
                for (int j = i + 1; j < fs.Count; j++)
                {
                    if (fs[i].Attitude == "hostile" || fs[j].Attitude == "hostile") continue;
                    if (g.Dist(fs[i].Seed, fs[j].Seed) > _minSpacing * 2.2) continue;
                    var path = AStar(fs[i].Seed, fs[j].Seed);
                    if (path != null) { _roads.Add(path); for (int k = 1; k < path.Count; k++) _roadEdges.Add(HexGrid.EdgeKey(path[k - 1], path[k])); }
                }
            foreach (var f in _factions)
            {
                var nodes = new List<HexCell>(); if (f.Seed != null) nodes.Add(f.Seed); foreach (var t in f.Towns) nodes.Add(t);
                if (nodes.Count < 2) continue;
                var inTree = new List<HexCell> { nodes[0] };
                var rest = new List<HexCell>(); for (int i = 1; i < nodes.Count; i++) rest.Add(nodes[i]);
                while (rest.Count > 0)
                {
                    int bi = 0, bj = 0; double bd = double.PositiveInfinity;
                    for (int i = 0; i < inTree.Count; i++) for (int j = 0; j < rest.Count; j++) { int dd = g.Dist(inTree[i], rest[j]); if (dd < bd) { bd = dd; bi = i; bj = j; } }
                    var path = AStar(inTree[bi], rest[bj]);
                    if (path != null) { _roads.Add(path); for (int k = 1; k < path.Count; k++) _roadEdges.Add(HexGrid.EdgeKey(path[k - 1], path[k])); }
                    inTree.Add(rest[bj]); rest.RemoveAt(bj);
                }
            }
        }

        private List<HexCell> AStar(HexCell a, HexCell b)
        {
            var g = _g; var gS = FillInf(g.N); var came = new HexCell[g.N];
            var fr = new Heap<HexCell>(); gS[a.I] = 0; fr.Push(a, g.Dist(a, b));
            while (fr.Size > 0)
            {
                var c = fr.Pop();
                if (c == b)
                {
                    var path = new List<HexCell>(); var cur = b; while (cur != null) { path.Add(cur); cur = came[cur.I]; } path.Reverse(); return path;
                }
                for (int d = 0; d < 6; d++)
                {
                    var nb = g.Step(c, d); if (nb == null || nb.IsUnder) continue;
                    double step = _p.RoadDirectness + Biomes.Def(nb.Biome).Move
                        + (nb.Biome == (int)Biome.Coast ? 2 : 0)
                        + (nb == _base ? 1000 : 0)
                        + Math.Abs(nb.Elevation - c.Elevation) * 0.5;
                    double ng = gS[c.I] + step;
                    if (ng < gS[nb.I]) { gS[nb.I] = (float)ng; came[nb.I] = c; fr.Push(nb, ng + g.Dist(nb, b)); }
                }
            }
            return null;
        }

        // ===================== Naming =====================

        private void AssignNames()
        {
            var g = _g;
            var byId = NamingData.ById;
            string Gen(SyllableGen gg) => NamingData.Generate(gg, _rng);

            var used = new HashSet<string>();
            foreach (var f in _factions)
            {
                string id = null;
                foreach (var v in f.Values) { if (NamingData.StyleByValue.TryGetValue(v, out var c) && byId.ContainsKey(c) && !used.Contains(c)) { id = c; break; } }
                if (id == null && NamingData.StyleByBiome.TryGetValue(f.PreferredBiome, out var bb) && byId.ContainsKey(bb) && !used.Contains(bb)) id = bb;
                if (id == null) { foreach (var s in NamingData.Styles) if (!used.Contains(s.Id)) { id = s.Id; break; } }
                used.Add(id); f.Style = byId[id]; f.StyleId = id; f.Name = _rng.Pick(f.Style.FactionNames);
            }

            foreach (var f in _factions)
            {
                if (f.Style == null) continue;
                var seen = new HashSet<string>();
                string Uniq(SyllableGen gg) { string n = Gen(gg); for (int t = 0; t < 12 && seen.Contains(n); t++) n = Gen(gg); seen.Add(n); return n; }
                if (f.Seed != null) f.Seed.PlaceName = Uniq(f.Style.Settlements);
                foreach (var t in f.Towns) t.PlaceName = Uniq(f.Style.Settlements);
            }

            var rid = new int[g.N]; for (int i = 0; i < g.N; i++) rid[i] = -1;
            _rivers = new List<River>();
            var defStyle = (_factions.Count > 0 && _factions[0].Style != null) ? _factions[0].Style : NamingData.Styles[0];
            foreach (var c in g.Cells)
            {
                if (!c.HasRiver || rid[c.I] >= 0) continue;
                var comp = new List<HexCell>(); var stack = new Stack<HexCell>(); int rIndex = _rivers.Count; rid[c.I] = rIndex; stack.Push(c);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop(); comp.Add(cur);
                    int[] dirs = { cur.RiverOut, cur.RiverIn };
                    foreach (var dd in dirs) { if (dd >= 0) { var n = g.Step(cur, dd); if (n != null && n.HasRiver && rid[n.I] < 0) { rid[n.I] = rIndex; stack.Push(n); } } }
                }
                var cnt = new Dictionary<int, int>(); foreach (var cc in comp) if (cc.Faction >= 0) { cnt.TryGetValue(cc.Faction, out int x); cnt[cc.Faction] = x + 1; }
                int bf = -1, bn = 0; for (int k = 0; k < _factions.Count; k++) { if (cnt.TryGetValue(k, out int x) && x > bn) { bn = x; bf = k; } }
                var style = (bf >= 0 && _factions[bf].Style != null) ? _factions[bf].Style : defStyle;
                string name = Gen(style.Rivers);
                HexCell mouth = comp[0];
                foreach (var cc in comp) { bool atSea = false; for (int d = 0; d < 6; d++) { var n = g.Step(cc, d); if (n != null && n.IsOcean) { atSea = true; break; } } if (atSea) { mouth = cc; break; } }
                foreach (var cc in comp) cc.RiverName = name;
                _rivers.Add(new River { Name = name, Cells = comp, Mouth = mouth, Faction = bf });
            }
        }

        private List<HexCell> StampAreaCells(HexCell seed, int biome, int size)
        {
            var g = _g; var outCells = new List<HexCell> { seed }; var seen = new HashSet<int> { seed.I }; var frontier = new List<HexCell> { seed };
            seed.Biome = biome;
            while (outCells.Count < size && frontier.Count > 0)
            {
                int idx = _rng.Range(0, frontier.Count); var c = frontier[idx];
                var nbs = new List<HexCell>();
                for (int d = 0; d < 6; d++) { var n = g.Step(c, d); if (n != null && !n.IsUnder && !seen.Contains(n.I) && n.Special == null && n.Settlement < 0 && n != _base && n != _mysteryCore) nbs.Add(n); }
                if (nbs.Count == 0) { frontier.RemoveAt(idx); continue; }
                var nn = _rng.Pick(nbs); seen.Add(nn.I); nn.Biome = biome; outCells.Add(nn); frontier.Add(nn);
            }
            return outCells;
        }

        // ===================== Stage 9: Special locations =====================

        private void PlaceSpecials()
        {
            var g = _g; var p = _p; _specials = new List<SpecialLocation>();
            var land = new List<HexCell>(); foreach (var c in g.Cells) if (!c.IsUnder) land.Add(c);
            if (land.Count == 0) { DeriveHexData(); return; }

            var cost = _base != null ? PathCostField(_base) : new float[g.N];
            double maxCost = 1e-4; foreach (var c in land) if (!float.IsInfinity(cost[c.I]) && cost[c.I] > maxCost) maxCost = cost[c.I];
            var settlements = new List<HexCell>(); foreach (var f in _factions) { if (f.Seed != null) settlements.Add(f.Seed); foreach (var t in f.Towns) settlements.Add(t); }
            double DistSettle(HexCell c) { double m = double.PositiveInfinity; foreach (var s in settlements) { int dd = g.Dist(c, s); if (dd < m) m = dd; } return double.IsInfinity(m) ? 0 : m; }
            double Remote(HexCell c) => (!float.IsInfinity(cost[c.I]) ? cost[c.I] / maxCost : 0) * 0.6 + Math.Min(1, DistSettle(c) / 16) * 0.4;
            bool NearWater(HexCell c) { if (c.HasRiver) return true; for (int d = 0; d < 6; d++) { var n = g.Step(c, d); if (n != null && (n.IsLake || n.HasRiver)) return true; } return false; }
            bool Coastal(HexCell c) { if (c.Biome == (int)Biome.Coast) return true; for (int d = 0; d < 6; d++) { var n = g.Step(c, d); if (n != null && n.IsOcean) return true; } return false; }
            bool IsBorderOf(HexCell c, int fid) { for (int d = 0; d < 6; d++) { var n = g.Step(c, d); if (n != null && !n.IsUnder && n.Faction != fid) return true; } return false; }

            var used = new HashSet<int>(); var placedCells = new List<HexCell>();
            foreach (var f in _factions) { if (f.Seed != null) { used.Add(f.Seed.I); placedCells.Add(f.Seed); } foreach (var t in f.Towns) { used.Add(t.I); placedCells.Add(t); } }
            if (_base != null) { used.Add(_base.I); placedCells.Add(_base); }
            if (_mysteryCore != null) { used.Add(_mysteryCore.I); placedCells.Add(_mysteryCore); }

            double Spread(HexCell c) { double m = double.PositiveInfinity; foreach (var q in placedCells) { int dd = g.Dist(c, q); if (dd < m) m = dd; } return m; }

            int nLand = land.Count; double md = p.LocationDensity;
            int D(double k) => Math.Max(0, (int)Math.Round(nLand / 350.0 * k * md));
            var roadEdgesList = new List<HexCell[]>();
            foreach (var path in _roads) for (int i = 1; i < path.Count; i++) roadEdgesList.Add(new[] { path[i - 1], path[i] });
            bool IsRiverEdge(HexCell a, HexCell b) => (a.RiverOut >= 0 && g.Step(a, a.RiverOut) == b) || (b.RiverOut >= 0 && g.Step(b, b.RiverOut) == a);
            int baseGuard = 4;
            var crossings = new List<HexCell[]>();
            foreach (var e in roadEdgesList)
            {
                var a = e[0]; var b = e[1]; if (a.IsUnder || b.IsUnder) continue;
                if (_base != null && (g.Dist(a, _base) < baseGuard || g.Dist(b, _base) < baseGuard)) continue;
                if (IsRiverEdge(a, b) || Math.Abs(a.Elevation - b.Elevation) > _elevMax * 0.3) crossings.Add(e);
            }
            bool swampExists = false; foreach (var c in land) if (c.Biome == (int)Biome.Swamp) { swampExists = true; break; }
            int nInv = Math.Max(1, D(1.1)), nCont = Math.Max(1, D(0.45)), nTrace = Math.Max(1, D(1.0)), nRes = Math.Max(1, D(1.1)), nLm = Math.Max(1, D(0.7));
            int nVolc = 1 + (nLand >= 6000 ? 1 : 0);
            int nDead = (nLand >= 1200 ? 1 : 0) + (nLand >= 7000 ? 1 : 0);
            int areaSize = 5 + Math.Min(14, nLand / 900);
            int nObs = Math.Min(6, Math.Max(1, (int)Math.Round(crossings.Count * 0.4)));
            int estTotal = nInv + nCont + nTrace + nRes + nLm + nVolc + nDead + (swampExists ? 1 : 0) + nObs + _factions.Count + 1;
            double spacing = Math.Max(2.2, Math.Sqrt((double)nLand / Math.Max(1, estTotal)) * 0.9);

            SpecialLocation AddLoc(HexCell cell, string archetype, string variant,
                bool ancient = false, bool neutral = false, int? factionOwned = null,
                int biome = -1, bool area = false, int aSize = 0, Anchor anchor = null)
            {
                if (cell == null) return null;
                used.Add(cell.I); placedCells.Add(cell);
                int owner = -1, watchedBy = -1;
                var modifiers = new List<string>();
                var contextTags = new List<string>();
                var evidenceSeedIds = new List<string>();
                if (ancient)
                {
                    contextTags.Add("ancient-origin");
                    evidenceSeedIds.Add("evidence-old-structure");
                }

                // Territory does not equal ownership. A territory only gives this
                // individual location a chance to be claimed, watched or ignored.
                if (factionOwned.HasValue)
                {
                    owner = factionOwned.Value;
                    modifiers.Add("FactionOwned");
                    contextTags.Add("territorial-marker");
                    evidenceSeedIds.Add("evidence-claim-markers");
                }
                else if (!neutral && cell.Faction >= 0)
                {
                    var relationRoll = _rng.NextDouble();
                    var claimChance = ancient ? 0.18 : 0.45;
                    var watchedChance = ancient ? 0.58 : 0.80;
                    if (relationRoll < claimChance)
                    {
                        owner = cell.Faction;
                        modifiers.Add("FactionOwned");
                        contextTags.Add("territorial-claim");
                        evidenceSeedIds.Add("evidence-claim-markers");
                    }
                    else if (relationRoll < watchedChance)
                    {
                        watchedBy = cell.Faction;
                        modifiers.Add("Watched");
                        contextTags.Add("territorial-observation");
                        evidenceSeedIds.Add("evidence-patrol-signs");
                    }

                    if ((owner >= 0 || watchedBy >= 0) && ancient && _rng.NextDouble() < 0.30)
                    {
                        modifiers.Add("Sacred");
                        contextTags.Add("sacred-site");
                        evidenceSeedIds.Add("evidence-ritual-signs");
                    }
                    else if ((owner >= 0 || watchedBy >= 0) && !ancient && _rng.NextDouble() < 0.20)
                    {
                        modifiers.Add("Guarded");
                        contextTags.Add("guarded-site");
                        evidenceSeedIds.Add("evidence-guard-routine");
                    }
                }
                List<HexCell> areaCells = null;
                if (biome >= 0)
                {
                    if (aSize > 0) { areaCells = StampAreaCells(cell, biome, aSize); foreach (var ac in areaCells) used.Add(ac.I); }
                    else { cell.Biome = biome; if (area) for (int d = 0; d < 6; d++) { var n = g.Step(cell, d); if (n != null && !n.IsUnder && _rng.NextDouble() < 0.55) n.Biome = biome; } }
                }
                cell.Special = variant;
                var loc = new SpecialLocation
                {
                    Cell = cell, Archetype = archetype, Variant = variant, Owner = owner, WatchedBy = watchedBy, Modifiers = modifiers,
                    ContextTags = contextTags, EvidenceSeedIds = evidenceSeedIds, Ancient = ancient,
                    Anchor = anchor ?? (areaCells != null ? new Anchor(AnchorKind.Area, areaCells) : new Anchor(AnchorKind.Point, new List<HexCell> { cell }))
                };
                _specials.Add(loc); return loc;
            }

            HexCell TakeBest(List<HexCell> cands, Func<HexCell, double> scoreFn)
            {
                HexCell best = null; double bs = double.NegativeInfinity;
                foreach (var c in cands)
                {
                    if (used.Contains(c.I) || c.Special != null || c.IsUnder) continue;
                    double dd = Spread(c); double rep = (placedCells.Count > 0 && dd < spacing) ? (spacing - dd) * 0.5 : 0;
                    double s = scoreFn(c) - rep + _rng.NextDouble() * 0.06;
                    if (s > bs) { bs = s; best = c; }
                }
                return best;
            }

            // 1. Route obstacles (edge)
            var seenEdge = new HashSet<string>(); int obs = 0;
            foreach (var e in crossings)
            {
                if (obs >= nObs) break; var a = e[0]; var b = e[1];
                string k = HexGrid.EdgeKey(a, b);
                if (seenEdge.Contains(k) || a.Special != null || b.Special != null || used.Contains(a.I) || used.Contains(b.I)) continue;
                if (placedCells.Count > 0 && Spread(a) < spacing * 0.6) continue;
                seenEdge.Add(k);
                AddLoc(a.Elevation <= b.Elevation ? a : b, "Wegehindernis", IsRiverEdge(a, b) ? "Zerstörte Brücke" : "Verschütteter Pass", ancient: true, anchor: new Anchor(AnchorKind.Edge, new List<HexCell> { a, b })); obs++;
            }
            if (obs == 0)
                foreach (var e in crossings)
                {
                    var a = e[0]; var b = e[1]; if (a.Special != null || b.Special != null) continue;
                    AddLoc(a.Elevation <= b.Elevation ? a : b, "Wegehindernis", IsRiverEdge(a, b) ? "Zerstörte Brücke" : "Verschütteter Pass", ancient: true, anchor: new Anchor(AnchorKind.Edge, new List<HexCell> { a, b })); break;
                }

            // 2. Investigation sites (remote, ancient)
            var ruinNames = new[] { "Alte Ruine", "Vergessenes Grab", "Steinkreis", "Verfallener Schrein" };
            for (int k = 0; k < nInv; k++)
                AddLoc(TakeBest(land, c => Remote(c) * 1.3), "Untersuchungsort", _rng.Pick(ruinNames), ancient: true, biome: (_rng.NextDouble() < 0.5 ? (int)Biome.Ruins : -1));

            // 3. Containment sites (remote + high, ancient)
            var contNames = new[] { "Versiegeltes Tor", "Verschlossene Höhle", "Gebannte Schwelle" };
            for (int k = 0; k < nCont; k++)
                AddLoc(TakeBest(land, c => Remote(c) * 0.7 + (c.Elevation / _elevMax) * 0.8), "Verwahrungsort", _rng.Pick(contNames), ancient: true);

            // 4. Trace sites (along roads, neutral)
            var roadCells = new List<HexCell>(); foreach (var path in _roads) foreach (var c in path) if (c.Special == null) roadCells.Add(c);
            var traceNames = new[] { "Verlassenes Lager", "Kalte Feuerstelle", "Zerbrochener Karren", "Altes Bootslager" };
            for (int k = 0; k < nTrace; k++)
                AddLoc(TakeBest(roadCells.Count > 0 ? roadCells : land, c => 1 - Remote(c) * 0.5), "Spurenort", _rng.Pick(traceNames), neutral: true);

            // 5. Resource sites (biome-appropriate)
            for (int k = 0; k < nRes; k++)
            {
                var c = TakeBest(land, cc => NearWater(cc) ? 1.0 : ((cc.Biome == (int)Biome.Forest || cc.Biome == (int)Biome.DeepForest) ? 0.7 : (Coastal(cc) ? 0.6 : 0.1)));
                if (c == null) break;
                string v = NearWater(c) ? _rng.Pick(new[] { "Süßwasserquelle", "Fischgrund" })
                    : (c.Biome == (int)Biome.Forest || c.Biome == (int)Biome.DeepForest) ? _rng.Pick(new[] { "Heilkräuter", "Holzbestand" })
                    : (Coastal(c) ? "Salzvorkommen" : "Beerenhain");
                AddLoc(c, "Ressourcenort", v);
            }

            // 6. Hazard zones (area, biome-driven, neutral)
            var swamps = new List<HexCell>(); foreach (var c in land) if (c.Biome == (int)Biome.Swamp && c.Special == null) swamps.Add(c);
            if (swamps.Count > 0) AddLoc(TakeBest(swamps, c => 1), "Gefahrenzone", "Giftiger Sumpf", neutral: true, anchor: new Anchor(AnchorKind.Area, new List<HexCell>()));
            for (int k = 0; k < nVolc; k++)
                AddLoc(TakeBest(land, c => (c.Elevation / _elevMax) * 0.8 + Remote(c) * 0.4), "Gefahrenzone", "Vulkanschlund", neutral: true, biome: (int)Biome.Volcanic, aSize: areaSize);
            for (int k = 0; k < nDead; k++)
                AddLoc(TakeBest(land, c => Remote(c)), "Gefahrenzone", "Tote Zone", neutral: true, biome: (int)Biome.DeadZones, aSize: (int)Math.Round(areaSize * 0.7));

            // 7. Landmarks (high/prominent, ancient-neutral)
            var lmNames = new[] { "Signalturm", "Gipfelmal", "Kolossstatue", "Alter Leuchtturm" };
            for (int k = 0; k < nLm; k++)
                AddLoc(TakeBest(land, c => (c.Elevation / _elevMax) + (Coastal(c) ? 0.3 : 0)), "Landmarke", _rng.Pick(lmNames), ancient: true);

            // 8. Territorial markers (faction, on borders)
            var gzNames = new[] { "Grenzsteine", "Warnzeichen", "Schädelmarken", "Bänder im Wind" };
            foreach (var f in _factions)
            {
                var border = new List<HexCell>(); foreach (var c in g.Cells) if (c.Faction == f.Id && c.Special == null && IsBorderOf(c, f.Id)) border.Add(c);
                int nn = Math.Min(border.Count, 1 + (f.Taboo.Count > 1 ? 1 : 0));
                for (int k = 0; k < nn; k++) { var c = TakeBest(border, cc => 1); if (c == null) break; AddLoc(c, "Grenzzeichen", _rng.Pick(gzNames), factionOwned: f.Id); }
            }

            DeriveHexData();
        }

        // ===================== Stats & validation (§7.1, §15.1) =====================

        private void ComputeStats()
        {
            var g = _g; var dist = new Dictionary<int, int>(); int land = 0, coastEdges = 0;
            foreach (var c in g.Cells)
            {
                if (c.IsUnder) continue; land++;
                dist.TryGetValue(c.Biome, out int cc); dist[c.Biome] = cc + 1;
                for (int d = 0; d < 6; d++) { var n = g.Step(c, d); if (n != null && n.IsOcean) coastEdges++; }
            }
            double shapeIndex = land > 0 ? coastEdges / Math.Sqrt(land) : 0;
            _stats = new WorldStats { Land = land, Total = g.N, BiomeCounts = dist, ShapeIndex = (float)shapeIndex, LandPct = (float)(100.0 * land / g.N) };
            var w = _stats.Warnings;
            if (shapeIndex < 4.0) w.Add($"Küste sehr rund (Formindex {shapeIndex:F1} < 4.0)");
            foreach (var kv in dist) { double pct = 100.0 * kv.Value / land; if (pct > 60) w.Add($"{Biomes.Name(kv.Key)} bedeckt {pct:F0}% des Landes (>60%)"); }
            if (Math.Abs(land * 100.0 / g.N - _p.LandPercentage) > 12) w.Add($"Land% ({_stats.LandPct:F1}) weicht stark vom Ziel ({_p.LandPercentage}) ab");
            if (_base == null) w.Add("Keine Basis platziert");
            int noSeed = 0; foreach (var f in _factions) if (f.Seed == null) noSeed++; if (noSeed > 0) w.Add($"{noSeed} Fraktion(en) ohne Seed");
            if (_mysteryCore == null) w.Add("Kein Mystery Core");
            var terr = new Dictionary<int, int>(); foreach (var c in g.Cells) if (!c.IsUnder && c.Faction >= 0) { terr.TryGetValue(c.Faction, out int x); terr[c.Faction] = x + 1; }
            foreach (var f in _factions) { terr.TryGetValue(f.Id, out int t); if (t < 8) w.Add($"Fraktion {f.Id} hat sehr kleines Territorium ({t} Hexes)"); }
            if (_specials.Count < 5) w.Add("Weniger als 5 Orte platziert");
        }
    }
}
