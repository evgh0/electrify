using CircuitSimulator.Core.Results;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Editor.Documents;
using CircuitSimulator.Editor.Simulation;

namespace CircuitSimulator.Editor.Tests;

public sealed class SimulationSessionTests
{
    [Fact]
    public async Task CompletesAndPublishesAResult()
    {
        var instance = DemoCatalog.All[0].Build(DemoCatalog.All[0].Defaults);
        using var session = new SimulationSession(new ImmediateRunner());

        await session.RunAsync(instance);

        Assert.Equal(SimulationRunState.Completed, session.State);
        Assert.NotNull(session.Result);
        Assert.Same(instance, session.ResultInstance);
    }

    [Fact]
    public async Task SupersededRunCannotReplaceNewerResult()
    {
        var firstInstance = DemoCatalog.All[0].Build(DemoCatalog.All[0].Defaults);
        var secondInstance = DemoCatalog.All[1].Build(DemoCatalog.All[1].Defaults);
        var runner = new ControlledRunner();
        using var session = new SimulationSession(runner);

        var firstRun = session.RunAsync(firstInstance);
        var secondRun = session.RunAsync(secondInstance);
        runner.CompleteSecond();
        await secondRun;
        await firstRun;

        Assert.Equal(SimulationRunState.Completed, session.State);
        Assert.Same(secondInstance, session.ResultInstance);
    }

    [Fact]
    public async Task CancellationRetainsPreviousSuccessfulResult()
    {
        var instance = DemoCatalog.All[0].Build(DemoCatalog.All[0].Defaults);
        var runner = new ControlledRunner();
        using var session = new SimulationSession(runner);
        runner.CompleteFirstImmediately = true;
        await session.RunAsync(instance);
        var previous = session.Result;

        runner.CompleteFirstImmediately = false;
        var pending = session.RunAsync(instance);
        session.Cancel();
        await pending;

        Assert.Equal(SimulationRunState.Cancelled, session.State);
        Assert.Same(previous, session.Result);
    }

    [Fact]
    public async Task FailurePublishesDiagnosticAndRetainsPreviousSuccessfulResult()
    {
        var instance = DemoCatalog.All[0].Build(DemoCatalog.All[0].Defaults);
        using var session = new SimulationSession(new SuccessThenFailureRunner());
        await session.RunAsync(instance);
        var previous = session.Result;

        await session.RunAsync(instance);

        Assert.Equal(SimulationRunState.Failed, session.State);
        Assert.Equal("Injected simulation failure.", session.ErrorMessage);
        Assert.Same(previous, session.Result);
        Assert.Same(instance, session.ResultInstance);
    }

    private sealed class ImmediateRunner : ITransientSimulationRunner
    {
        public Task<TransientSimulationResult> RunAsync(
            DemoCircuitInstance instance,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TransientSimulationSolver().Solve(
                instance.Circuit,
                instance.SimulationOptions,
                cancellationToken));
    }

    private sealed class ControlledRunner : ITransientSimulationRunner
    {
        private readonly List<(DemoCircuitInstance Instance, TaskCompletionSource<TransientSimulationResult> Completion)> _runs = [];

        public bool CompleteFirstImmediately { get; set; }

        public Task<TransientSimulationResult> RunAsync(
            DemoCircuitInstance instance,
            CancellationToken cancellationToken)
        {
            if (CompleteFirstImmediately)
            {
                return Task.FromResult(new TransientSimulationSolver().Solve(instance.Circuit, instance.SimulationOptions));
            }

            var completion = new TaskCompletionSource<TransientSimulationResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            _runs.Add((instance, completion));
            return completion.Task;
        }

        public void CompleteSecond()
        {
            var run = _runs[1];
            run.Completion.SetResult(new TransientSimulationSolver().Solve(
                run.Instance.Circuit,
                run.Instance.SimulationOptions));
        }
    }

    private sealed class SuccessThenFailureRunner : ITransientSimulationRunner
    {
        private bool _hasCompleted;

        public Task<TransientSimulationResult> RunAsync(
            DemoCircuitInstance instance,
            CancellationToken cancellationToken)
        {
            if (_hasCompleted)
            {
                return Task.FromException<TransientSimulationResult>(
                    new InvalidOperationException("Injected simulation failure."));
            }

            _hasCompleted = true;
            return Task.FromResult(new TransientSimulationSolver().Solve(
                instance.Circuit,
                instance.SimulationOptions,
                cancellationToken));
        }
    }
}
