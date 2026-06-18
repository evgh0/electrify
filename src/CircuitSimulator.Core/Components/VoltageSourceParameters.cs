namespace CircuitSimulator.Core.Components;

/// <summary>
/// Immutable parameters for an independent voltage source.
/// </summary>
public sealed record VoltageSourceParameters : IComponentParameters
{
    /// <summary>
    /// Gets the source voltage in volts.
    /// </summary>
    /// <remarks>
    /// The source constrains terminal 0 minus terminal 1 to this voltage.
    /// </remarks>
    public double Voltage { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.VoltageSource;

    /// <summary>
    /// Initializes a new voltage-source parameter set.
    /// </summary>
    /// <param name="voltage">The finite voltage in volts.</param>
    public VoltageSourceParameters(double voltage)
    {
        if (!double.IsFinite(voltage))
        {
            throw new ArgumentOutOfRangeException(nameof(voltage), voltage, "Voltage must be finite.");
        }

        Voltage = voltage;
    }
}
