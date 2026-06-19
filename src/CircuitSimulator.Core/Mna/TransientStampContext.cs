using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Simulation;

namespace CircuitSimulator.Core.Mna;

/// <summary>Provides time, committed history, and an optional nonlinear iterate to transient stamps.</summary>
public sealed class TransientStampContext
{
    private readonly double[]? _solutionEstimate;

    /// <summary>Initializes a transient stamp context.</summary>
    public TransientStampContext(
        CompiledCircuit circuit,
        SimulationState state,
        double time,
        double timeStep,
        IReadOnlyList<double>? solutionEstimate = null)
    {
        Circuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        State = state ?? throw new ArgumentNullException(nameof(state));
        if (!ReferenceEquals(circuit, state.Circuit))
        {
            throw new ArgumentException("Simulation state belongs to a different compiled circuit.", nameof(state));
        }

        if (!Guard.IsFinite(time))
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Time must be finite.");
        }

        if (!Guard.IsFinite(timeStep) || timeStep <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "Time step must be finite and greater than zero.");
        }

        if (solutionEstimate is not null)
        {
            if (solutionEstimate.Count != circuit.VariableMap.Dimension)
            {
                throw new ArgumentException("Solution estimate length must match the MNA dimension.", nameof(solutionEstimate));
            }

            _solutionEstimate = solutionEstimate.ToArray();
            if (_solutionEstimate.Any(value => !Guard.IsFinite(value)))
            {
                throw new ArgumentException("Solution estimate values must be finite.", nameof(solutionEstimate));
            }
        }

        Time = time;
        TimeStep = timeStep;
    }

    /// <summary>Gets the compiled circuit.</summary>
    public CompiledCircuit Circuit { get; }

    /// <summary>Gets committed component state.</summary>
    public SimulationState State { get; }

    /// <summary>Gets the target sample time.</summary>
    public double Time { get; }

    /// <summary>Gets the current integration step.</summary>
    public double TimeStep { get; }

    /// <summary>Gets the nonlinear solution estimate when present.</summary>
    public IReadOnlyList<double>? SolutionEstimate => _solutionEstimate;
}
