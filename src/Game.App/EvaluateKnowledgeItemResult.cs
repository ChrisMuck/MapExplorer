#nullable enable
using System;

namespace Game.App
{

public sealed class EvaluateKnowledgeItemResult
{
    private EvaluateKnowledgeItemResult(
        bool success,
        string? itemId,
        string? name,
        int knowledgeAwarded,
        int remainingKnowledgePoints,
        string? insightText,
        string? archiveEntry,
        string? error)
    {
        Success = success;
        ItemId = itemId;
        Name = name;
        KnowledgeAwarded = knowledgeAwarded;
        RemainingKnowledgePoints = remainingKnowledgePoints;
        InsightText = insightText;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public string? ItemId { get; }

    public string? Name { get; }

    public int KnowledgeAwarded { get; }

    public int RemainingKnowledgePoints { get; }

    public string? InsightText { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static EvaluateKnowledgeItemResult Evaluated(string itemId, string name, int knowledgeAwarded, int remainingKnowledgePoints, string insightText, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Evaluated item needs an archive entry.", nameof(archiveEntry));
        }

        return new EvaluateKnowledgeItemResult(true, itemId, name, knowledgeAwarded, remainingKnowledgePoints, insightText, archiveEntry, null);
    }

    public static EvaluateKnowledgeItemResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected evaluation needs an error.", nameof(error));
        }

        return new EvaluateKnowledgeItemResult(false, null, null, 0, 0, null, null, error);
    }
}
}
