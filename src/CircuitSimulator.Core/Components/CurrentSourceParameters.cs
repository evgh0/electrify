namespace CircuitSimulator.Core.Components;

/// <summary>
/// Immutable parameters for an independent current source.
/// </summary>
public sealed record CurrentSourceParameters : IComponentParameters
{
    /// <summary>
    /// Gets the source current in amperes.
    /// </summary>
    /// <remarks>
    /// Positive current flows from terminal 0, the positive/reference terminal, to terminal 1.
    /// </remarks>
    public double Current { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.CurrentSource;

    /// <summary>
    /// Initializes a new current-source parameter set.
    /// </summary>
    /// <param name="current">The finite current in amperes.</param>
    public CurrentSourceParameters(double current)
    {
        if (!double.IsFinite(current))
        {
            throw new ArgumentOutOfRangeException(nameof(current), current, "Current must be finite.");
        }

        Current = current;
    }
}
