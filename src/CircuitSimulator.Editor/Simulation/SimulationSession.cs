using CircuitSimulator.Core.Results;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Editor.Documents;

namespace CircuitSimulator.Editor.Simulation;

/// <summary>Lifecycle state for editor-managed transient simulation.</summary>
public enum SimulationRunState
{
    /// <summary>No simulation has started.</summary>
    NotRun,
    /// <summary>A simulation is running.</summary>
    Running,
    /// <summary>The latest simulation completed.</summary>
    Completed,
    /// <summary>The latest simulation was cancelled.</summary>
    Cancelled,
    /// <summary>The latest simulation failed.</summary>
    Failed
}

/// <summary>Abstracts transient execution for orchestration tests.</summary>
public interface ITransientSimulationRunner
{
    /// <summary>Runs one immutable demo instance.</summary>
    Task<TransientSimulationResult> RunAsync(
        DemoCircuitInstance instance,
        CancellationToken cancellationToken);
}

/// <summary>Runs the core transient solver away from the caller thread.</summary>
public sealed class CoreTransientSimulationRunner : ITransientSimulationRunner
{
    /// <inheritdoc />
    public Task<TransientSimulationResult> RunAsync(
        DemoCircuitInstance instance,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return Task.Run(
            () => new TransientSimulationSolver().Solve(
                instance.Circuit,
                instance.SimulationOptions,
                cancellationToken),
            cancellationToken);
    }
}

/// <summary>Coordinates cancellable runs while preserving the last successful result.</summary>
public sealed class SimulationSession : IDisposable
{
    private readonly ITransientSimulationRunner _runner;
    private CancellationTokenSource? _cancellation;
    private long _generation;

    /// <summary>Initializes a simulation session.</summary>
    public SimulationSession(ITransientSimulationRunner? runner = null) =>
        _runner = runner ?? new CoreTransientSimulationRunner();

    /// <summary>Raised after externally visible state changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the current run state.</summary>
    public SimulationRunState State { get; private set; } = SimulationRunState.NotRun;

    /// <summary>Gets the last successful result.</summary>
    public TransientSimulationResult? Result { get; private set; }

    /// <summary>Gets the instance associated with the last successful result.</summary>
    public DemoCircuitInstance? ResultInstance { get; private set; }

    /// <summary>Gets the latest failure diagnostic.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Runs a demo and ignores completion from superseded runs.</summary>
    public async Task RunAsync(DemoCircuitInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        CancelActive(setCancelledState: false);
        var generation = ++_generation;
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        State = SimulationRunState.Running;
        ErrorMessage = null;
        Changed?.Invoke(this, EventArgs.Empty);

        try
        {
            var result = await _runner.RunAsync(instance, token);
            if (generation != _generation)
            {
                return;
            }

            Result = result;
            ResultInstance = instance;
            State = SimulationRunState.Completed;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            if (generation == _generation)
            {
                State = SimulationRunState.Cancelled;
            }
        }
        catch (Exception exception)
        {
            if (generation != _generation)
            {
                return;
            }

            ErrorMessage = exception.Message;
            State = SimulationRunState.Failed;
        }
        finally
        {
            if (generation == _generation)
            {
                _cancellation?.Dispose();
                _cancellation = null;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Cancels the active run without clearing a previous successful result.</summary>
    public void Cancel() => CancelActive(setCancelledState: true);

    /// <inheritdoc />
    public void Dispose()
    {
        CancelActive(setCancelledState: false);
        _cancellation?.Dispose();
    }

    private void CancelActive(bool setCancelledState)
    {
        if (_cancellation is not { IsCancellationRequested: false } cancellation)
        {
            return;
        }

        _generation++;
        cancellation.Cancel();
        cancellation.Dispose();
        _cancellation = null;
        if (setCancelledState)
        {
            State = SimulationRunState.Cancelled;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
