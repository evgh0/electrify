#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>
/// Immutable parameters for an independent voltage source.
/// </summary>
internal sealed record VoltageSourceParameters : IComponentParameters
{
    /// <summary>Gets the waveform evaluated during transient analysis.</summary>
    public SourceWaveform TransientWaveform { get; }

    /// <summary>Gets whether this source constrains zero volts for every supported time.</summary>
    internal bool IsZeroForAllTime => TransientWaveform.IsZeroForAllTime;

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.VoltageSource;

    /// <summary>
    /// Initializes a new voltage-source parameter set.
    /// </summary>
    /// <param name="voltage">The finite voltage in volts.</param>
    public VoltageSourceParameters(double voltage)
        : this(new ConstantSourceWaveform(voltage))
    {
    }

    /// <summary>Initializes voltage-source transient excitation.</summary>
    public VoltageSourceParameters(SourceWaveform transientWaveform)
    {
        TransientWaveform = transientWaveform ?? throw new ArgumentNullException(nameof(transientWaveform));
    }
}

}
