#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Results;

namespace CircuitSimulator.Core.Simulation
{

/// <summary>Owns one ongoing fixed-step transient run for Unity frame-driven realtime simulation.</summary>
internal sealed class RealtimeSimulationSession
{
    private readonly TransientSimulationStepper _stepper;
    private TransientSample? _latestSample;
    private int _activeOperation;

    /// <summary>Initializes a realtime session by compiling a physical circuit once.</summary>
    public RealtimeSimulationSession(
        Circuit circuit,
        RealtimeSimulationOptions options,
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null)
        : this(
            Compile(circuit),
            options,
            linearSystemSolver,
            assembler)
    {
    }

    /// <summary>Initializes a realtime session for an already compiled circuit.</summary>
    public RealtimeSimulationSession(
        CompiledCircuit circuit,
        RealtimeSimulationOptions options,
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null)
    {
        CompiledCircuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _stepper = new TransientSimulationStepper(
            circuit,
            startTime: 0.0,
            options.InitialConditions,
            options.NewtonOptions,
            linearSystemSolver ?? new DenseLinearSystemSolver(),
            assembler ?? new MnaAssembler());
    }

    /// <summary>Gets the immutable compiled circuit used for this run.</summary>
    public CompiledCircuit CompiledCircuit { get; }

    /// <summary>Gets the ongoing simulation settings.</summary>
    public RealtimeSimulationOptions Options { get; }

    /// <summary>Gets the time of the latest committed step, or zero before the first step.</summary>
    public double CurrentTime => LatestSample?.Time ?? 0.0;

    /// <summary>Gets the latest committed sample, or <see langword="null"/> before the first step.</summary>
    public TransientSample? LatestSample => Volatile.Read(ref _latestSample);

    /// <summary>Gets the current control state of an ideal switch.</summary>
    public bool GetSwitchState(ComponentId componentId) => _stepper.GetSwitchState(componentId);

    /// <summary>Attempts to get the current control state of an ideal switch.</summary>
    public bool TryGetSwitchState(ComponentId componentId, out bool isClosed) =>
        _stepper.TryGetSwitchState(componentId, out isClosed);

    /// <summary>Changes an ideal switch state for the next numerical step.</summary>
    public void SetSwitchState(ComponentId componentId, bool isClosed) =>
        _stepper.SetSwitchState(componentId, isClosed);

    /// <summary>Immediately advances exactly one fixed integration step.</summary>
    public TransientSample Advance(CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            var sample = _stepper.Advance(Options.TimeStep, cancellationToken);
            Volatile.Write(ref _latestSample, sample);
            return sample;
        }
        finally
        {
            ExitOperation();
        }
    }

    private static CompiledCircuit Compile(Circuit circuit)
    {
        Guard.NotNull(circuit, nameof(circuit));
        return new CircuitCompiler().Compile(circuit);
    }

    private void EnterOperation()
    {
        if (Interlocked.CompareExchange(ref _activeOperation, 1, 0) != 0)
        {
            throw new InvalidOperationException("The realtime simulation session already has an active operation.");
        }
    }

    private void ExitOperation() => Volatile.Write(ref _activeOperation, 0);
}

}
