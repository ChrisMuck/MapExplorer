#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class FactionOfferResult
{
    private FactionOfferResult(bool success, FactionOfferState? offer, string? message, string? archiveEntry, string? error)
    {
        Success = success;
        Offer = offer;
        Message = message;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public FactionOfferState? Offer { get; }

    public string? Message { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static FactionOfferResult Accepted(FactionOfferState offer, string message, string archiveEntry)
    {
        if (offer == null)
        {
            throw new ArgumentNullException(nameof(offer));
        }

        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Accepted offer result needs text.");
        }

        return new FactionOfferResult(true, offer, message, archiveEntry, null);
    }

    public static FactionOfferResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected offer result needs an error.", nameof(error));
        }

        return new FactionOfferResult(false, null, null, null, error);
    }
}
}
