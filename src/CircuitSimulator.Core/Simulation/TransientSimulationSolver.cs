using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Results;

namespace CircuitSimulator.Core.Simulation;

/// <summary>Runs fixed-step backward-Euler transient simulations.</summary>
public sealed class TransientSimulationSolver
{
    private readonly ILinearSystemSolver _linearSystemSolver;
    private readonly MnaAssembler _assembler;

    /// <summary>Initializes a transient simulation solver.</summary>
    public TransientSimulationSolver(
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null)
    {
        _linearSystemSolver = linearSystemSolver ?? new MathNetLinearSystemSolver();
        _assembler = assembler ?? new MnaAssembler();
    }

    /// <summary>Compiles and simulates a physical circuit.</summary>
    public TransientSimulationResult Solve(
        Circuit circuit,
        TransientSimulationOptions options,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(circuit, nameof(circuit));
        return Solve(new CircuitCompiler().Compile(circuit), options, cancellationToken);
    }

    /// <summary>Simulates an already compiled circuit.</summary>
    public TransientSimulationResult Solve(
        CompiledCircuit circuit,
        TransientSimulationOptions options,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(circuit, nameof(circuit));
        Guard.NotNull(options, nameof(options));
        var stepper = new TransientSimulationStepper(
            circuit,
            options.StartTime,
            options.InitialConditions,
            options.NewtonOptions,
            _linearSystemSolver,
            _assembler);
        var samples = new List<TransientSample>();

        while (stepper.CurrentTime < options.StopTime)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = options.StopTime - stepper.CurrentTime;
            var step = Math.Min(options.TimeStep, remaining);
            samples.Add(stepper.Advance(step, cancellationToken));
        }

        return new TransientSimulationResult(circuit, options, samples);
    }
}
