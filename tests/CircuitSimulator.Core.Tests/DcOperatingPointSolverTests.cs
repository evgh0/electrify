using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Core.Tests;

public sealed class DcOperatingPointSolverTests
{
    [Fact]
    public void SolvesVoltageDividerWithDocumentedSignConventions()
    {
        var (circuit, voltageSource, resistor1, resistor2) = CircuitTestFactory.CreateVoltageDivider();
        var result = new DcOperatingPointSolver().Solve(circuit);

        AssertEx.NearlyEqual(10.0, result.GetTerminalVoltage(voltageSource.Positive));
        AssertEx.NearlyEqual(20.0 / 3.0, result.GetTerminalVoltage(resistor2.Positive));
        AssertEx.NearlyEqual(-1.0 / 300.0, result.GetBranchCurrent(voltageSource.ComponentId));
        AssertEx.NearlyEqual(1.0 / 300.0, result.GetComponentCurrent(resistor1.ComponentId));
        AssertEx.NearlyEqual(1.0 / 300.0, result.GetComponentCurrent(resistor2.ComponentId));
        AssertEx.NearlyEqual(-1.0 / 300.0, result.GetComponentCurrent(voltageSource.ComponentId));
    }

    [Fact]
    public void CurrentSourceIntoResistorToGroundMatchesOhmsLaw()
    {
        var builder = new CircuitBuilder();
        var currentSource = builder.AddCurrentSource("I1", 0.002);
        var resistor = builder.AddResistor("R1", 1_000.0);

        builder.Connect(currentSource.Positive, resistor.Negative);
        builder.MarkAsGround(currentSource.Positive);
        builder.Connect(currentSource.Negative, resistor.Positive);

        var result = new DcOperatingPointSolver().Solve(builder.Build());

        AssertEx.NearlyEqual(2.0, result.GetTerminalVoltage(resistor.Positive));
        AssertEx.NearlyEqual(0.002, result.GetComponentCurrent(resistor.ComponentId));
        AssertEx.NearlyEqual(0.002, result.GetComponentCurrent(currentSource.ComponentId));
        Assert.False(result.TryGetBranchCurrent(resistor.ComponentId, out _));
    }

    [Fact]
    public void ParallelResistorsAccumulateConductance()
    {
        var builder = new CircuitBuilder();
        var currentSource = builder.AddCurrentSource("I1", 0.003);
        var resistor1 = builder.AddResistor("R1", 1_000.0);
        var resistor2 = builder.AddResistor("R2", 2_000.0);

        builder.Connect(currentSource.Positive, resistor1.Negative, resistor2.Negative);
        builder.MarkAsGround(currentSource.Positive);
        builder.Connect(currentSource.Negative, resistor1.Positive, resistor2.Positive);

        var result = new DcOperatingPointSolver().Solve(builder.Build());

        AssertEx.NearlyEqual(2.0, result.GetTerminalVoltage(resistor1.Positive));
        AssertEx.NearlyEqual(0.002, result.GetComponentCurrent(resistor1.ComponentId));
        AssertEx.NearlyEqual(0.001, result.GetComponentCurrent(resistor2.ComponentId));
    }

    [Fact]
    public void MultipleVoltageSourceBranchCurrentsUseDeterministicComponentOrder()
    {
        var builder = new CircuitBuilder();
        var voltageSource1 = builder.AddVoltageSource("V1", 5.0);
        var voltageSource2 = builder.AddVoltageSource("V2", 3.0);
        var resistor1 = builder.AddResistor("R1", 1_000.0);
        var resistor2 = builder.AddResistor("R2", 1_500.0);

        builder.Connect(voltageSource1.Negative, voltageSource2.Negative, resistor1.Negative, resistor2.Negative);
        builder.MarkAsGround(voltageSource1.Negative);
        builder.Connect(voltageSource1.Positive, resistor1.Positive);
        builder.Connect(voltageSource2.Positive, resistor2.Positive);

        var compiled = new CircuitCompiler().Compile(builder.Build());
        var result = new DcOperatingPointSolver().Solve(compiled);

        Assert.Equal(new VariableIndex(2), compiled.VariableMap.GetBranchCurrentIndex(voltageSource1.ComponentId));
        Assert.Equal(new VariableIndex(3), compiled.VariableMap.GetBranchCurrentIndex(voltageSource2.ComponentId));
        AssertEx.NearlyEqual(-0.005, result.GetBranchCurrent(voltageSource1.ComponentId));
        AssertEx.NearlyEqual(-0.002, result.GetBranchCurrent(voltageSource2.ComponentId));
    }

    [Fact]
    public void FloatingCircuitFailsBeforeSolve()
    {
        var builder = new CircuitBuilder();
        var resistor = builder.AddResistor("R1", 100.0);
        var voltageSource = builder.AddVoltageSource("V1", 1.0);

        builder.MarkAsGround(voltageSource.Negative);
        builder.Connect(voltageSource.Positive, voltageSource.Negative);
        builder.Connect(resistor.Positive, resistor.Negative);

        var exception = Assert.Throws<CircuitCompilationException>(() => new DcOperatingPointSolver().Solve(builder.Build()));

        Assert.Contains(exception.ValidationReport.Issues, issue => issue.Code == ValidationCodes.VoltageSourceSelfLoopNonzero);
    }

    [Fact]
    public void GroundOnlyPassiveCircuitProducesZeroDimensionalResult()
    {
        var builder = new CircuitBuilder();
        var resistor = builder.AddResistor("R1", 100.0);

        builder.Connect(resistor.Positive, resistor.Negative);
        builder.MarkAsGround(resistor.Positive);

        var result = new DcOperatingPointSolver().Solve(builder.Build());

        Assert.Equal(0, result.CompiledCircuit.VariableMap.Dimension);
        Assert.Empty(result.Solution);
        AssertEx.NearlyEqual(0.0, result.GetComponentCurrent(resistor.ComponentId));
    }

    [Fact]
    public void AssembledVoltageDividerMatrixMatchesExpectedMnaSystem()
    {
        var (circuit, _, _, _) = CircuitTestFactory.CreateVoltageDivider();
        var compiled = new CircuitCompiler().Compile(circuit);
        var system = new MnaAssembler().AssembleDc(compiled);

        AssertEx.NearlyEqual(0.001, system.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(-0.001, system.GetMatrixEntry(0, 1));
        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(0, 2));
        AssertEx.NearlyEqual(-0.001, system.GetMatrixEntry(1, 0));
        AssertEx.NearlyEqual(0.0015, system.GetMatrixEntry(1, 1));
        AssertEx.NearlyEqual(0.0, system.GetMatrixEntry(1, 2));
        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(2, 0));
        AssertEx.NearlyEqual(0.0, system.GetMatrixEntry(2, 1));
        AssertEx.NearlyEqual(0.0, system.GetMatrixEntry(0, 2) - system.GetMatrixEntry(2, 0));
        AssertEx.NearlyEqual(10.0, system.GetRightHandSideEntry(2));
    }
}
