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
    public double Voltage => DcValue;

    /// <summary>Gets the value used by DC operating-point analysis.</summary>
    public double DcValue { get; }

    /// <summary>Gets the waveform evaluated during transient analysis.</summary>
    public SourceWaveform TransientWaveform { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.VoltageSource;

    /// <summary>
    /// Initializes a new voltage-source parameter set.
    /// </summary>
    /// <param name="voltage">The finite voltage in volts.</param>
    public VoltageSourceParameters(double voltage)
        : this(voltage, new ConstantSourceWaveform(voltage))
    {
    }

    /// <summary>Initializes voltage-source DC and transient excitation.</summary>
    public VoltageSourceParameters(double dcValue, SourceWaveform transientWaveform)
    {
        if (!double.IsFinite(dcValue))
        {
            throw new ArgumentOutOfRangeException(nameof(dcValue), dcValue, "DC voltage must be finite.");
        }

        DcValue = dcValue;
        TransientWaveform = transientWaveform ?? throw new ArgumentNullException(nameof(transientWaveform));
    }
}
