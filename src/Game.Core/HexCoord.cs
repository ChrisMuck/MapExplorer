using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public readonly struct HexCoord : IEquatable<HexCoord>
{
    public HexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    public int Q { get; }

    public int R { get; }

    public int S
    {
        get { return -Q - R; }
    }

    public static HexCoord Zero { get; } = new(0, 0);

    public HexCoord Neighbor(HexDirection direction)
    {
        return this + direction.ToOffset();
    }

    public IReadOnlyList<HexCoord> Neighbors()
    {
        return new[]
        {
            Neighbor(HexDirection.East),
            Neighbor(HexDirection.NorthEast),
            Neighbor(HexDirection.NorthWest),
            Neighbor(HexDirection.West),
            Neighbor(HexDirection.SouthWest),
            Neighbor(HexDirection.SouthEast)
        };
    }

    public int DistanceTo(HexCoord other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    public static HexCoord operator +(HexCoord left, HexCoord right)
    {
        return new HexCoord(left.Q + right.Q, left.R + right.R);
    }

    public static HexCoord operator -(HexCoord left, HexCoord right)
    {
        return new HexCoord(left.Q - right.Q, left.R - right.R);
    }

    public static bool operator ==(HexCoord left, HexCoord right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HexCoord left, HexCoord right)
    {
        return !left.Equals(right);
    }

    public bool Equals(HexCoord other)
    {
        return Q == other.Q && R == other.R;
    }

    public override bool Equals(object? obj)
    {
        return obj is HexCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Q, R);
    }

    public override string ToString()
    {
        return $"({Q}, {R})";
    }
}
}
