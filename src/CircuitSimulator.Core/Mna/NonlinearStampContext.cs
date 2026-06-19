using CircuitSimulator.Core.Compilation;

namespace CircuitSimulator.Core.Mna;

/// <summary>Provides a finite Newton iterate to nonlinear DC component stamps.</summary>
public sealed class NonlinearStampContext
{
    private readonly double[] _solutionEstimate;

    /// <summary>Initializes a nonlinear stamp context.</summary>
    public NonlinearStampContext(CompiledCircuit circuit, IReadOnlyList<double> solutionEstimate)
    {
        Circuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        ArgumentNullException.ThrowIfNull(solutionEstimate);
        if (solutionEstimate.Count != circuit.VariableMap.Dimension)
        {
            throw new ArgumentException("Solution estimate length must match the MNA dimension.", nameof(solutionEstimate));
        }

        _solutionEstimate = solutionEstimate.ToArray();
        if (_solutionEstimate.Any(value => !double.IsFinite(value)))
        {
            throw new ArgumentException("Solution estimate values must be finite.", nameof(solutionEstimate));
        }
    }

    /// <summary>Gets the compiled circuit.</summary>
    public CompiledCircuit Circuit { get; }

    /// <summary>Gets the immutable solution estimate.</summary>
    public IReadOnlyList<double> SolutionEstimate => _solutionEstimate;
}
