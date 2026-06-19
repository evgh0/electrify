using System.Runtime.CompilerServices;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Results;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.RealtimeDemo.Models;
using CircuitSimulator.RealtimeDemo.Simulation;

namespace CircuitSimulator.RealtimeDemo.Tests;

public sealed class RealtimeDemoTests
{
    [Fact]
    public void PredefinedCircuitHasDeterministicTwoStageRcTopology()
    {
        var demo = RealtimeDemoCircuit.Create();
        var compiled = new CircuitCompiler().Compile(demo.Circuit);

        Assert.Equal(5, demo.Components.Count);
        Assert.Equal(
            [
                ComponentKind.VoltageSource,
                ComponentKind.Resistor,
                ComponentKind.Capacitor,
                ComponentKind.Resistor,
                ComponentKind.Capacitor
            ],
            demo.Components.Select(component => component.Kind));
        Assert.Equal(Enumerable.Range(0, 5), demo.Components.Select(component => component.ComponentId.Value));
        Assert.Equal(demo.SecondCapacitorId, demo.DefaultSelectedComponentId);
        Assert.False(compiled.ValidationReport.HasErrors);

        var source = Assert.IsType<VoltageSourceParameters>(demo.Circuit.GetComponent(demo.SourceId).Parameters);
        var waveform = Assert.IsType<SinusoidalSourceWaveform>(source.TransientWaveform);
        Assert.Equal(0.0, waveform.Offset);
        Assert.Equal(5.0, waveform.Amplitude);
        Assert.Equal(RealtimeDemoCircuit.SourceFrequencyHz, waveform.FrequencyHz);
        Assert.Equal(100e-6, Assert.IsType<CapacitorParameters>(
            demo.Circuit.GetComponent(demo.FirstCapacitorId).Parameters).Capacitance);
        Assert.Equal(220e-6, Assert.IsType<CapacitorParameters>(
            demo.Circuit.GetComponent(demo.SecondCapacitorId).Parameters).Capacitance);
    }

    [Fact]
    public void EverySelectableComponentProducesFiniteVipMeasurements()
    {
        var demo = RealtimeDemoCircuit.Create();
        var session = new RealtimeSimulationSession(
            demo.Circuit,
            new RealtimeSimulationOptions(RealtimeDemoCircuit.TimeStep));
        var samples = Enumerable.Range(0, 20).Select(_ => session.Advance()).ToArray();

        foreach (var component in demo.Components)
        {
            var snapshot = WaveformChartSnapshot.Create(
                demo,
                component.ComponentId,
                samples,
                RealtimeDemoCircuit.HistoryDuration);

            Assert.Equal(samples.Length, snapshot.Points.Count);
            Assert.All(snapshot.Points, point =>
            {
                Assert.True(double.IsFinite(point.Voltage));
                Assert.True(double.IsFinite(point.Current));
                Assert.True(double.IsFinite(point.Power));
                Assert.Equal(point.Voltage * point.Current, point.Power);
            });
        }
    }

    [Fact]
    public void RollingBufferIsBoundedOrderedAndRetainsExactWindowBoundary()
    {
        var demo = RealtimeDemoCircuit.Create();
        var session = new RealtimeSimulationSession(
            demo.Circuit,
            new RealtimeSimulationOptions(1.0));
        var buffer = new RollingSampleBuffer(duration: 2.0, timeStep: 1.0);

        for (var index = 0; index < 4; index++)
        {
            buffer.Add(session.Advance());
        }

        var retained = buffer.Snapshot();
        Assert.Equal(3, buffer.Capacity);
        Assert.Equal(3, buffer.Count);
        Assert.Equal([2.0, 3.0, 4.0], retained.Select(sample => sample.Time));
        Assert.Throws<ArgumentException>(() => buffer.Add(retained[^1]));
    }

    [Fact]
    public void ChangingSelectionReusesHistoryWithoutAdvancingSimulation()
    {
        var demo = RealtimeDemoCircuit.Create();
        var session = new RealtimeSimulationSession(
            demo.Circuit,
            new RealtimeSimulationOptions(RealtimeDemoCircuit.TimeStep));
        var samples = Enumerable.Range(0, 100).Select(_ => session.Advance()).ToArray();
        var timeBeforeSelection = session.CurrentTime;

        var first = WaveformChartSnapshot.Create(
            demo,
            demo.FirstCapacitorId,
            samples,
            RealtimeDemoCircuit.HistoryDuration);
        var second = WaveformChartSnapshot.Create(
            demo,
            demo.SecondCapacitorId,
            samples,
            RealtimeDemoCircuit.HistoryDuration);

        Assert.Equal(timeBeforeSelection, session.CurrentTime);
        Assert.Equal(first.StopTime, second.StopTime);
        Assert.Equal("C1", first.Component.Name);
        Assert.Equal("C2", second.Component.Name);
        Assert.NotEqual(first.Latest!.Value.Voltage, second.Latest!.Value.Voltage);
    }

    [Fact]
    public async Task ControllerCancellationStopsBackgroundConsumerCleanly()
    {
        var demo = RealtimeDemoCircuit.Create();
        var controller = new RealtimeDemoController(
            demo,
            new BlockingSampleSource(),
            new RollingSampleBuffer(RealtimeDemoCircuit.HistoryDuration, RealtimeDemoCircuit.TimeStep));

        controller.Start();
        Assert.Equal(RealtimeDemoState.Running, controller.State);
        await controller.StopAsync().WaitAsync(TimeSpan.FromSeconds(2.0));

        Assert.Equal(RealtimeDemoState.Stopped, controller.State);
        Assert.Null(controller.ErrorMessage);
        await controller.DisposeAsync();
    }

    [Fact]
    public async Task ControllerReportsStreamFailureAndStopsConsuming()
    {
        var demo = RealtimeDemoCircuit.Create();
        var controller = new RealtimeDemoController(
            demo,
            new FailingSampleSource(),
            new RollingSampleBuffer(RealtimeDemoCircuit.HistoryDuration, RealtimeDemoCircuit.TimeStep));

        controller.Start();
        await controller.Completion.WaitAsync(TimeSpan.FromSeconds(2.0));

        Assert.Equal(RealtimeDemoState.Failed, controller.State);
        Assert.Equal("Injected realtime demo failure.", controller.ErrorMessage);
        await controller.DisposeAsync();
    }

    private sealed class BlockingSampleSource : IRealtimeSampleSource
    {
        public IAsyncEnumerable<TransientSample> RunAsync(CancellationToken cancellationToken) =>
            BlockAsync(cancellationToken);

        private static async IAsyncEnumerable<TransientSample> BlockAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            yield break;
        }
    }

    private sealed class FailingSampleSource : IRealtimeSampleSource
    {
        public IAsyncEnumerable<TransientSample> RunAsync(CancellationToken cancellationToken) =>
            new FailingAsyncEnumerable(cancellationToken);

        private sealed class FailingAsyncEnumerable : IAsyncEnumerable<TransientSample>, IAsyncEnumerator<TransientSample>
        {
            private readonly CancellationToken _cancellationToken;

            public FailingAsyncEnumerable(CancellationToken cancellationToken) =>
                _cancellationToken = cancellationToken;

            public TransientSample Current =>
                throw new InvalidOperationException("The failing source has no current sample.");

            public IAsyncEnumerator<TransientSample> GetAsyncEnumerator(
                CancellationToken cancellationToken = default) => this;

            public ValueTask<bool> MoveNextAsync()
            {
                _cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromException<bool>(
                    new InvalidOperationException("Injected realtime demo failure."));
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
