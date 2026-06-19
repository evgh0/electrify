using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Results;

namespace CircuitSimulator.Core.Simulation;

/// <summary>Owns mutable state and advances one transient simulation through accepted fixed steps.</summary>
internal sealed class TransientSimulationStepper
{
    private readonly CompiledCircuit _circuit;
    private readonly SimulationState _state;
    private readonly ILinearSystemSolver _linearSystemSolver;
    private readonly MnaAssembler _assembler;
    private readonly NewtonRaphsonOptions _newtonOptions;
    private readonly NewtonRaphsonSolver? _nonlinearSolver;
    private double[] _previousSolution;
    private double _currentTime;

    public TransientSimulationStepper(
        CompiledCircuit circuit,
        double startTime,
        TransientInitialConditions initialConditions,
        NewtonRaphsonOptions newtonOptions,
        ILinearSystemSolver linearSystemSolver,
        MnaAssembler assembler)
    {
        _circuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        Guard.NotNull(initialConditions, nameof(initialConditions));
        _newtonOptions = newtonOptions ?? throw new ArgumentNullException(nameof(newtonOptions));
        _linearSystemSolver = linearSystemSolver ?? throw new ArgumentNullException(nameof(linearSystemSolver));
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
        if (!Guard.IsFinite(startTime) || startTime < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTime), startTime, "Start time must be finite and non-negative.");
        }

        _state = SimulationState.Create(circuit, initialConditions);
        _previousSolution = new double[circuit.VariableMap.Dimension];
        _nonlinearSolver = NonlinearCircuitUtilities.ContainsNonlinearComponents(circuit)
            ? new NewtonRaphsonSolver(linearSystemSolver)
            : null;
        _currentTime = startTime;
    }

    public double CurrentTime => Volatile.Read(ref _currentTime);

    public TransientSample Advance(double timeStep, CancellationToken cancellationToken)
    {
        if (!Guard.IsFinite(timeStep) || timeStep <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "Time step must be finite and greater than zero.");
        }

        var targetTime = CurrentTime + timeStep;
        if (!Guard.IsFinite(targetTime) || targetTime <= CurrentTime)
        {
            throw new SimulationException(
                $"A transient step of {timeStep:R} s cannot advance simulation time {CurrentTime:R} s.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var solution = Solve(targetTime, timeStep, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var sample = new TransientSample(_circuit, targetTime, timeStep, _state, solution);
        cancellationToken.ThrowIfCancellationRequested();
        _state.Commit(solution, timeStep);
        _previousSolution = solution;
        Volatile.Write(ref _currentTime, targetTime);
        return sample;
    }

    private double[] Solve(double targetTime, double timeStep, CancellationToken cancellationToken)
    {
        if (_nonlinearSolver is null)
        {
            var system = _assembler.AssembleTransient(
                new TransientStampContext(_circuit, _state, targetTime, timeStep));
            return _linearSystemSolver.Solve(system);
        }

        try
        {
            return _nonlinearSolver.Solve(
                _circuit.VariableMap,
                estimate => _assembler.AssembleTransient(
                    new TransientStampContext(_circuit, _state, targetTime, timeStep, estimate)),
                _previousSolution,
                _newtonOptions,
                (current, candidate) => NonlinearCircuitUtilities.LimitDiodeVoltageStep(
                    _circuit,
                    current,
                    candidate,
                    _newtonOptions.MaximumDiodeVoltageStep),
                cancellationToken);
        }
        catch (NonlinearConvergenceException exception)
        {
            throw new NonlinearConvergenceException(
                $"Nonlinear transient solve failed at time {targetTime:R} s after {exception.IterationCount} iterations; maximum correction was {exception.MaximumCorrection:R}.",
                exception.IterationCount,
                exception.MaximumCorrection,
                targetTime,
                exception);
        }
    }
}
