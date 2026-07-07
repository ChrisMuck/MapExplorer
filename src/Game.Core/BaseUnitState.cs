using System;

namespace Game.Core
{

public enum BaseUnitKind
{
    Porter,
    Soldier
}

/// <summary>A single generic unit in the base stock (porter or soldier) with a rest condition.</summary>
public sealed class BaseUnitState
{
    public BaseUnitState(string id, BaseUnitKind kind, bool isExhausted = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Unit id must not be empty.", nameof(id));
        }

        Id = id;
        Kind = kind;
        IsExhausted = isExhausted;
    }

    public string Id { get; }

    public BaseUnitKind Kind { get; }

    public bool IsExhausted { get; }
}
}
