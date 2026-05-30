using System;
using System.Collections.Generic;

namespace LadybugDB;

/// <summary>Identifies a node or relationship by its table id and in-table offset.</summary>
public readonly record struct InternalId(ulong TableId, ulong Offset)
{
    /// <inheritdoc />
    public override string ToString() => $"{TableId}:{Offset}";
}

/// <summary>
/// A Cypher interval: a calendar-aware duration of months, days, and microseconds. Months and days
/// are kept separate from the absolute microsecond component because their length is calendar
/// dependent.
/// </summary>
public readonly record struct Interval(int Months, int Days, long Micros)
{
    /// <summary>
    /// Converts the day and microsecond components to a <see cref="TimeSpan"/>. The
    /// <see cref="Months"/> component is excluded because a month is not a fixed duration.
    /// </summary>
    public TimeSpan ToTimeSpan() => TimeSpan.FromTicks((Days * TimeSpan.TicksPerDay) + (Micros * 10));
}

/// <summary>A graph node value: its identity, label, and properties.</summary>
public sealed record Node(InternalId Id, string Label, IReadOnlyDictionary<string, object?> Properties);

/// <summary>A graph relationship value: its identity, endpoints, label, and properties.</summary>
public sealed record Rel(
    InternalId Id,
    InternalId Source,
    InternalId Destination,
    string Label,
    IReadOnlyDictionary<string, object?> Properties);

/// <summary>A recursive (variable-length) relationship: the chain of nodes and relationships.</summary>
public sealed record RecursiveRel(IReadOnlyList<Node> Nodes, IReadOnlyList<Rel> Rels);
