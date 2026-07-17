#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class CompleteExpeditionResult
{
    private CompleteExpeditionResult(bool success, HexCoord? baseLocation, int expeditionNumber, int expeditionDay,
        int completionWorldDay, int nextExpeditionAvailableWorldDay, int securedKnowledge, int baseKnowledgePoints,
        int returnedFindingsCount, string teamOutcome, string? archiveEntry, string? error)
    {
        Success = success;
        BaseLocation = baseLocation;
        ExpeditionNumber = expeditionNumber;
        ExpeditionDay = expeditionDay;
        CompletionWorldDay = completionWorldDay;
        NextExpeditionAvailableWorldDay = nextExpeditionAvailableWorldDay;
        SecuredKnowledge = securedKnowledge;
        BaseKnowledgePoints = baseKnowledgePoints;
        ReturnedFindingsCount = returnedFindingsCount;
        TeamOutcome = teamOutcome;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public HexCoord? BaseLocation { get; }

    public int ExpeditionNumber { get; }

    public int ExpeditionDay { get; }

    public int CompletionWorldDay { get; }

    public int NextExpeditionAvailableWorldDay { get; }

    public int SecuredKnowledge { get; }

    public int BaseKnowledgePoints { get; }

    public int ReturnedFindingsCount { get; }

    public string TeamOutcome { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static CompleteExpeditionResult Completed(HexCoord baseLocation, int expeditionNumber, int expeditionDay,
        int completionWorldDay, int nextExpeditionAvailableWorldDay, int securedKnowledge, int baseKnowledgePoints,
        int returnedFindingsCount, string teamOutcome, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Completed expedition needs an archive entry.", nameof(archiveEntry));
        }

        if (returnedFindingsCount < 0) throw new ArgumentOutOfRangeException(nameof(returnedFindingsCount));
        if (teamOutcome is not ("all-returned" or "partial-return" or "none-returned"))
            throw new ArgumentException("Unknown team outcome.", nameof(teamOutcome));
        return new CompleteExpeditionResult(true, baseLocation, expeditionNumber, expeditionDay, completionWorldDay,
            nextExpeditionAvailableWorldDay, securedKnowledge, baseKnowledgePoints, returnedFindingsCount, teamOutcome, archiveEntry, null);
    }

    public static CompleteExpeditionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected expedition completion needs an error.", nameof(error));
        }

        return new CompleteExpeditionResult(false, null, 0, 0, 0, 0, 0, 0, 0, "none-returned", null, error);
    }
}
}
