using System.Collections.ObjectModel;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Simulation;

namespace CircuitSimulator.Core.Results;

/// <summary>Immutable result of a completed transient simulation.</summary>
public sealed class TransientSimulationResult
{
    /// <summary>Initializes a transient result snapshot.</summary>
    public TransientSimulationResult(
        CompiledCircuit compiledCircuit,
        TransientSimulationOptions options,
        IEnumerable<TransientSample> samples)
    {
        CompiledCircuit = compiledCircuit ?? throw new ArgumentNullException(nameof(compiledCircuit));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(samples);
        Samples = new ReadOnlyCollection<TransientSample>(samples.ToArray());
    }

    /// <summary>Gets the reused compiled circuit.</summary>
    public CompiledCircuit CompiledCircuit { get; }

    /// <summary>Gets the simulation settings.</summary>
    public TransientSimulationOptions Options { get; }

    /// <summary>Gets accepted samples in ascending time order.</summary>
    public IReadOnlyList<TransientSample> Samples { get; }
}
