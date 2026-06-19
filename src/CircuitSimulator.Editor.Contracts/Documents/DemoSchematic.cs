using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Editor.Contracts.Geometry;

namespace CircuitSimulator.Editor.Contracts.Documents;

/// <summary>Identifies a logical component across deterministic circuit rebuilds.</summary>
public readonly record struct DemoComponentKey
{
    /// <summary>Initializes a stable component key.</summary>
    public DemoComponentKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>Gets the stable key value.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Supported fixed symbol orientations.</summary>
public enum ComponentOrientation
{
    /// <summary>Horizontal symbol orientation.</summary>
    Horizontal,
    /// <summary>Vertical symbol orientation.</summary>
    Vertical
}

/// <summary>Places one physical component in a fixed demo schematic.</summary>
public sealed record SchematicComponentPlacement(
    DemoComponentKey Key,
    ComponentId ComponentId,
    Point2 Position,
    ComponentOrientation Orientation = ComponentOrientation.Horizontal);

/// <summary>Defines an orthogonal schematic wire path.</summary>
public sealed class SchematicWirePath
{
    private readonly IReadOnlyList<Point2> _points;

    /// <summary>Initializes a wire path from at least two points.</summary>
    public SchematicWirePath(IEnumerable<Point2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var pointArray = points.ToArray();
        if (pointArray.Length < 2)
        {
            throw new ArgumentException("A schematic wire requires at least two points.", nameof(points));
        }

        _points = new ReadOnlyCollection<Point2>(pointArray);
    }

    /// <summary>Gets path points in drawing order.</summary>
    public IReadOnlyList<Point2> Points => _points;
}

/// <summary>Contains immutable fixed layout for a demo circuit.</summary>
public sealed class DemoSchematic
{
    /// <summary>Initializes a schematic snapshot.</summary>
    public DemoSchematic(
        IEnumerable<SchematicComponentPlacement> components,
        IEnumerable<SchematicWirePath> wires,
        Point2 groundPosition)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(wires);
        Components = new ReadOnlyCollection<SchematicComponentPlacement>(components.ToArray());
        Wires = new ReadOnlyCollection<SchematicWirePath>(wires.ToArray());
        GroundPosition = groundPosition;

        if (Components.Select(component => component.Key).Distinct().Count() != Components.Count)
        {
            throw new ArgumentException("Schematic component keys must be unique.", nameof(components));
        }
    }

    /// <summary>Gets fixed component placements.</summary>
    public IReadOnlyList<SchematicComponentPlacement> Components { get; }

    /// <summary>Gets fixed orthogonal wire paths.</summary>
    public IReadOnlyList<SchematicWirePath> Wires { get; }

    /// <summary>Gets the ground symbol position.</summary>
    public Point2 GroundPosition { get; }
}
