using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Tests.Support;

internal static class CircuitTestFactory
{
    public static (
        Circuit Circuit,
        TwoTerminalComponentHandle VoltageSource,
        TwoTerminalComponentHandle Resistor1,
        TwoTerminalComponentHandle Resistor2) CreateVoltageDivider()
    {
        var builder = new CircuitBuilder();
        var voltageSource = builder.AddVoltageSource("V1", 10.0);
        var resistor1 = builder.AddResistor("R1", 1_000.0);
        var resistor2 = builder.AddResistor("R2", 2_000.0);

        builder.Connect(voltageSource.Negative, resistor2.Negative);
        builder.MarkAsGround(voltageSource.Negative);
        builder.Connect(voltageSource.Positive, resistor1.Positive);
        builder.Connect(resistor1.Negative, resistor2.Positive);

        return (builder.Build(), voltageSource, resistor1, resistor2);
    }
}
