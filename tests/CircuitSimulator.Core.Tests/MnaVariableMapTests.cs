using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class MnaVariableMapTests
{
    [Fact]
    public void AllocatesNodeVoltagesBeforeBranchCurrentsDeterministically()
    {
        var (circuit, voltageSource, resistor1, _) = CircuitTestFactory.CreateVoltageDivider();
        var compiled = new CircuitCompiler().Compile(circuit);
        var map = compiled.VariableMap;

        Assert.Null(map.GetNodeVoltageIndex(NodeId.Ground));
        Assert.Equal(new VariableIndex(0), map.GetNodeVoltageIndex(new NodeId(1)));
        Assert.Equal(new VariableIndex(1), map.GetNodeVoltageIndex(new NodeId(2)));
        Assert.Equal(new VariableIndex(2), map.GetBranchCurrentIndex(voltageSource.ComponentId));
        Assert.Equal(3, map.Dimension);
        Assert.Equal(
            new[] { MnaVariableKind.NodeVoltage, MnaVariableKind.NodeVoltage, MnaVariableKind.BranchCurrent },
            map.Variables.Select(variable => variable.Kind).ToArray());
        Assert.Throws<MnaAssemblyException>(() => map.GetBranchCurrentIndex(resistor1.ComponentId));
    }

    [Fact]
    public void VoltageSourceBranchVariablesAreOrderedByComponentId()
    {
        var nodes = new[]
        {
            new ElectricalNode(NodeId.Ground, isGround: true, [new TerminalId(0)]),
            new ElectricalNode(new NodeId(1), isGround: false, [new TerminalId(1)])
        };
        var components = new[]
        {
            new CompiledComponent(new ComponentId(3), "V3", new VoltageSourceParameters(3.0), [new NodeId(1), NodeId.Ground]),
            new CompiledComponent(new ComponentId(1), "V1", new VoltageSourceParameters(1.0), [new NodeId(1), NodeId.Ground]),
            new CompiledComponent(new ComponentId(2), "R1", new ResistorParameters(1.0), [new NodeId(1), NodeId.Ground])
        };

        var map = MnaVariableMap.Create(nodes, components);

        Assert.Equal(new VariableIndex(1), map.GetBranchCurrentIndex(new ComponentId(1)));
        Assert.Equal(new VariableIndex(2), map.GetBranchCurrentIndex(new ComponentId(3)));
    }
}
