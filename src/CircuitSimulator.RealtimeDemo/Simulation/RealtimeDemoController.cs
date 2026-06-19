using CircuitSimulator.Core.Results;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.RealtimeDemo.Models;

namespace CircuitSimulator.RealtimeDemo.Simulation;

/// <summary>Observable lifecycle states for the standalone realtime demonstration.</summary>
public enum RealtimeDemoState
{
    /// <summary>The sample stream has not started.</summary>
    NotStarted,
    /// <summary>The sample stream is active.</summary>
    Running,
    /// <summary>The sample stream was stopped or completed.</summary>
    Stopped,
    /// <summary>The sample stream failed.</summary>
    Failed
}

/// <summary>Abstracts an ongoing immutable sample stream for lifecycle tests.</summary>
public interface IRealtimeSampleSource
{
    /// <summary>Streams samples until cancellation, completion, or failure.</summary>
    IAsyncEnumerable<TransientSample> RunAsync(CancellationToken cancellationToken);
}

/// <summary>Adapts the Core realtime session to the demo controller.</summary>
public sealed class CoreRealtimeSampleSource : IRealtimeSampleSource
{
    private readonly RealtimeSimulationSession _session;

    /// <summary>Initializes a Core realtime sample source.</summary>
    public CoreRealtimeSampleSource(RealtimeSimulationSession session) =>
        _session = session ?? throw new ArgumentNullException(nameof(session));

    /// <inheritdoc />
    public IAsyncEnumerable<TransientSample> RunAsync(CancellationToken cancellationToken) =>
        _session.RunAsync(cancellationToken);
}

/// <summary>Runs the Core realtime stream and publishes bounded immutable chart snapshots.</summary>
public sealed class RealtimeDemoController : IAsyncDisposable
{
    private readonly object _lifecycleSync = new();
    private readonly IRealtimeSampleSource _source;
    private readonly RollingSampleBuffer _history;
    private CancellationTokenSource? _cancellation;
    private Task? _completion;
    private int _state;
    private string? _errorMessage;

    /// <summary>Initializes a controller over a predefined circuit and sample source.</summary>
    public RealtimeDemoController(
        RealtimeDemoCircuit circuit,
        IRealtimeSampleSource source,
        RollingSampleBuffer history)
    {
        Circuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    /// <summary>Gets the predefined circuit.</summary>
    public RealtimeDemoCircuit Circuit { get; }

    /// <summary>Gets the current lifecycle state.</summary>
    public RealtimeDemoState State => (RealtimeDemoState)Volatile.Read(ref _state);

    /// <summary>Gets the latest failure diagnostic.</summary>
    public string? ErrorMessage => Volatile.Read(ref _errorMessage);

    /// <summary>Gets the current background-stream completion task.</summary>
    public Task Completion
    {
        get
        {
            lock (_lifecycleSync)
            {
                return _completion ?? Task.CompletedTask;
            }
        }
    }

    /// <summary>Creates a production controller using <see cref="RealtimeSimulationSession"/>.</summary>
    public static RealtimeDemoController Create(RealtimeDemoCircuit circuit)
    {
        ArgumentNullException.ThrowIfNull(circuit);
        var session = new RealtimeSimulationSession(
            circuit.Circuit,
            new RealtimeSimulationOptions(RealtimeDemoCircuit.TimeStep));
        return new RealtimeDemoController(
            circuit,
            new CoreRealtimeSampleSource(session),
            new RollingSampleBuffer(RealtimeDemoCircuit.HistoryDuration, RealtimeDemoCircuit.TimeStep));
    }

    /// <summary>Starts consuming the realtime stream on a background task.</summary>
    public void Start()
    {
        lock (_lifecycleSync)
        {
            if (_completion is not null)
            {
                throw new InvalidOperationException("The realtime demo controller can only be started once.");
            }

            _cancellation = new CancellationTokenSource();
            Volatile.Write(ref _state, (int)RealtimeDemoState.Running);
            _completion = Task.Run(() => ConsumeAsync(_cancellation.Token));
        }
    }

    /// <summary>Builds an immutable chart snapshot for the requested component.</summary>
    public WaveformChartSnapshot CreateSnapshot(CircuitSimulator.Core.Model.ComponentId componentId) =>
        WaveformChartSnapshot.Create(
            Circuit,
            componentId,
            _history.Snapshot(),
            RealtimeDemoCircuit.HistoryDuration);

    /// <summary>Cancels and awaits the background sample consumer.</summary>
    public async Task StopAsync()
    {
        Task? completion;
        lock (_lifecycleSync)
        {
            _cancellation?.Cancel();
            completion = _completion;
        }

        if (completion is not null)
        {
            await completion.ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        lock (_lifecycleSync)
        {
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var sample in _source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                _history.Add(sample);
            }

            Volatile.Write(ref _state, (int)RealtimeDemoState.Stopped);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Volatile.Write(ref _state, (int)RealtimeDemoState.Stopped);
        }
        catch (Exception exception)
        {
            Volatile.Write(ref _errorMessage, exception.Message);
            Volatile.Write(ref _state, (int)RealtimeDemoState.Failed);
        }
    }
}
