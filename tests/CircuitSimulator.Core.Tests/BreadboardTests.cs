using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class BreadboardTests
{
    [Fact]
    public void StandardLayoutConnectsTerminalStripsAndKeepsRailsSeparate()
    {
        var layout = BreadboardLayout.Standard;

        Assert.True(layout.AreConnected(
            BreadboardHole.TerminalStrip(BreadboardRow.A, 1),
            BreadboardHole.TerminalStrip(BreadboardRow.E, 1)));
        Assert.True(layout.AreConnected(
            BreadboardHole.TerminalStrip(BreadboardRow.F, 30),
            BreadboardHole.TerminalStrip(BreadboardRow.J, 30)));
        Assert.False(layout.AreConnected(
            BreadboardHole.TerminalStrip(BreadboardRow.A, 1),
            BreadboardHole.TerminalStrip(BreadboardRow.F, 1)));
        Assert.False(layout.AreConnected(
            BreadboardHole.TerminalStrip(BreadboardRow.A, 1),
            BreadboardHole.TerminalStrip(BreadboardRow.A, 2)));

        Assert.True(layout.AreConnected(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 1),
            BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 30)));
        Assert.True(layout.AreConnected(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopNegative, 1),
            BreadboardHole.PowerRail(BreadboardPowerRail.TopNegative, 30)));
        Assert.False(layout.AreConnected(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 1),
            BreadboardHole.PowerRail(BreadboardPowerRail.TopNegative, 1)));
        Assert.False(layout.AreConnected(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 1),
            BreadboardHole.PowerRail(BreadboardPowerRail.BottomPositive, 1)));
    }

    [Fact]
    public void ConnectedHolesAreReturnedInDeterministicOrder()
    {
        var terminalStrip = BreadboardLayout.Standard.GetConnectedHoles(
            BreadboardHole.TerminalStrip(BreadboardRow.C, 7));
        var rail = BreadboardLayout.Standard.GetConnectedHoles(
            BreadboardHole.PowerRail(BreadboardPowerRail.BottomNegative, 12));

        Assert.Equal(
            new[]
            {
                BreadboardHole.TerminalStrip(BreadboardRow.A, 7),
                BreadboardHole.TerminalStrip(BreadboardRow.B, 7),
                BreadboardHole.TerminalStrip(BreadboardRow.C, 7),
                BreadboardHole.TerminalStrip(BreadboardRow.D, 7),
                BreadboardHole.TerminalStrip(BreadboardRow.E, 7)
            },
            terminalStrip);
        Assert.Equal(30, rail.Count);
        Assert.Equal(BreadboardHole.PowerRail(BreadboardPowerRail.BottomNegative, 1), rail[0]);
        Assert.Equal(BreadboardHole.PowerRail(BreadboardPowerRail.BottomNegative, 30), rail[^1]);
    }

    [Fact]
    public void InvalidCoordinatesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BreadboardLayout(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BreadboardHole.TerminalStrip(BreadboardRow.A, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BreadboardHole.TerminalStrip((BreadboardRow)99, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BreadboardHole.PowerRail((BreadboardPowerRail)99, 1));

        var layout = new BreadboardLayout(columnCount: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetNet(BreadboardHole.TerminalStrip(BreadboardRow.A, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetNet(default));
    }

    [Fact]
    public void InsertConnectsTerminalsThatShareABreadboardNet()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 5.0);
        var resistor = builder.AddResistor("R1", 1_000.0);
        var board = new Breadboard(builder);
        var firstHole = BreadboardHole.TerminalStrip(BreadboardRow.A, 1);
        var secondHole = BreadboardHole.TerminalStrip(BreadboardRow.E, 1);

        Assert.Equal(source.Positive, board.Insert(firstHole, source.Positive));
        board.Insert(secondHole, resistor.Positive);

        var circuit = builder.Build();

        Assert.Single(circuit.Wires);
        Assert.Equal(new WireDefinition(source.Positive, resistor.Positive), circuit.Wires[0]);
        Assert.True(board.TryGetInsertedTerminal(firstHole, out var inserted));
        Assert.Equal(source.Positive, inserted);
        Assert.Throws<CircuitModelException>(() => board.Insert(firstHole, resistor.Negative));
        Assert.Throws<CircuitModelException>(() => board.Insert(
            BreadboardHole.TerminalStrip(BreadboardRow.B, 1),
            source.Positive));
    }

    [Fact]
    public void InsertRejectsTerminalsNotOwnedByAssociatedBuilderWithoutOccupyingHole()
    {
        var builder = new CircuitBuilder();
        var otherBuilder = new CircuitBuilder();
        var otherResistor = otherBuilder.AddResistor("R1", 1_000.0);
        var board = new Breadboard(builder);
        var hole = BreadboardHole.TerminalStrip(BreadboardRow.A, 1);

        Assert.Throws<CircuitModelException>(() => board.Insert(hole, otherResistor.Positive));

        Assert.False(board.TryGetInsertedTerminal(hole, out _));
    }

    [Fact]
    public void ConnectedNetsCanBeJoinedBeforeOrAfterTerminalsAreInserted()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 5.0);
        var resistor = builder.AddResistor("R1", 1_000.0);
        var board = new Breadboard(builder);
        var positiveRail = BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 1);
        var positiveStrip = BreadboardHole.TerminalStrip(BreadboardRow.C, 10);
        var negativeRail = BreadboardHole.PowerRail(BreadboardPowerRail.TopNegative, 1);
        var negativeStrip = BreadboardHole.TerminalStrip(BreadboardRow.H, 10);

        board.ConnectNets(positiveRail, positiveStrip);
        board.Insert(positiveRail, source.Positive);
        board.Insert(positiveStrip, resistor.Positive);
        board.Insert(negativeRail, source.Negative);
        board.Insert(negativeStrip, resistor.Negative);
        board.ConnectNets(negativeRail, negativeStrip);
        board.MarkNetAsGround(negativeRail);

        var result = new DcOperatingPointSolver().Solve(builder.Build());

        Assert.True(board.AreConnected(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 30),
            BreadboardHole.TerminalStrip(BreadboardRow.E, 10)));
        Assert.False(board.Layout.AreConnected(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 30),
            BreadboardHole.TerminalStrip(BreadboardRow.E, 10)));
        AssertEx.NearlyEqual(5.0, result.GetComponentVoltage(resistor.ComponentId));
        AssertEx.NearlyEqual(0.005, result.GetComponentCurrent(resistor.ComponentId));
        AssertEx.NearlyEqual(-0.005, result.GetBranchCurrent(source.ComponentId));
    }

    [Fact]
    public void MarkNetAsGroundRequiresAnInsertedTerminal()
    {
        var board = new Breadboard(new CircuitBuilder());

        Assert.Throws<CircuitModelException>(() => board.MarkNetAsGround(
            BreadboardHole.PowerRail(BreadboardPowerRail.TopNegative, 1)));
    }
}
