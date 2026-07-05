#nullable enable

namespace Game.App
{

public sealed class AdvanceBaseTimeResult
{
    private AdvanceBaseTimeResult(bool success, int worldDay, bool nextExpeditionReady, string? error)
    {
        Success = success;
        WorldDay = worldDay;
        NextExpeditionReady = nextExpeditionReady;
        Error = error;
    }

    public bool Success { get; }

    public int WorldDay { get; }

    public bool NextExpeditionReady { get; }

    public string? Error { get; }

    public static AdvanceBaseTimeResult Advanced(int worldDay, bool nextExpeditionReady)
    {
        return new AdvanceBaseTimeResult(true, worldDay, nextExpeditionReady, null);
    }

    public static AdvanceBaseTimeResult Rejected(string error)
    {
        return new AdvanceBaseTimeResult(false, 0, false, error);
    }
}
}
