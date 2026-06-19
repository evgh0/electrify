using System.Runtime.CompilerServices;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Results;

namespace CircuitSimulator.Core.Simulation;

/// <summary>
/// Owns one ongoing fixed-step transient run and can advance it manually or at realtime wall-clock pace.
/// </summary>
/// <remarks>
/// A session is bound to one immutable compiled circuit. It supports one active operation at a time and
/// does not retain sample history beyond <see cref="LatestSample"/>.
/// </remarks>
public sealed class RealtimeSimulationSession
{
    private readonly TransientSimulationStepper _stepper;
    private readonly TimeProvider _timeProvider;
    private TransientSample? _latestSample;
    private int _activeOperation;

    /// <summary>Initializes a realtime session by compiling a physical circuit once.</summary>
    public RealtimeSimulationSession(
        Circuit circuit,
        RealtimeSimulationOptions options,
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null,
        TimeProvider? timeProvider = null)
        : this(
            Compile(circuit),
            options,
            linearSystemSolver,
            assembler,
            timeProvider)
    {
    }

    /// <summary>Initializes a realtime session for an already compiled circuit.</summary>
    public RealtimeSimulationSession(
        CompiledCircuit circuit,
        RealtimeSimulationOptions options,
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null,
        TimeProvider? timeProvider = null)
    {
        CompiledCircuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _stepper = new TransientSimulationStepper(
            circuit,
            startTime: 0.0,
            options.InitialConditions,
            options.NewtonOptions,
            linearSystemSolver ?? new MathNetLinearSystemSolver(),
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

    /// <summary>
    /// Immediately advances exactly one fixed integration step without wall-clock pacing.
    /// </summary>
    /// <remarks>
    /// A variable-frame-rate application can maintain an elapsed-time accumulator and call this method
    /// zero, one, or multiple times per rendered frame. It must preserve any fractional remainder.
    /// </remarks>
    public TransientSample Advance(CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            return AdvanceCore(cancellationToken);
        }
        finally
        {
            ExitOperation();
        }
    }

    /// <summary>
    /// Streams fixed-step samples indefinitely at best-effort 1x wall-clock pace.
    /// </summary>
    /// <remarks>
    /// Absolute monotonic deadlines prevent scheduling drift. If solving falls behind, steps run without
    /// additional delay until caught up; integration steps are never enlarged or skipped. Disposing or
    /// cancelling the enumeration pauses the session at its last committed sample. A later enumeration
    /// resumes from that state with a new wall-clock anchor.
    /// </remarks>
    public async IAsyncEnumerable<TransientSample> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            var wallClockAnchor = _timeProvider.GetTimestamp();
            long stepsSinceAnchor = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                stepsSinceAnchor = checked(stepsSinceAnchor + 1);
                var targetElapsedSeconds = stepsSinceAnchor * Options.TimeStep;
                if (!double.IsFinite(targetElapsedSeconds) ||
                    targetElapsedSeconds > TimeSpan.MaxValue.TotalSeconds)
                {
                    throw new SimulationException("Realtime wall-clock deadline exceeded the supported duration.");
                }

                var targetElapsed = TimeSpan.FromSeconds(targetElapsedSeconds);
                var elapsed = _timeProvider.GetElapsedTime(wallClockAnchor);
                var remaining = targetElapsed - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, _timeProvider, cancellationToken).ConfigureAwait(false);
                }

                yield return AdvanceCore(cancellationToken);
            }
        }
        finally
        {
            ExitOperation();
        }
    }

    private static CompiledCircuit Compile(Circuit circuit)
    {
        ArgumentNullException.ThrowIfNull(circuit);
        return new CircuitCompiler().Compile(circuit);
    }

    private TransientSample AdvanceCore(CancellationToken cancellationToken)
    {
        var sample = _stepper.Advance(Options.TimeStep, cancellationToken);
        Volatile.Write(ref _latestSample, sample);
        return sample;
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
