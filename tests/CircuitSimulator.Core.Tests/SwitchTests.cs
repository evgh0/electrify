using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Core.Tests;

public sealed class SwitchTests
{
    [Fact]
    public void BuilderCreatesAnIdealSwitchWithStableInitialState()
    {
        var builder = new CircuitBuilder();
        var switchHandle = builder.AddSwitch("S1", initiallyClosed: true);
        var circuit = builder.Build();

        var parameters = Assert.IsType<SwitchParameters>(
            circuit.GetComponent(switchHandle.ComponentId).Parameters);
        Assert.True(parameters.InitiallyClosed);
        Assert.Equal(ComponentKind.Switch, parameters.Kind);
    }

    [Fact]
    public void VariableMapAllocatesSwitchesAfterVoltageSourcesAndInductors()
    {
        var builder = new CircuitBuilder();
        var switch2 = builder.AddSwitch("S2");
        var inductor = builder.AddInductor("L1", 1.0);
        var voltageSource = builder.AddVoltageSource("V1", 1.0);
        var switch1 = builder.AddSwitch("S1");
        builder.Connect(switch2.Positive, inductor.Positive, voltageSource.Positive, switch1.Positive);
        builder.Connect(switch2.Negative, inductor.Negative, voltageSource.Negative, switch1.Negative);
        builder.MarkAsGround(voltageSource.Negative);

        var compiled = new CircuitCompiler().Compile(builder.Build());

        Assert.Equal(new VariableIndex(1), compiled.VariableMap.GetBranchCurrentIndex(voltageSource.ComponentId));
        Assert.Equal(new VariableIndex(2), compiled.VariableMap.GetBranchCurrentIndex(inductor.ComponentId));
        Assert.Equal(new VariableIndex(3), compiled.VariableMap.GetBranchCurrentIndex(switch2.ComponentId));
        Assert.Equal(new VariableIndex(4), compiled.VariableMap.GetBranchCurrentIndex(switch1.ComponentId));
    }

