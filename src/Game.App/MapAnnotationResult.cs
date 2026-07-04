#nullable enable
using System;

namespace Game.App
{

public sealed class MapAnnotationResult
{
    private MapAnnotationResult(bool success, string? id, string? error)
    {
        Success = success;
        Id = id;
        Error = error;
    }

    public bool Success { get; }

    public string? Id { get; }

    public string? Error { get; }

    public static MapAnnotationResult Added(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Annotation id must not be empty.", nameof(id));
        }

        return new MapAnnotationResult(true, id, null);
    }

    public static MapAnnotationResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected annotation needs an error message.", nameof(error));
        }

        return new MapAnnotationResult(false, null, error);
    }
}
}
