#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class CompleteExpeditionResult
{
    private CompleteExpeditionResult(bool success, HexCoord? baseLocation, int expeditionNumber, int expeditionDay, int nextExpeditionAvailableWorldDay, int securedKnowledge, int baseKnowledgePoints, string? archiveEntry, string? error)
    {
        Success = success;
        BaseLocation = baseLocation;
        ExpeditionNumber = expeditionNumber;
        ExpeditionDay = expeditionDay;
        NextExpeditionAvailableWorldDay = nextExpeditionAvailableWorldDay;
        SecuredKnowledge = securedKnowledge;
        BaseKnowledgePoints = baseKnowledgePoints;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public HexCoord? BaseLocation { get; }

    public int ExpeditionNumber { get; }

    public int ExpeditionDay { get; }

    public int NextExpeditionAvailableWorldDay { get; }

    public int SecuredKnowledge { get; }

    public int BaseKnowledgePoints { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static CompleteExpeditionResult Completed(HexCoord baseLocation, int expeditionNumber, int expeditionDay, int nextExpeditionAvailableWorldDay, int securedKnowledge, int baseKnowledgePoints, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Completed expedition needs an archive entry.", nameof(archiveEntry));
        }

        return new CompleteExpeditionResult(true, baseLocation, expeditionNumber, expeditionDay, nextExpeditionAvailableWorldDay, securedKnowledge, baseKnowledgePoints, archiveEntry, null);
    }

    public static CompleteExpeditionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected expedition completion needs an error.", nameof(error));
        }

        return new CompleteExpeditionResult(false, null, 0, 0, 0, 0, 0, null, error);
    }
}
}
