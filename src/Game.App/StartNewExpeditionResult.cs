#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class StartNewExpeditionResult
{
    private StartNewExpeditionResult(bool success, int expeditionNumber, HexCoord? startLocation, string? archiveEntry, string? error)
    {
        Success = success;
        ExpeditionNumber = expeditionNumber;
        StartLocation = startLocation;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public int ExpeditionNumber { get; }

    public HexCoord? StartLocation { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static StartNewExpeditionResult Started(int expeditionNumber, HexCoord startLocation, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Started expedition needs an archive entry.", nameof(archiveEntry));
        }

        return new StartNewExpeditionResult(true, expeditionNumber, startLocation, archiveEntry, null);
    }

    public static StartNewExpeditionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected new expedition needs an error.", nameof(error));
        }

        return new StartNewExpeditionResult(false, 0, null, null, error);
    }
}
}
