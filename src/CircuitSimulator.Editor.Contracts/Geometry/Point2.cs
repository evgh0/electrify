namespace CircuitSimulator.Editor.Contracts.Geometry;

/// <summary>Represents a point in schematic world coordinates.</summary>
public readonly record struct Point2(double X, double Y)
{
    /// <summary>Returns the Euclidean distance to another point.</summary>
    public double DistanceTo(Point2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
