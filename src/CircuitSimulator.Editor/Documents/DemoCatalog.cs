using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Editor.Contracts.Documents;
using CircuitSimulator.Editor.Contracts.Geometry;

namespace CircuitSimulator.Editor.Documents;

/// <summary>Provides the built-in deterministic transient demonstrations.</summary>
public static class DemoCatalog
{
    /// <summary>Gets all built-in demonstrations in navigation order.</summary>
    public static IReadOnlyList<DemoCircuitDefinition> All { get; } =
        new ReadOnlyCollection<DemoCircuitDefinition>([CreateRc(), CreateRl(), CreateRectifier()]);

    private static DemoCircuitDefinition CreateRc()
    {
        var sourceKey = Key("V1");
        var resistorKey = Key("R1");
        var capacitorKey = Key("C1");
        return new DemoCircuitDefinition(
            "rc-step",
            "RC Charging",
            "A 5 V step charges a capacitor through a resistor.",
            [
                Parameter("v1.voltage", sourceKey, "Source voltage", "V", 5.0),
                Parameter("r1.resistance", resistorKey, "Resistance", "Ω", 1_000.0, 0.0),
                Parameter("c1.capacitance", capacitorKey, "Capacitance", "F", 100e-6, 0.0),
                Parameter("run.stop", null, "Stop time", "s", 0.5, 0.0),
                Parameter("run.step", null, "Time step", "s", 0.5e-3, 0.0)
            ],
            capacitorKey,
            (definition, values) =>
            {
                var builder = new CircuitBuilder();
                var source = builder.AddVoltageSource("V1", values["v1.voltage"]);
                var resistor = builder.AddResistor("R1", values["r1.resistance"]);
                var capacitor = builder.AddCapacitor("C1", values["c1.capacitance"]);
                builder.Connect(source.Negative, capacitor.Negative);
                builder.MarkAsGround(source.Negative);
                builder.Connect(source.Positive, resistor.Positive);
                builder.Connect(resistor.Negative, capacitor.Positive);
                var ids = Ids((sourceKey, source), (resistorKey, resistor), (capacitorKey, capacitor));
                var schematic = new DemoSchematic(
                    [
                        Place(sourceKey, source, -180, 0, ComponentOrientation.Vertical),
                        Place(resistorKey, resistor, 0, -100),
                        Place(capacitorKey, capacitor, 180, 0, ComponentOrientation.Vertical)
                    ],
                    [
                        Wire((-180, -50), (-180, -100), (-50, -100)),
                        Wire((50, -100), (180, -100), (180, -50)),
                        Wire((-180, 50), (-180, 100), (180, 100), (180, 50)),
                        Wire((0, 100), (0, 130))
                    ],
                    P(0, 130));
                return Instance(definition, values, builder, schematic, ids, sourceKey);
            });
    }

    private static DemoCircuitDefinition CreateRl()
    {
        var sourceKey = Key("V1");
        var resistorKey = Key("R1");
        var inductorKey = Key("L1");
        return new DemoCircuitDefinition(
            "rl-step",
            "RL Response",
            "A 5 V step establishes current through an inductor.",
            [
                Parameter("v1.voltage", sourceKey, "Source voltage", "V", 5.0),
                Parameter("r1.resistance", resistorKey, "Resistance", "Ω", 100.0, 0.0),
                Parameter("l1.inductance", inductorKey, "Inductance", "H", 100e-3, 0.0),
                Parameter("run.stop", null, "Stop time", "s", 5e-3, 0.0),
                Parameter("run.step", null, "Time step", "s", 5e-6, 0.0)
            ],
            inductorKey,
            (definition, values) =>
            {
                var builder = new CircuitBuilder();
                var source = builder.AddVoltageSource("V1", values["v1.voltage"]);
                var resistor = builder.AddResistor("R1", values["r1.resistance"]);
                var inductor = builder.AddInductor("L1", values["l1.inductance"]);
                builder.Connect(source.Negative, inductor.Negative);
                builder.MarkAsGround(source.Negative);
                builder.Connect(source.Positive, resistor.Positive);
                builder.Connect(resistor.Negative, inductor.Positive);
                var ids = Ids((sourceKey, source), (resistorKey, resistor), (inductorKey, inductor));
                var schematic = new DemoSchematic(
                    [
                        Place(sourceKey, source, -180, 0, ComponentOrientation.Vertical),
                        Place(resistorKey, resistor, 0, -100),
                        Place(inductorKey, inductor, 180, 0, ComponentOrientation.Vertical)
                    ],
                    [
                        Wire((-180, -50), (-180, -100), (-50, -100)),
                        Wire((50, -100), (180, -100), (180, -50)),
                        Wire((-180, 50), (-180, 100), (180, 100), (180, 50)),
                        Wire((0, 100), (0, 130))
                    ],
                    P(0, 130));
                return Instance(definition, values, builder, schematic, ids, sourceKey);
            });
    }

