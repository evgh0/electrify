using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Tests;

public sealed class CircuitBuilderTests
{
    [Fact]
    public void AddsSupportedComponentsWithStableIdsAndTerminalOwnership()
    {
        var builder = new CircuitBuilder();
        var resistor = builder.AddResistor("R1", 100.0);
        var currentSource = builder.AddCurrentSource("I1", 0.01);
        var voltageSource = builder.AddVoltageSource("V1", 5.0);
        var circuit = builder.Build();

        Assert.Equal(new ComponentId(0), resistor.ComponentId);
        Assert.Equal(new ComponentId(1), currentSource.ComponentId);
        Assert.Equal(new ComponentId(2), voltageSource.ComponentId);
        Assert.Equal(new TerminalId(0), resistor.Positive);
        Assert.Equal(new TerminalId(5), voltageSource.Negative);
        Assert.Equal(6, circuit.Terminals.Count);
        Assert.IsType<ResistorParameters>(circuit.GetComponent(resistor.ComponentId).Parameters);
        Assert.Equal(resistor.ComponentId, circuit.GetTerminal(resistor.Positive).OwnerComponentId);
        Assert.Equal(0, circuit.GetTerminal(resistor.Positive).LocalIndex);
        Assert.Equal(1, circuit.GetTerminal(resistor.Negative).LocalIndex);
    }

    [Fact]
    public void BuildReturnsSnapshotUnaffectedByLaterBuilderChanges()
    {
        var builder = new CircuitBuilder();
        var first = builder.AddResistor("R1", 100.0);
        builder.MarkAsGround(first.Negative);
        var snapshot = builder.Build();

        var second = builder.AddResistor("R2", 200.0);
        builder.Connect(first.Positive, second.Positive);
        var updated = builder.Build();

        Assert.Single(snapshot.Components);
        Assert.Empty(snapshot.Wires);
        Assert.Equal(2, updated.Components.Count);
        Assert.Single(updated.Wires);
    }

    [Fact]
    public void RejectsInvalidReferencesDuplicateNamesAndInvalidParameters()
    {
        var builder = new CircuitBuilder();
        builder.AddResistor("R1", 100.0);

        Assert.Throws<CircuitModelException>(() => builder.Connect(new TerminalId(99), new TerminalId(0)));
        Assert.Throws<CircuitModelException>(() => builder.MarkAsGround(new TerminalId(99)));
        Assert.Throws<CircuitModelException>(() => builder.AddVoltageSource("R1", 5.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddResistor("Bad", 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddCurrentSource("BadI", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddVoltageSource("BadV", double.PositiveInfinity));
    }

    [Fact]
    public void ConnectParamsConnectsAllTerminalsToOneNet()
    {
        var builder = new CircuitBuilder();
        var resistor1 = builder.AddResistor("R1", 100.0);
        var resistor2 = builder.AddResistor("R2", 200.0);
        var resistor3 = builder.AddResistor("R3", 300.0);

        builder.Connect(resistor1.Positive, resistor2.Positive, resistor3.Positive);
        builder.MarkAsGround(resistor1.Negative);
        var circuit = builder.Build();

        Assert.Equal(2, circuit.Wires.Count);
    }
}
