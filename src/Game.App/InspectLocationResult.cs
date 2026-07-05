#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class InspectLocationResult
{
    private InspectLocationResult(
        bool success,
        SpecialLocationState? location,
        string message,
        string? archiveEntry,
        string? error)
    {
        Success = success;
        Location = location;
        Message = message;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public SpecialLocationState? Location { get; }

    public string Message { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static InspectLocationResult Inspected(SpecialLocationState location, string message, string? archiveEntry)
    {
        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Inspect result message must not be empty.", nameof(message));
        }

        return new InspectLocationResult(true, location, message, archiveEntry, null);
    }

    public static InspectLocationResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected inspect result needs an error message.", nameof(error));
        }

        return new InspectLocationResult(false, null, error, null, error);
    }
}
}
