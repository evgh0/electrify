namespace CircuitSimulator.Core.Numerics;

/// <summary>
/// Central numerical tolerances used by solvers and tests.
/// </summary>
public static class NumericalConstants
{
    /// <summary>
    /// Default pivot tolerance for singular-matrix detection.
    /// </summary>
    public const double DefaultPivotTolerance = 1e-12;

    /// <summary>
    /// Default comparison tolerance for public examples and tests.
    /// </summary>
    public const double DefaultComparisonTolerance = 1e-9;
}