    [Fact]
    public void OpenAndClosedSwitchesStampFixedDimensionExactEquations()
    {
        var open = CompileParallelSwitch(initiallyClosed: false);
        var closed = CompileParallelSwitch(initiallyClosed: true);

        var openSystem = new MnaAssembler().AssembleDc(open);
        var closedSystem = new MnaAssembler().AssembleDc(closed);

        Assert.Equal(2, openSystem.Dimension);
        Assert.Equal(2, closedSystem.Dimension);
        AssertEx.NearlyEqual(1.0, openSystem.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(0.0, openSystem.GetMatrixEntry(0, 1));
        AssertEx.NearlyEqual(0.0, openSystem.GetMatrixEntry(1, 0));
        AssertEx.NearlyEqual(1.0, openSystem.GetMatrixEntry(1, 1));
        AssertEx.NearlyEqual(1.0, closedSystem.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(1.0, closedSystem.GetMatrixEntry(0, 1));
        AssertEx.NearlyEqual(1.0, closedSystem.GetMatrixEntry(1, 0));
        AssertEx.NearlyEqual(0.0, closedSystem.GetMatrixEntry(1, 1));
    }

    [Theory]
    [InlineData(false, 0.0, 5.0, 0.0)]
    [InlineData(true, 5.0, 0.0, 0.005)]
    public void DcUsesInitialStateAndMapsSwitchReadings(
        bool initiallyClosed,
        double expectedLoadVoltage,
        double expectedSwitchVoltage,
        double expectedSwitchCurrent)
    {
        var (circuit, switchHandle, load) = CreateDrivenSwitchCircuit(initiallyClosed);

        var result = new DcOperatingPointSolver().Solve(circuit);

        AssertEx.NearlyEqual(expectedLoadVoltage, result.GetComponentVoltage(load.ComponentId));
        AssertEx.NearlyEqual(expectedSwitchVoltage, result.GetComponentVoltage(switchHandle.ComponentId));
        AssertEx.NearlyEqual(expectedSwitchCurrent, result.GetComponentCurrent(switchHandle.ComponentId));
        AssertEx.NearlyEqual(expectedSwitchCurrent, result.GetBranchCurrent(switchHandle.ComponentId));
    }

    [Theory]
    [InlineData(false, 0.0)]
    [InlineData(true, 5.0)]
    public void BoundedTransientUsesTheConfiguredInitialState(
        bool initiallyClosed,
        double expectedLoadVoltage)
    {
        var (circuit, _, load) = CreateDrivenSwitchCircuit(initiallyClosed);

        var result = new TransientSimulationSolver().Solve(
            circuit,
            new TransientSimulationOptions(0.0, 0.1, 0.1));

        AssertEx.NearlyEqual(
            expectedLoadVoltage,
            result.Samples[0].GetComponentVoltage(load.ComponentId));
    }

    [Fact]
    public void InitiallyClosedSelfLoopIsRejected()
    {
        var builder = new CircuitBuilder();
        var switchHandle = builder.AddSwitch("S1", initiallyClosed: true);
        builder.Connect(switchHandle.Positive, switchHandle.Negative);
        builder.MarkAsGround(switchHandle.Negative);

        var exception = Assert.Throws<CircuitCompilationException>(
            () => new CircuitCompiler().Compile(builder.Build()));

        Assert.Contains(
            exception.ValidationReport.Issues,
            issue => issue.Code == ValidationCodes.MnaSingularSystem &&
                     issue.ComponentId == switchHandle.ComponentId);
    }

    [Fact]
    public void RealtimeSwitchingPreservesTimeAndCapacitorHistory()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 1.0);
        var resistor = builder.AddResistor("R1", 1.0);
        var switchHandle = builder.AddSwitch("S1");
        var capacitor = builder.AddCapacitor("C1", 1.0);
        var bleeder = builder.AddResistor("R2", 1.0);
        builder.Connect(source.Negative, capacitor.Negative, bleeder.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, switchHandle.Positive);
        builder.Connect(switchHandle.Negative, capacitor.Positive, bleeder.Positive);
        var session = new RealtimeSimulationSession(
            builder.Build(),
            new RealtimeSimulationOptions(1.0));

        var open = session.Advance();
        session.SetSwitchState(switchHandle.ComponentId, true);
        var charging1 = session.Advance();
        var charging2 = session.Advance();
        session.SetSwitchState(switchHandle.ComponentId, false);
        var discharging = session.Advance();

        AssertEx.NearlyEqual(0.0, open.GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(1.0 / 3.0, charging1.GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(4.0 / 9.0, charging2.GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(2.0 / 9.0, discharging.GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(4.0, session.CurrentTime);
        Assert.False(session.GetSwitchState(switchHandle.ComponentId));
    }

    [Fact]
    public void SingularOpenStepCanBeRetriedAfterClosingWithoutReset()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 1.0);
        var resistor = builder.AddResistor("R1", 1.0);
        var switchHandle = builder.AddSwitch("S1", initiallyClosed: true);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(source.Negative, resistor.Negative, switchHandle.Positive);
        builder.MarkAsGround(switchHandle.Negative);
        var session = new RealtimeSimulationSession(
            builder.Build(),
            new RealtimeSimulationOptions(0.25));
        var accepted = session.Advance();

        session.SetSwitchState(switchHandle.ComponentId, false);
        Assert.Throws<SingularMatrixException>(() => session.Advance());
        Assert.Same(accepted, session.LatestSample);
        AssertEx.NearlyEqual(0.25, session.CurrentTime);

        session.SetSwitchState(switchHandle.ComponentId, true);
        var recovered = session.Advance();

        AssertEx.NearlyEqual(0.5, recovered.Time);
    }

    [Fact]
    public async Task ConcurrentChangeAppliesAfterTheAlreadyAssembledStep()
    {
        var (circuit, switchHandle, load) = CreateDrivenSwitchCircuit(initiallyClosed: false);
        using var solver = new BlockingLinearSystemSolver();
        var session = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(0.1),
            solver);

        var pending = Task.Run(() => session.Advance());
        Assert.True(solver.WaitUntilEntered(TimeSpan.FromSeconds(5)));
        session.SetSwitchState(switchHandle.ComponentId, true);
        solver.Continue();
        var openSample = await pending;
        var closedSample = session.Advance();

        AssertEx.NearlyEqual(0.0, openSample.GetComponentVoltage(load.ComponentId));
        AssertEx.NearlyEqual(5.0, closedSample.GetComponentVoltage(load.ComponentId));
    }

    [Fact]
    public async Task ActiveRealtimeStreamAcceptsAStateChangeForItsNextStep()
    {
        var (circuit, switchHandle, load) = CreateDrivenSwitchCircuit(initiallyClosed: false);
        var timeProvider = new ManualTimeProvider();
        var session = new RealtimeSimulationSession(
            circuit,
            new RealtimeSimulationOptions(1.0),
            timeProvider: timeProvider);
        await using var stream = session.RunAsync().GetAsyncEnumerator();

        var pending = stream.MoveNextAsync().AsTask();
        session.SetSwitchState(switchHandle.ComponentId, true);
        timeProvider.Advance(TimeSpan.FromSeconds(1.0));

        Assert.True(await pending);
        AssertEx.NearlyEqual(5.0, stream.Current.GetComponentVoltage(load.ComponentId));
    }

    [Fact]
    public void SwitchStateApisValidateComponentKinds()
    {
        var (circuit, switchHandle, load) = CreateDrivenSwitchCircuit(initiallyClosed: false);
        var session = new RealtimeSimulationSession(circuit, new RealtimeSimulationOptions(0.1));

        Assert.True(session.TryGetSwitchState(switchHandle.ComponentId, out var isClosed));
        Assert.False(isClosed);
        Assert.False(session.TryGetSwitchState(load.ComponentId, out _));
        Assert.Throws<SimulationException>(() => session.GetSwitchState(load.ComponentId));
        Assert.Throws<SimulationException>(() => session.SetSwitchState(new ComponentId(999), true));
    }

    private static CompiledCircuit CompileParallelSwitch(bool initiallyClosed)
    {
        var builder = new CircuitBuilder();
        var resistor = builder.AddResistor("R1", 1.0);
        var switchHandle = builder.AddSwitch("S1", initiallyClosed);
        builder.Connect(resistor.Positive, switchHandle.Positive);
        builder.Connect(resistor.Negative, switchHandle.Negative);
        builder.MarkAsGround(resistor.Negative);
        return new CircuitCompiler().Compile(builder.Build());
    }

    private static (Circuit Circuit, TwoTerminalComponentHandle Switch, TwoTerminalComponentHandle Load)
        CreateDrivenSwitchCircuit(bool initiallyClosed)
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 5.0);
        var switchHandle = builder.AddSwitch("S1", initiallyClosed);
        var load = builder.AddResistor("R1", 1_000.0);
        builder.Connect(source.Negative, load.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, switchHandle.Positive);
        builder.Connect(switchHandle.Negative, load.Positive);
        return (builder.Build(), switchHandle, load);
    }

    private sealed class BlockingLinearSystemSolver : ILinearSystemSolver, IDisposable
    {
        private readonly ManualResetEventSlim _entered = new(false);
        private readonly ManualResetEventSlim _continue = new(false);
        private readonly MathNetLinearSystemSolver _inner = new();
        private int _shouldBlock = 1;

        public double[] Solve(MnaLinearSystem system)
        {
            if (Interlocked.Exchange(ref _shouldBlock, 0) == 1)
            {
                _entered.Set();
                _continue.Wait();
            }

            return _inner.Solve(system);
        }

        public bool WaitUntilEntered(TimeSpan timeout) => _entered.Wait(timeout);

        public void Continue() => _continue.Set();

        public void Dispose()
        {
            _entered.Dispose();
            _continue.Dispose();
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
            ManualTimer[] due;
            lock (_sync)
            {
                _timestamp += elapsed.Ticks;
                due = _timers.Where(timer => timer.IsDueUnderLock(_timestamp)).ToArray();
            }

            foreach (var timer in due)
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _owner;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private long? _dueTimestamp;
            private bool _disposed;

            public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
            {
                _owner = owner;
                _callback = callback;
                _state = state;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_owner._sync)
                {
                    ChangeUnderLock(dueTime, period);
                    return !_disposed;
                }
            }

            public void ChangeUnderLock(TimeSpan dueTime, TimeSpan period)
            {
                _dueTimestamp = dueTime == Timeout.InfiniteTimeSpan
                    ? null
                    : _owner._timestamp + dueTime.Ticks;
            }

            public bool IsDueUnderLock(long timestamp) =>
                !_disposed && _dueTimestamp.HasValue && _dueTimestamp.Value <= timestamp;

            public void Fire()
            {
                lock (_owner._sync)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _dueTimestamp = null;
                }

                _callback(_state);
            }

            public void Dispose()
            {
                lock (_owner._sync)
                {
                    _disposed = true;
                    _owner._timers.Remove(this);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
