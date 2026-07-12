using System;
using System.Collections.Generic;

namespace WorldGen
{
    /// <summary>
    /// Deterministic PRNG (mulberry32) — reproducible across platforms, unlike
    /// UnityEngine.Random. One seed + same parameters = same world (design §2).
    /// </summary>
    public sealed class Rng
    {
        private uint _a;
        public Rng(uint seed) { _a = seed; }

        /// <summary>Next value in [0,1).</summary>
        public double NextDouble()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = _a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t = ((t + (t ^ (t >> 7)) * (t | 61u)) ^ t);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        /// <summary>Integer in [lo, hi).</summary>
        public int Range(int lo, int hi) => lo + (int)(NextDouble() * (hi - lo));

        /// <summary>Random element of a non-empty list.</summary>
        public T Pick<T>(IReadOnlyList<T> list) => list[(int)(NextDouble() * list.Count)];
    }

    public static class Hashing
    {
        /// <summary>
        /// Stable sub-seed from a master seed + salt parts (FNV-style). Lets each
        /// generation stage draw from its own stream so one layer can be re-rolled
        /// without disturbing the others (design §2.1).
        /// </summary>
        public static uint SubSeed(params object[] parts)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (var part in parts)
                {
                    string s = Convert.ToString(part, System.Globalization.CultureInfo.InvariantCulture) ?? "";
                    foreach (char c in s) { h = (h ^ c) * 16777619u; }
                    h = (h ^ 0x9e3779b9u) * 16777619u;
                }
                return h;
            }
        }

        /// <summary>Deterministic per-cell jitter in [0,1) without allocating an Rng.</summary>
        public static double CellNoise(int x, int z, uint seed)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(z * 19349663) ^ seed;
                h = (h ^ (h >> 13)) * 0x85ebca6bu;
                h ^= h >> 16;
                return h / 4294967296.0;
            }
        }
    }

    /// <summary>
    /// Deterministic value noise + fBm + ridged noise, used for the landmass
    /// silhouette (design §4 Revision).
    /// </summary>
    public sealed class Noise
    {
        private readonly uint _s;
        public Noise(uint seed) { _s = seed; }

        private double Hash(int ix, int iy)
        {
            unchecked
            {
                uint h = ((uint)ix * 0x27d4eb2du) ^ ((uint)iy * 0x165667b1u) ^ (_s * 0x9e3779b1u);
                h = (h ^ (h >> 15)) * 0x85ebca6bu;
                h ^= h >> 13;
                return h / 4294967296.0;
            }
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        public double Sample(double x, double y)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            double u = Fade(x - x0), v = Fade(y - y0);
            return Lerp(
                Lerp(Hash(x0, y0), Hash(x0 + 1, y0), u),
                Lerp(Hash(x0, y0 + 1), Hash(x0 + 1, y0 + 1), u), v);
        }

        public double Fbm(double x, double y, int oct)
        {
            double sum = 0, amp = 0.5, frq = 1, norm = 0;
            for (int o = 0; o < oct; o++) { sum += amp * Sample(x * frq, y * frq); norm += amp; amp *= 0.5; frq *= 2; }
            return sum / norm;
        }

        public double Ridged(double x, double y, int oct)
        {
            double sum = 0, amp = 0.5, frq = 1, norm = 0;
            for (int o = 0; o < oct; o++) { double val = Sample(x * frq, y * frq); val = 1 - Math.Abs(2 * val - 1); sum += amp * val; norm += amp; amp *= 0.5; frq *= 2; }
            return sum / norm;
        }
    }

    public static class MathUtil
    {
        public static double Smoothstep(double a, double b, double x)
        {
            x = Math.Max(0, Math.Min(1, (x - a) / (b - a)));
            return x * x * (3 - 2 * x);
        }
        public static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
