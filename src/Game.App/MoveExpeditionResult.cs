#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class MoveExpeditionResult
{
    private MoveExpeditionResult(bool success, HexCoord? from, HexCoord? to, int cost,
        IEnumerable<SceneDescriptionResult>? arrivalScenes, string? error)
    {
        Success = success;
        From = from;
        To = to;
        Cost = cost;
        ArrivalScenes = (arrivalScenes ?? Enumerable.Empty<SceneDescriptionResult>()).ToList();
        Error = error;
    }

    public bool Success { get; }

    public HexCoord? From { get; }

    public HexCoord? To { get; }

    public int Cost { get; }

    public IReadOnlyList<SceneDescriptionResult> ArrivalScenes { get; }

    public string? Error { get; }

    public static MoveExpeditionResult Moved(HexCoord from, HexCoord to, int cost,
        IEnumerable<SceneDescriptionResult>? arrivalScenes = null)
    {
        return new MoveExpeditionResult(true, from, to, cost, arrivalScenes, null);
    }

    public static MoveExpeditionResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected movement needs an error message.", nameof(error));
        }

        return new MoveExpeditionResult(false, null, null, 0, null, error);
    }
}
}
