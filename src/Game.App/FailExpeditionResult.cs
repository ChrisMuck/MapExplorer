#nullable enable
using System;

namespace Game.App
{

public sealed class FailExpeditionResult
{
    private FailExpeditionResult(bool success, int expeditionNumber, int nextExpeditionAvailableWorldDay, int lostUnsecuredKnowledge, string? archiveEntry, string? error)
    {
        Success = success;
        ExpeditionNumber = expeditionNumber;
        NextExpeditionAvailableWorldDay = nextExpeditionAvailableWorldDay;
        LostUnsecuredKnowledge = lostUnsecuredKnowledge;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public int ExpeditionNumber { get; }

    public int NextExpeditionAvailableWorldDay { get; }

    public int LostUnsecuredKnowledge { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static FailExpeditionResult Failed(int expeditionNumber, int nextExpeditionAvailableWorldDay, int lostUnsecuredKnowledge, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Failed expedition needs an archive entry.", nameof(archiveEntry));
        }

        return new FailExpeditionResult(true, expeditionNumber, nextExpeditionAvailableWorldDay, lostUnsecuredKnowledge, archiveEntry, null);
    }

    public static FailExpeditionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected expedition failure needs an error.", nameof(error));
        }

        return new FailExpeditionResult(false, 0, 0, 0, null, error);
    }
}
}
