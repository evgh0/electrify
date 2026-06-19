using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class TransientSimulationTests
{
    [Fact]
    public void RcChargingMatchesBackwardEulerRecurrence()
    {
        var (circuit, capacitor) = CreateRcCircuit(sourceVoltage: 1.0);

        var result = new TransientSimulationSolver().Solve(
            circuit,
            new TransientSimulationOptions(0.0, 2.0, 1.0));

        Assert.Equal(2, result.Samples.Count);
        AssertEx.NearlyEqual(0.5, result.Samples[0].GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(0.75, result.Samples[1].GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(0.25, result.Samples[1].GetComponentCurrent(capacitor.ComponentId));
    }

    [Fact]
    public void RcDischargingUsesExplicitInitialVoltage()
    {
        var (circuit, capacitor) = CreateRcCircuit(sourceVoltage: 0.0);
        var initial = new TransientInitialConditions(
            capacitorVoltages: new Dictionary<ComponentId, double> { [capacitor.ComponentId] = 1.0 });

        var result = new TransientSimulationSolver().Solve(
            circuit,
            new TransientSimulationOptions(0.0, 1.0, 1.0, initial));

        AssertEx.NearlyEqual(0.5, result.Samples[0].GetComponentVoltage(capacitor.ComponentId));
        AssertEx.NearlyEqual(-0.5, result.Samples[0].GetComponentCurrent(capacitor.ComponentId));
    }

    [Fact]
    public void RlStepMatchesBackwardEulerRecurrence()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 1.0);
        var resistor = builder.AddResistor("R1", 1.0);
        var inductor = builder.AddInductor("L1", 1.0);
        builder.Connect(source.Negative, inductor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, inductor.Positive);

        var result = new TransientSimulationSolver().Solve(
            builder.Build(),
            new TransientSimulationOptions(0.0, 2.0, 1.0));

        AssertEx.NearlyEqual(0.5, result.Samples[0].GetComponentCurrent(inductor.ComponentId));
        AssertEx.NearlyEqual(0.75, result.Samples[1].GetComponentCurrent(inductor.ComponentId));
    }

    [Fact]
    public void FinalStepIsShortenedAndRunsAreIndependent()
    {
        var (circuit, capacitor) = CreateRcCircuit(sourceVoltage: 1.0);
        var options = new TransientSimulationOptions(0.0, 2.5, 1.0);
        var solver = new TransientSimulationSolver();

        var first = solver.Solve(circuit, options);
        var second = solver.Solve(circuit, options);

        Assert.Equal(new[] { 1.0, 2.0, 2.5 }, first.Samples.Select(sample => sample.Time));
        AssertEx.NearlyEqual(0.5, first.Samples[^1].TimeStep);
        AssertEx.NearlyEqual(
            first.Samples[0].GetComponentVoltage(capacitor.ComponentId),
            second.Samples[0].GetComponentVoltage(capacitor.ComponentId));
    }

    [Fact]
    public void InvalidInitialConditionAndCancellationFailBeforeACommit()
    {
        var (circuit, capacitor) = CreateRcCircuit(sourceVoltage: 1.0);
        var compiled = new CircuitCompiler().Compile(circuit);
        var invalid = new TransientInitialConditions(
            inductorCurrents: new Dictionary<ComponentId, double> { [capacitor.ComponentId] = 1.0 });
        Assert.Throws<SimulationException>(() => SimulationState.Create(compiled, invalid));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            new TransientSimulationSolver().Solve(
                compiled,
                new TransientSimulationOptions(0.0, 1.0, 0.1),
                cancellation.Token));
    }

    [Fact]
    public void FailedStateCommitLeavesAllReactiveHistoryUnchanged()
    {
        var (circuit, capacitor) = CreateRcCircuit(sourceVoltage: 1.0);
        var compiled = new CircuitCompiler().Compile(circuit);
        var initial = new TransientInitialConditions(
            capacitorVoltages: new Dictionary<ComponentId, double> { [capacitor.ComponentId] = 2.0 });
        var state = SimulationState.Create(compiled, initial);
        var invalidSolution = new double[compiled.VariableMap.Dimension];
        invalidSolution[compiled.VariableMap.GetNodeVoltageIndex(
            compiled.Netlist.GetNode(capacitor.Positive))!.Value.Value] = double.NaN;

        Assert.Throws<SimulationException>(() => state.Commit(invalidSolution, 1.0));
        AssertEx.NearlyEqual(2.0, state.GetCapacitorState(capacitor.ComponentId).PreviousVoltage);
        AssertEx.NearlyEqual(0.0, state.GetCapacitorState(capacitor.ComponentId).PreviousCurrent);
    }

    [Fact]
    public void InvalidTimingOptionsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransientSimulationOptions(-1.0, 1.0, 0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransientSimulationOptions(1.0, 1.0, 0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransientSimulationOptions(0.0, 1.0, 0.0));
    }

    private static (Circuit Circuit, TwoTerminalComponentHandle Capacitor) CreateRcCircuit(double sourceVoltage)
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", sourceVoltage);
        var resistor = builder.AddResistor("R1", 1.0);
        var capacitor = builder.AddCapacitor("C1", 1.0);
        builder.Connect(source.Negative, capacitor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, capacitor.Positive);
        return (builder.Build(), capacitor);
    }
}
