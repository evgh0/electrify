using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class RealtimeSimulationTests
{
    [Fact]
    public void OptionsRequireAPositiveFiniteTimeStep()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RealtimeSimulationOptions(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RealtimeSimulationOptions(-1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RealtimeSimulationOptions(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RealtimeSimulationOptions(double.PositiveInfinity));

        var options = new RealtimeSimulationOptions(0.25);

        AssertEx.NearlyEqual(0.25, options.TimeStep);
        Assert.Same(TransientInitialConditions.Zero, options.InitialConditions);
        Assert.Same(NewtonRaphsonOptions.Default, options.NewtonOptions);
    }

    [Fact]
    public void ManualRcStepsMatchBoundedTransientSimulation()
    {
        var (circuit, capacitor) = CreateRcCircuit();
        var bounded = new TransientSimulationSolver().Solve(
            circuit,
            new TransientSimulationOptions(0.0, 2.0, 1.0));
        var realtime = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0));

        AssertEx.NearlyEqual(0.0, realtime.CurrentTime);
        Assert.Null(realtime.LatestSample);

        var first = realtime.Advance();
        var second = realtime.Advance();

        Assert.Same(second, realtime.LatestSample);
        AssertEx.NearlyEqual(2.0, realtime.CurrentTime);
        AssertEx.NearlyEqual(
            bounded.Samples[0].GetComponentVoltage(capacitor.ComponentId),
            first.GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(
            bounded.Samples[1].GetComponentVoltage(capacitor.ComponentId),
            second.GetComponentVoltage(capacitor.ComponentId));
    }

    [Fact]
    public void ManualRlStepsPreserveFixedStepHistory()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 1.0);
        var resistor = builder.AddResistor("R1", 1.0);
        var inductor = builder.AddInductor("L1", 1.0);
        builder.Connect(source.Negative, inductor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, inductor.Positive);
        var session = new RealtimeSimulationSession(
            builder.Build(),
            new RealtimeSimulationOptions(1.0));

        var first = session.Advance();
        var second = session.Advance();

        AssertEx.NearlyEqual(0.5, first.GetComponentCurrent(inductor.ComponentId));
        AssertEx.NearlyEqual(0.75, second.GetComponentCurrent(inductor.ComponentId));
    }

    [Fact]
    public void VariableFrameAccumulatorCanRunZeroOneOrMultipleFixedSteps()
    {
        var (circuit, _) = CreateRcCircuit();
        var session = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(0.1));
        var accumulator = 0.0;

        var firstFrameSteps = AdvanceFrame(session, ref accumulator, 0.04);
        var secondFrameSteps = AdvanceFrame(session, ref accumulator, 0.07);
        var thirdFrameSteps = AdvanceFrame(session, ref accumulator, 0.25);

        Assert.Equal(0, firstFrameSteps);
        Assert.Equal(1, secondFrameSteps);
        Assert.Equal(2, thirdFrameSteps);
        AssertEx.NearlyEqual(0.3, session.CurrentTime);
        AssertEx.NearlyEqual(0.06, accumulator);
    }

    [Fact]
    public void SessionsOverOneCompiledCircuitHaveIndependentHistory()
    {
        var (circuit, capacitor) = CreateRcCircuit();
        var compiled = new CircuitCompiler().Compile(circuit);
        var options = new RealtimeSimulationOptions(1.0);
        var first = new RealtimeSimulationSession(compiled, options);
        var second = new RealtimeSimulationSession(compiled, options);

        first.Advance();
        first.Advance();
        var secondFirstSample = second.Advance();

        Assert.Same(compiled, first.CompiledCircuit);
        Assert.Same(compiled, second.CompiledCircuit);
        AssertEx.NearlyEqual(2.0, first.CurrentTime);
        AssertEx.NearlyEqual(1.0, second.CurrentTime);
        AssertEx.NearlyEqual(0.5, secondFirstSample.GetComponentVoltage(capacitor.ComponentId));
    }

    [Fact]
    public async Task StreamUsesAbsoluteDeadlinesAndCatchesUpWithoutSkippingSteps()
    {
        var (circuit, _) = CreateRcCircuit();
        var timeProvider = new ManualTimeProvider();
        var session = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0),
            timeProvider: timeProvider);
        await using var enumerator = session.RunAsync().GetAsyncEnumerator();

        var firstMove = enumerator.MoveNextAsync().AsTask();
        Assert.False(firstMove.IsCompleted);
        timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        Assert.False(firstMove.IsCompleted);
        timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        Assert.True(await firstMove);
        AssertEx.NearlyEqual(1.0, enumerator.Current.Time);

        var secondMove = enumerator.MoveNextAsync().AsTask();
        timeProvider.Advance(TimeSpan.FromSeconds(2.5));
        Assert.True(await secondMove);
        AssertEx.NearlyEqual(2.0, enumerator.Current.Time);

        Assert.True(await enumerator.MoveNextAsync());
        AssertEx.NearlyEqual(3.0, enumerator.Current.Time);
        AssertEx.NearlyEqual(3.0, session.CurrentTime);
    }

    [Fact]
    public async Task CancellationAndRestartPauseAtTheLastCommittedStep()
    {
        var (circuit, _) = CreateRcCircuit();
        var timeProvider = new ManualTimeProvider();
        var session = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0),
            timeProvider: timeProvider);
        using var firstCancellation = new CancellationTokenSource();
        await using (var first = session.RunAsync(firstCancellation.Token).GetAsyncEnumerator())
        {
            var pending = first.MoveNextAsync().AsTask();
            firstCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }

        AssertEx.NearlyEqual(0.0, session.CurrentTime);
        Assert.Null(session.LatestSample);

        await using (var resumed = session.RunAsync().GetAsyncEnumerator())
        {
            var pending = resumed.MoveNextAsync().AsTask();
            timeProvider.Advance(TimeSpan.FromSeconds(1.0));
            Assert.True(await pending);
            AssertEx.NearlyEqual(1.0, resumed.Current.Time);
        }

        await using var resumedAgain = session.RunAsync().GetAsyncEnumerator();
        var next = resumedAgain.MoveNextAsync().AsTask();
        Assert.False(next.IsCompleted);
        timeProvider.Advance(TimeSpan.FromSeconds(1.0));
        Assert.True(await next);
        AssertEx.NearlyEqual(2.0, resumedAgain.Current.Time);
    }

    [Fact]
    public async Task ActiveStreamRejectsConcurrentOperations()
    {
        var (circuit, _) = CreateRcCircuit();
        var timeProvider = new ManualTimeProvider();
        var session = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0),
            timeProvider: timeProvider);
        using var cancellation = new CancellationTokenSource();
        await using var first = session.RunAsync(cancellation.Token).GetAsyncEnumerator();
        var pending = first.MoveNextAsync().AsTask();

        Assert.Throws<InvalidOperationException>(() => session.Advance());
        await using var second = session.RunAsync().GetAsyncEnumerator();
        await Assert.ThrowsAsync<InvalidOperationException>(() => second.MoveNextAsync().AsTask());

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        AssertEx.NearlyEqual(1.0, session.Advance().Time);
    }

    [Fact]
    public void CancelledAndFailedStepsDoNotCommitHistoryOrTime()
    {
        var (circuit, capacitor) = CreateRcCircuit();
        using var cancellation = new CancellationTokenSource();
        var cancellingSolver = new CancelAfterFirstSolveLinearSystemSolver(cancellation);
        var cancelledSession = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0),
            cancellingSolver);

        Assert.Throws<OperationCanceledException>(() => cancelledSession.Advance(cancellation.Token));
        AssertEx.NearlyEqual(0.0, cancelledSession.CurrentTime);
        Assert.Null(cancelledSession.LatestSample);
        AssertEx.NearlyEqual(
            0.5,
            cancelledSession.Advance().GetComponentVoltage(capacitor.ComponentId));

        var failingSession = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0),
            new FailFirstLinearSystemSolver());
        Assert.Throws<SimulationException>(() => failingSession.Advance());
        AssertEx.NearlyEqual(0.0, failingSession.CurrentTime);
        Assert.Null(failingSession.LatestSample);
        AssertEx.NearlyEqual(
            0.5,
            failingSession.Advance().GetComponentVoltage(capacitor.ComponentId));
    }

    private static int AdvanceFrame(
        RealtimeSimulationSession session,
        ref double accumulator,
        double elapsedTime)
    {
        accumulator += elapsedTime;
        var steps = 0;
        while (accumulator >= session.Options.TimeStep)
        {
            session.Advance();
            accumulator -= session.Options.TimeStep;
            steps++;
        }

        return steps;
    }

    private static (Circuit Circuit, TwoTerminalComponentHandle Capacitor) CreateRcCircuit()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 1.0);
        var resistor = builder.AddResistor("R1", 1.0);
        var capacitor = builder.AddCapacitor("C1", 1.0);
        builder.Connect(source.Negative, capacitor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, capacitor.Positive);
        return (builder.Build(), capacitor);
    }

    private sealed class CancelAfterFirstSolveLinearSystemSolver : ILinearSystemSolver
    {
        private readonly CancellationTokenSource _cancellation;
        private readonly MathNetLinearSystemSolver _inner = new();
        private bool _shouldCancel = true;

        public CancelAfterFirstSolveLinearSystemSolver(CancellationTokenSource cancellation) =>
            _cancellation = cancellation;

        public double[] Solve(MnaLinearSystem system)
        {
            var solution = _inner.Solve(system);
            if (_shouldCancel)
            {
                _shouldCancel = false;
                _cancellation.Cancel();
            }

            return solution;
        }
    }

    private sealed class FailFirstLinearSystemSolver : ILinearSystemSolver
    {
        private readonly MathNetLinearSystemSolver _inner = new();
        private bool _shouldFail = true;

        public double[] Solve(MnaLinearSystem system)
        {
            if (_shouldFail)
            {
                _shouldFail = false;
                throw new SimulationException("Injected realtime solve failure.");
            }

            return _inner.Solve(system);
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly object _sync = new();
        private readonly HashSet<ManualTimer> _timers = [];
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            lock (_sync)
            {
                return _timestamp;
            }
        }

        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync)
            {
                return DateTimeOffset.UnixEpoch + TimeSpan.FromTicks(_timestamp);
            }
        }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var timer = new ManualTimer(this, callback, state);
            lock (_sync)
            {
                _timers.Add(timer);
                timer.ChangeUnderLock(dueTime, period);
            }

            return timer;
        }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }

            List<ManualTimer> due;
            lock (_sync)
            {
                _timestamp = checked(_timestamp + elapsed.Ticks);
                due = _timers.Where(timer => timer.IsDueUnderLock(_timestamp)).ToList();
                foreach (var timer in due)
                {
                    timer.RescheduleUnderLock(_timestamp);
                }
            }

            foreach (var timer in due)
            {
                timer.Fire();
            }
        }

        private void Change(ManualTimer timer, TimeSpan dueTime, TimeSpan period)
        {
            lock (_sync)
            {
                timer.ChangeUnderLock(dueTime, period);
            }
        }

        private void Remove(ManualTimer timer)
        {
            lock (_sync)
            {
                _timers.Remove(timer);
            }
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _owner;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private long _nextTimestamp = long.MaxValue;
            private long _periodTicks = Timeout.InfiniteTimeSpan.Ticks;
            private bool _disposed;

            public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
            {
                _owner = owner;
                _callback = callback;
                _state = state;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                _owner.Change(this, dueTime, period);
                return true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.Remove(this);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public void ChangeUnderLock(TimeSpan dueTime, TimeSpan period)
            {
                ValidateTimeout(dueTime, nameof(dueTime));
                ValidateTimeout(period, nameof(period));
                _periodTicks = period == Timeout.InfiniteTimeSpan ? -1 : period.Ticks;
                _nextTimestamp = dueTime == Timeout.InfiniteTimeSpan
                    ? long.MaxValue
                    : checked(_owner._timestamp + dueTime.Ticks);
            }

            public bool IsDueUnderLock(long timestamp) =>
                !_disposed && _nextTimestamp <= timestamp;

            public void RescheduleUnderLock(long timestamp)
            {
                _nextTimestamp = _periodTicks < 0
                    ? long.MaxValue
                    : checked(timestamp + _periodTicks);
            }

            public void Fire()
            {
                if (!_disposed)
                {
                    _callback(_state);
                }
            }

            private static void ValidateTimeout(TimeSpan value, string parameterName)
            {
                if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(parameterName);
                }
            }
        }
    }
}
