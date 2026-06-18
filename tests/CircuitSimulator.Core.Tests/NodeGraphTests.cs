using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class NodeGraphTests
{
    [Fact]
    public void ParallelComponentsRemainDistinctAndIncidentLookupIsCorrect()
    {
        var builder = new CircuitBuilder();
        var voltageSource = builder.AddVoltageSource("V1", 5.0);
        var resistor1 = builder.AddResistor("R1", 1_000.0);
        var resistor2 = builder.AddResistor("R2", 2_000.0);

        builder.Connect(voltageSource.Negative, resistor1.Negative, resistor2.Negative);
        builder.MarkAsGround(voltageSource.Negative);
        builder.Connect(voltageSource.Positive, resistor1.Positive, resistor2.Positive);

        var compiled = new CircuitCompiler().Compile(builder.Build());
        var incident = compiled.Graph.GetIncidentComponents(new NodeId(1));

        Assert.Equal(3, compiled.Graph.Edges.Count);
        Assert.Contains(resistor1.ComponentId, incident);
        Assert.Contains(resistor2.ComponentId, incident);
        Assert.Contains(voltageSource.ComponentId, incident);
    }

    [Fact]
    public void ConnectedComponentDiscoveryIncludesIsolatedNodes()
    {
        var nodes = new[]
        {
            new ElectricalNode(NodeId.Ground, isGround: true, [new TerminalId(0)]),
            new ElectricalNode(new NodeId(1), isGround: false, [new TerminalId(1)]),
            new ElectricalNode(new NodeId(2), isGround: false, [new TerminalId(2)])
        };
        var edges = new[]
        {
            new ComponentGraphEdge(new ComponentId(0), [NodeId.Ground, new NodeId(1)])
        };
        var graph = new NodeGraph(nodes, edges);

        var connected = graph.GetConnectedComponents();

        Assert.Equal(2, connected.Count);
        Assert.Equal(new[] { NodeId.Ground, new NodeId(1) }, connected[0]);
        Assert.Equal(new[] { new NodeId(2) }, connected[1]);
    }

    [Fact]
    public void ComponentNodeOrderingPreservesPolarity()
    {
        var (circuit, voltageSource, _, _) = CircuitTestFactory.CreateVoltageDivider();
        var compiled = new CircuitCompiler().Compile(circuit);
        var edge = compiled.Graph.Edges.Single(edge => edge.ComponentId == voltageSource.ComponentId);

        Assert.Equal(new NodeId(1), edge.IncidentNodes[0]);
        Assert.Equal(NodeId.Ground, edge.IncidentNodes[1]);
    }
}