    private static DemoCircuitDefinition CreateRectifier()
    {
        var sourceKey = Key("V1");
        var diodeKey = Key("D1");
        var capacitorKey = Key("C1");
        var loadKey = Key("Rload");
        return new DemoCircuitDefinition(
            "diode-rectifier",
            "Diode Rectifier",
            "A nonlinear half-wave rectifier charges a smoothing capacitor.",
            [
                Parameter("v1.amplitude", sourceKey, "Peak amplitude", "V", 10.0, 0.0),
                Parameter("v1.frequency", sourceKey, "Frequency", "Hz", 50.0, 0.0),
                Parameter("d1.saturation", diodeKey, "Saturation current", "A", 1e-12, 0.0),
                Parameter("c1.capacitance", capacitorKey, "Capacitance", "F", 100e-6, 0.0),
                Parameter("rload.resistance", loadKey, "Load resistance", "Ω", 1_000.0, 0.0),
                Parameter("run.stop", null, "Stop time", "s", 0.1, 0.0),
                Parameter("run.step", null, "Time step", "s", 50e-6, 0.0)
            ],
            capacitorKey,
            (definition, values) =>
            {
                var builder = new CircuitBuilder();
                var source = builder.AddSinusoidalVoltageSource(
                    "V1",
                    0.0,
                    values["v1.amplitude"],
                    values["v1.frequency"]);
                var diode = builder.AddDiode("D1", values["d1.saturation"]);
                var capacitor = builder.AddCapacitor("C1", values["c1.capacitance"]);
                var load = builder.AddResistor("Rload", values["rload.resistance"]);
                builder.Connect(source.Negative, capacitor.Negative, load.Negative);
                builder.MarkAsGround(source.Negative);
                builder.Connect(source.Positive, diode.Positive);
                builder.Connect(diode.Negative, capacitor.Positive, load.Positive);
                var ids = Ids(
                    (sourceKey, source),
                    (diodeKey, diode),
                    (capacitorKey, capacitor),
                    (loadKey, load));
                var schematic = new DemoSchematic(
                    [
                        Place(sourceKey, source, -220, 20, ComponentOrientation.Vertical),
                        Place(diodeKey, diode, -70, -100),
                        Place(capacitorKey, capacitor, 100, 20, ComponentOrientation.Vertical),
                        Place(loadKey, load, 230, 20, ComponentOrientation.Vertical)
                    ],
                    [
                        Wire((-220, -30), (-220, -100), (-120, -100)),
                        Wire((-20, -100), (100, -100), (100, -30)),
                        Wire((100, -100), (230, -100), (230, -30)),
                        Wire((-220, 70), (-220, 120), (230, 120), (230, 70)),
                        Wire((100, 70), (100, 120)),
                        Wire((0, 120), (0, 150))
                    ],
                    P(0, 150));
                return Instance(definition, values, builder, schematic, ids, sourceKey);
            });
    }

    private static DemoCircuitInstance Instance(
        DemoCircuitDefinition definition,
        DemoParameterValues values,
        CircuitBuilder builder,
        DemoSchematic schematic,
        IReadOnlyDictionary<DemoComponentKey, ComponentId> ids,
        DemoComponentKey reference) =>
        new(
            definition,
            values,
            builder.Build(),
            schematic,
            ids,
            new TransientSimulationOptions(0.0, values["run.stop"], values["run.step"]),
            reference);

    private static DemoParameterDefinition Parameter(
        string key,
        DemoComponentKey? owner,
        string name,
        string unit,
        double value,
        double minimumExclusive = double.NegativeInfinity) =>
        new(key, owner, name, unit, value, minimumExclusive);

    private static DemoComponentKey Key(string value) => new(value);

    private static SchematicComponentPlacement Place(
        DemoComponentKey key,
        TwoTerminalComponentHandle handle,
        double x,
        double y,
        ComponentOrientation orientation = ComponentOrientation.Horizontal) =>
        new(key, handle.ComponentId, P(x, y), orientation);

    private static IReadOnlyDictionary<DemoComponentKey, ComponentId> Ids(
        params (DemoComponentKey Key, TwoTerminalComponentHandle Handle)[] components) =>
        components.ToDictionary(component => component.Key, component => component.Handle.ComponentId);

    private static SchematicWirePath Wire(params (double X, double Y)[] points) =>
        new(points.Select(point => P(point.X, point.Y)));

    private static Point2 P(double x, double y) => new(x, y);
}
