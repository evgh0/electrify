using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Validation;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class CircuitCompilerTests
{
    [Fact]
    public void VoltageDividerCompilesWithDeterministicGroundAndNodeNumbering()
    {
        var (circuit, voltageSource, resistor1, resistor2) = CircuitTestFactory.CreateVoltageDivider();
        var compiled = new CircuitCompiler().Compile(circuit);

        Assert.Equal(NodeId.Ground, compiled.Netlist.GroundNodeId);
        Assert.Equal(3, compiled.Netlist.Nodes.Count);
        Assert.True(compiled.Netlist.Nodes[0].IsGround);
        Assert.Equal(new NodeId(1), compiled.Netlist.GetNode(voltageSource.Positive));
        Assert.Equal(new NodeId(1), compiled.Netlist.GetNode(resistor1.Positive));
        Assert.Equal(new NodeId(2), compiled.Netlist.GetNode(resistor1.Negative));
        Assert.Equal(new NodeId(2), compiled.Netlist.GetNode(resistor2.Positive));
        Assert.Equal(NodeId.Ground, compiled.Netlist.GetNode(voltageSource.Negative));
        Assert.Equal(NodeId.Ground, compiled.Netlist.GetNode(resistor2.Negative));
    }

    [Fact]
    public void DuplicateWiresDoNotChangeCompiledTopology()
    {
        var builder = new CircuitBuilder();
        var voltageSource = builder.AddVoltageSource("V1", 5.0);
        var resistor = builder.AddResistor("R1", 100.0);

        builder.Connect(voltageSource.Negative, resistor.Negative);
        builder.Connect(voltageSource.Negative, resistor.Negative);
        builder.Connect(voltageSource.Positive, resistor.Positive);
        builder.Connect(voltageSource.Positive, resistor.Positive);
        builder.MarkAsGround(voltageSource.Negative);

        var compiled = new CircuitCompiler().Compile(builder.Build());

        Assert.Equal(2, compiled.Netlist.Nodes.Count);
        Assert.Equal(new NodeId(1), compiled.Netlist.GetNode(voltageSource.Positive));
        Assert.Equal(new NodeId(1), compiled.Netlist.GetNode(resistor.Positive));
    }

    [Fact]
    public void MultipleConnectedGroundMarkersAreAccepted()
    {
        var (circuit, voltageSource, _, resistor2) = CircuitTestFactory.CreateVoltageDivider();
        var builder = new CircuitBuilder();
        var v = builder.AddVoltageSource("V1", 10.0);
        var r1 = builder.AddResistor("R1", 1_000.0);
        var r2 = builder.AddResistor("R2", 2_000.0);

        builder.Connect(v.Negative, r2.Negative);
        builder.MarkAsGround(v.Negative);
        builder.MarkAsGround(r2.Negative);
        builder.Connect(v.Positive, r1.Positive);
        builder.Connect(r1.Negative, r2.Positive);

        var compiled = new CircuitCompiler().Compile(builder.Build());

        Assert.Equal(NodeId.Ground, compiled.Netlist.GetNode(v.Negative));
        Assert.Equal(NodeId.Ground, compiled.Netlist.GetNode(r2.Negative));
        Assert.Equal(NodeId.Ground, compiled.Netlist.GetNode(voltageSource.Negative));
        Assert.Equal(NodeId.Ground, new CircuitCompiler().Compile(circuit).Netlist.GetNode(resistor2.Negative));
    }

    [Fact]
    public void DisconnectedGroundMarkersFailCompilation()
    {
        var builder = new CircuitBuilder();
        var voltageSource = builder.AddVoltageSource("V1", 5.0);
        var resistor = builder.AddResistor("R1", 100.0);

        builder.Connect(voltageSource.Positive, resistor.Positive);
        builder.MarkAsGround(voltageSource.Negative);
        builder.MarkAsGround(resistor.Negative);

        var exception = Assert.Throws<CircuitCompilationException>(() => new CircuitCompiler().Compile(builder.Build()));

        Assert.Contains(exception.ValidationReport.Issues, issue => issue.Code == ValidationCodes.CircuitMultipleDisconnectedGrounds);
    }

    [Fact]
    public void FloatingSubnetworksFailCompilation()
    {
        var builder = new CircuitBuilder();
        var voltageSource = builder.AddVoltageSource("V1", 5.0);
        var groundedResistor = builder.AddResistor("R1", 100.0);
        var floatingResistor = builder.AddResistor("R2", 200.0);

        builder.Connect(voltageSource.Negative, groundedResistor.Negative);
        builder.Connect(voltageSource.Positive, groundedResistor.Positive);
        builder.MarkAsGround(voltageSource.Negative);
        builder.Connect(floatingResistor.Positive, floatingResistor.Negative);

        var exception = Assert.Throws<CircuitCompilationException>(() => new CircuitCompiler().Compile(builder.Build()));

        Assert.Contains(exception.ValidationReport.Issues, issue => issue.Code == ValidationCodes.NodeFloatingSubnetwork);
    }

    [Fact]
    public void NonzeroVoltageSourceSelfLoopFailsCompilation()
    {
        var builder = new CircuitBuilder();
        var voltageSource = builder.AddVoltageSource("V1", 5.0);

        builder.Connect(voltageSource.Positive, voltageSource.Negative);
        builder.MarkAsGround(voltageSource.Negative);

        var exception = Assert.Throws<CircuitCompilationException>(() => new CircuitCompiler().Compile(builder.Build()));

        Assert.Contains(exception.ValidationReport.Issues, issue => issue.Code == ValidationCodes.VoltageSourceSelfLoopNonzero);
    }

    [Fact]
    public void ResistorSelfLoopOnGroundCompilesAsZeroDimensionalWarning()
    {
        var builder = new CircuitBuilder();
        var resistor = builder.AddResistor("R1", 100.0);

        builder.Connect(resistor.Positive, resistor.Negative);
        builder.MarkAsGround(resistor.Positive);

        var compiled = new CircuitCompiler().Compile(builder.Build());

        Assert.Single(compiled.Netlist.Nodes);
        Assert.Equal(0, compiled.VariableMap.Dimension);
        Assert.Contains(compiled.ValidationReport.Issues, issue => issue.Code == ValidationCodes.ComponentSelfLoop);
    }
}
