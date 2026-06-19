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
    public double Current => DcValue;

    /// <summary>Gets the value used by DC operating-point analysis.</summary>
    public double DcValue { get; }

    /// <summary>Gets the waveform evaluated during transient analysis.</summary>
    public SourceWaveform TransientWaveform { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.CurrentSource;

    /// <summary>
    /// Initializes a new current-source parameter set.
    /// </summary>
    /// <param name="current">The finite current in amperes.</param>
    public CurrentSourceParameters(double current)
        : this(current, new ConstantSourceWaveform(current))
    {
    }

    /// <summary>Initializes current-source DC and transient excitation.</summary>
    public CurrentSourceParameters(double dcValue, SourceWaveform transientWaveform)
    {
        if (!double.IsFinite(dcValue))
        {
            throw new ArgumentOutOfRangeException(nameof(dcValue), dcValue, "DC current must be finite.");
        }

        DcValue = dcValue;
        TransientWaveform = transientWaveform ?? throw new ArgumentNullException(nameof(transientWaveform));
    }
}
