#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class ResolveEventResult
{
    private ResolveEventResult(bool success, EventState? eventState, EventOptionState? option, string? message, string? error)
    {
        Success = success;
        Event = eventState;
        Option = option;
        Message = message;
        Error = error;
    }

    public bool Success { get; }

    public EventState? Event { get; }

    public EventOptionState? Option { get; }

    public string? Message { get; }

    public string? Error { get; }

    public static ResolveEventResult Resolved(EventState eventState, EventOptionState option, string message)
    {
        if (eventState == null)
        {
            throw new ArgumentNullException(nameof(eventState));
        }

        if (option == null)
        {
            throw new ArgumentNullException(nameof(option));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Resolved event needs a message.", nameof(message));
        }

        return new ResolveEventResult(true, eventState, option, message, null);
    }

    public static ResolveEventResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected event resolution needs an error.", nameof(error));
        }

        return new ResolveEventResult(false, null, null, null, error);
    }
}
}
