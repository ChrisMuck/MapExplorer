/// <summary>
/// Shared deterministic per-hex hashing used for every visual variety decision (rotation, prefab
/// pick, density thresholds, region classification, wind-sway jitter, ...). Same seed + same
/// coordinates always produce the same value, so a given world seed always looks the same.
/// </summary>
public static class HexVisualHash
{
    public static float Value01(int seed, int a, int b, int c)
    {
        unchecked
        {
            var h = seed;
            h = h * 73856093 ^ a * 19349663;
            h = h * 83492791 ^ b * 297121507;
            h = h * 1103515245 ^ c * 12345;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }
}
