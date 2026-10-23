using System.Collections.Generic;
using UnityEngine;

namespace MapExplorer.MapStyle
{
    /// <summary>
    /// Spatial index over world-space polylines (rivers, ravines, roads) so terrain code can ask for the
    /// distance to the nearest line without testing every segment.
    /// </summary>
    public sealed class MapStylePolylineIndex
    {
        public struct Hit
        {
            public float Distance;
            public Vector2 Point;
            public Vector2 Tangent;
            public string PathId;
        }

        private readonly struct Segment
        {
            public readonly Vector2 A;
            public readonly Vector2 B;
            public readonly string PathId;

            public Segment(Vector2 a, Vector2 b, string pathId)
            {
                A = a;
                B = b;
                PathId = pathId;
            }
        }

        private readonly float cellSize;
        private readonly Dictionary<Vector2Int, List<Segment>> cells = new();

        public MapStylePolylineIndex(float cellSize = 6f)
        {
            this.cellSize = cellSize;
        }

        public int SegmentCount { get; private set; }

        public void AddPolyline(string pathId, IReadOnlyList<Vector2> points)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                AddSegment(new Segment(points[i], points[i + 1], pathId));
            }
        }

        private void AddSegment(Segment segment)
        {
            SegmentCount++;
            var min = Vector2.Min(segment.A, segment.B);
            var max = Vector2.Max(segment.A, segment.B);
            var from = ToCell(min - Vector2.one * cellSize);
            var to = ToCell(max + Vector2.one * cellSize);
            for (var x = from.x; x <= to.x; x++)
            {
                for (var y = from.y; y <= to.y; y++)
                {
                    var key = new Vector2Int(x, y);
                    if (!cells.TryGetValue(key, out var list))
                    {
                        list = new List<Segment>();
                        cells[key] = list;
                    }

                    list.Add(segment);
                }
            }
        }

        /// <summary>Nearest line within the segment cell of the query point; Distance is infinity when none is near.</summary>
        public Hit Nearest(Vector2 point)
        {
            var best = new Hit { Distance = float.PositiveInfinity };
            if (!cells.TryGetValue(ToCell(point), out var list))
            {
                return best;
            }

            foreach (var segment in list)
            {
                var direction = segment.B - segment.A;
                var lengthSquared = direction.sqrMagnitude;
                var t = lengthSquared < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - segment.A, direction) / lengthSquared);
                var closest = segment.A + direction * t;
                var distance = Vector2.Distance(point, closest);
                if (distance < best.Distance)
                {
                    best.Distance = distance;
                    best.Point = closest;
                    best.Tangent = lengthSquared < 0.0001f ? Vector2.right : direction.normalized;
                    best.PathId = segment.PathId;
                }
            }

            return best;
        }

        public float Distance(Vector2 point)
        {
            return Nearest(point).Distance;
        }

        private Vector2Int ToCell(Vector2 point)
        {
            return new Vector2Int(Mathf.FloorToInt(point.x / cellSize), Mathf.FloorToInt(point.y / cellSize));
        }
    }
}
