#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class FactionInteractionResult
{
    private FactionInteractionResult(bool success, FactionInteractionState? interaction, string? message,
        FactionContactPresentation? presentation, string? error)
    {
        Success = success;
        Interaction = interaction;
        Message = message;
        Presentation = presentation;
        Error = error;
    }

    public bool Success { get; }

    public FactionInteractionState? Interaction { get; }

    public string? Message { get; }

    public FactionContactPresentation? Presentation { get; }

    public string? Error { get; }

    public static FactionInteractionResult Opened(FactionInteractionState interaction, string message,
        FactionContactPresentation? presentation = null)
    {
        if (interaction == null)
        {
            throw new ArgumentNullException(nameof(interaction));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Interaction result needs a message.", nameof(message));
        }

        return new FactionInteractionResult(true, interaction, message, presentation, null);
    }

    public static FactionInteractionResult Closed(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Interaction result needs a message.", nameof(message));
        }

        return new FactionInteractionResult(true, null, message, null, null);
    }

    public static FactionInteractionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected interaction result needs an error.", nameof(error));
        }

        return new FactionInteractionResult(false, null, null, null, error);
    }
}
}
