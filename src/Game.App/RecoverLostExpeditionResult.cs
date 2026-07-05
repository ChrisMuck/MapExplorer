#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class RecoverLostExpeditionResult
{
    private RecoverLostExpeditionResult(
        bool success,
        LostExpeditionRecord? record,
        int recoveredKnowledge,
        string? archiveEntry,
        string? error)
    {
        Success = success;
        Record = record;
        RecoveredKnowledge = recoveredKnowledge;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public LostExpeditionRecord? Record { get; }

    public int RecoveredKnowledge { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static RecoverLostExpeditionResult Recovered(LostExpeditionRecord record, int recoveredKnowledge, string archiveEntry)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (recoveredKnowledge <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recoveredKnowledge), recoveredKnowledge, "Recovered knowledge must be positive.");
        }

        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Archive entry must not be empty.", nameof(archiveEntry));
        }

        return new RecoverLostExpeditionResult(true, record, recoveredKnowledge, archiveEntry, null);
    }

    public static RecoverLostExpeditionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected recovery result needs an error message.", nameof(error));
        }

        return new RecoverLostExpeditionResult(false, null, 0, null, error);
    }
}
}
