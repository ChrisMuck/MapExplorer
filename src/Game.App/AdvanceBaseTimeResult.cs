#nullable enable

namespace Game.App
{

public sealed class AdvanceBaseTimeResult
{
    private AdvanceBaseTimeResult(bool success, int worldDay, bool nextExpeditionReady, string? worldReactionEntry, string? error)
    {
        Success = success;
        WorldDay = worldDay;
        NextExpeditionReady = nextExpeditionReady;
        WorldReactionEntry = worldReactionEntry;
        Error = error;
    }

    public bool Success { get; }

    public int WorldDay { get; }

    public bool NextExpeditionReady { get; }

    public string? WorldReactionEntry { get; }

    public string? Error { get; }

    public static AdvanceBaseTimeResult Advanced(int worldDay, bool nextExpeditionReady, string? worldReactionEntry = null)
    {
        return new AdvanceBaseTimeResult(true, worldDay, nextExpeditionReady, worldReactionEntry, null);
    }

    public static AdvanceBaseTimeResult Rejected(string error)
    {
        return new AdvanceBaseTimeResult(false, 0, false, null, error);
    }
}
}
