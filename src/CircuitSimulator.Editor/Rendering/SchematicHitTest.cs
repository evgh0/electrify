using CircuitSimulator.Editor.Contracts.Documents;
using CircuitSimulator.Editor.Contracts.Geometry;

namespace CircuitSimulator.Editor.Rendering;

/// <summary>Provides deterministic hit testing for fixed schematic placements.</summary>
public static class SchematicHitTest
{
    /// <summary>Finds the nearest component center inside a world-coordinate tolerance.</summary>
    public static DemoComponentKey? FindNearest(DemoSchematic schematic, Point2 point, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(schematic);
        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance));
        }

        return schematic.Components
            .Select(component => (component.Key, Distance: component.Position.DistanceTo(point)))
            .Where(item => item.Distance <= tolerance)
            .OrderBy(item => item.Distance)
            .Select(item => (DemoComponentKey?)item.Key)
            .FirstOrDefault();
    }
}
