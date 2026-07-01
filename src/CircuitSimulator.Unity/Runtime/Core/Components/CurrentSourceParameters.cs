#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>
/// Immutable parameters for an independent current source.
/// </summary>
internal sealed record CurrentSourceParameters : IComponentParameters
{
    /// <summary>Gets the waveform evaluated during transient analysis.</summary>
    public SourceWaveform TransientWaveform { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.CurrentSource;

    /// <summary>
    /// Initializes a new current-source parameter set.
    /// </summary>
    /// <param name="current">The finite current in amperes.</param>
    public CurrentSourceParameters(double current)
        : this(new ConstantSourceWaveform(current))
    {
    }

    /// <summary>Initializes current-source transient excitation.</summary>
    public CurrentSourceParameters(SourceWaveform transientWaveform)
    {
        TransientWaveform = transientWaveform ?? throw new ArgumentNullException(nameof(transientWaveform));
    }
}

}
