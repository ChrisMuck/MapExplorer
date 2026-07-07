#nullable enable
using System;

namespace Game.App
{

public sealed class StartBaseActionResult
{
    private StartBaseActionResult(
        bool success,
        BaseActionKind kind,
        int knowledgeSpent,
        int remainingKnowledgePoints,
        int worldDay,
        string? archiveEntry,
        string? error)
    {
        Success = success;
        Kind = kind;
        KnowledgeSpent = knowledgeSpent;
        RemainingKnowledgePoints = remainingKnowledgePoints;
        WorldDay = worldDay;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public BaseActionKind Kind { get; }

    public int KnowledgeSpent { get; }

    public int RemainingKnowledgePoints { get; }

    public int WorldDay { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static StartBaseActionResult Applied(
        BaseActionKind kind,
        int knowledgeSpent,
        int remainingKnowledgePoints,
        int worldDay,
        string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Applied base action needs an archive entry.", nameof(archiveEntry));
        }

        return new StartBaseActionResult(true, kind, knowledgeSpent, remainingKnowledgePoints, worldDay, archiveEntry, null);
    }

    public static StartBaseActionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected base action needs an error.", nameof(error));
        }

        return new StartBaseActionResult(false, default, 0, 0, 0, null, error);
    }
}
}
