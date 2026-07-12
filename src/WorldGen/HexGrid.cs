using System;
using System.Collections.Generic;

namespace WorldGen
{
    /// <summary>
    /// One logical hex tile — pure data, no art (design §2). Elevation is a
    /// continuous float used for movement/rivers/climate/sightlines only.
    /// </summary>
    public sealed class HexCell
    {
        public int I;                 // flat index
        public int Col, Row;          // offset coords (x,z)
        public int AX, AZ;            // axial coords (AX = Col - (Row>>1), AZ = Row)

        public float Elevation, WaterLevel;
        public float Moisture, Clouds, Temperature;
        public int Biome = -1;

        public int RiverIn = -1, RiverOut = -1;   // hex direction of incoming/outgoing river, or -1
        public bool IsLake, IsOcean;

        public int Faction = -1;      // territory owner (influence), or -1 = unclaimed
        public float Influence;
        public bool Contested;

        public int Settlement = -1;   // faction id whose settlement sits here, or -1
        public bool IsCapital;

        public float MoveCost = 1, Visibility = 1, Hazard;

        public string Special;        // special-location variant label, if any
        public string PlaceName;      // settlement name, if a settlement
        public string RiverName;      // river-system name, if a river cell

        public bool IsUnder => Elevation < WaterLevel;
        public float ViewElev => Elevation >= WaterLevel ? Elevation : WaterLevel;
        public bool HasRiver => RiverIn >= 0 || RiverOut >= 0;
    }

    /// <summary>
    /// Pointy-top hex grid with axial coordinates (Catlike Coding convention).
    /// Directions order: NE, E, SE, SW, W, NW.
    /// </summary>
    public sealed class HexGrid
    {
        public readonly int W, H, N;
        public readonly HexCell[] Cells;

        private static readonly int[,] DIR = { { 0, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, 0 }, { -1, 1 } };

        public HexGrid(int w, int h)
        {
            W = w; H = h; N = w * h;
            Cells = new HexCell[N];
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                {
                    int i = x + z * w;
                    Cells[i] = new HexCell { I = i, Col = x, Row = z, AZ = z, AX = x - (z >> 1) };
                }
        }

        public HexCell Get(int x, int z)
        {
            if (x < 0 || z < 0 || x >= W || z >= H) return null;
            return Cells[x + z * W];
        }

        /// <summary>Neighbor of c in direction d (0..5), or null off-grid.</summary>
        public HexCell Step(HexCell c, int d)
        {
            int nX = c.AX + DIR[d, 0], nZ = c.AZ + DIR[d, 1];
            int nx = nX + (nZ >> 1);
            return Get(nx, nZ);
        }

        public int Dist(HexCell a, HexCell b)
        {
            int aY = -a.AX - a.AZ, bY = -b.AX - b.AZ;
            return (Math.Abs(a.AX - b.AX) + Math.Abs(aY - bY) + Math.Abs(a.AZ - b.AZ)) / 2;
        }

        public static int Opp(int d) => (d + 3) % 6;

        public static string EdgeKey(HexCell a, HexCell b) => a.I < b.I ? a.I + "-" + b.I : b.I + "-" + a.I;
    }

    /// <summary>Binary min-heap keyed by a double priority. Used for Dijkstra / A*.</summary>
    public sealed class Heap<T>
    {
        private readonly struct Node { public readonly T Item; public readonly double Pri; public Node(T i, double p) { Item = i; Pri = p; } }
        private readonly List<Node> _a = new List<Node>();
        public int Size => _a.Count;

        public void Push(T item, double pri)
        {
            _a.Add(new Node(item, pri));
            int i = _a.Count - 1;
            while (i > 0)
            {
                int par = (i - 1) >> 1;
                if (_a[par].Pri <= _a[i].Pri) break;
                (_a[par], _a[i]) = (_a[i], _a[par]);
                i = par;
            }
        }

        public T Pop()
        {
            var top = _a[0];
            var last = _a[_a.Count - 1];
            _a.RemoveAt(_a.Count - 1);
            if (_a.Count > 0)
            {
                _a[0] = last;
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, s = i;
                    if (l < _a.Count && _a[l].Pri < _a[s].Pri) s = l;
                    if (r < _a.Count && _a[r].Pri < _a[s].Pri) s = r;
                    if (s == i) break;
                    (_a[s], _a[i]) = (_a[i], _a[s]);
                    i = s;
                }
            }
            return top.Item;
        }
    }
}
