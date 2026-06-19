using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Editor.Documents;
using CircuitSimulator.Editor.Contracts.Geometry;
using CircuitSimulator.Editor.Rendering;

namespace CircuitSimulator.Editor.Tests;

public sealed class DemoCatalogTests
{
    [Fact]
    public void CatalogBuildsThreeDeterministicValidCircuits()
    {
        Assert.Equal(3, DemoCatalog.All.Count);

        foreach (var definition in DemoCatalog.All)
        {
            var first = definition.Build(definition.Defaults);
            var second = definition.Build(definition.Defaults);
            Assert.Equal(
                first.Schematic.Components.Select(component => component.ComponentId),
                second.Schematic.Components.Select(component => component.ComponentId));
            Assert.Equal(first.Circuit.Components.Count, first.Schematic.Components.Count);
            Assert.NotEmpty(first.Schematic.Wires);
            Assert.Equal(
                first.GetComponentId(definition.DefaultSelectedComponent),
                second.GetComponentId(definition.DefaultSelectedComponent));
        }
    }

    [Fact]
    public void RcAndRlPresetsMatchFirstBackwardEulerStep()
    {
        var solver = new TransientSimulationSolver();
        var rcDefinition = DemoCatalog.All.Single(definition => definition.Id == "rc-step");
        var rc = rcDefinition.Build(rcDefinition.Defaults);
        var rcResult = solver.Solve(rc.Circuit, rc.SimulationOptions);
        var rcVoltage = rcResult.Samples[0].GetComponentVoltage(rc.GetComponentId(rcDefinition.DefaultSelectedComponent));
        Assert.Equal(5.0 * 0.0005 / (0.1 + 0.0005), rcVoltage, 9);

        var rlDefinition = DemoCatalog.All.Single(definition => definition.Id == "rl-step");
        var rl = rlDefinition.Build(rlDefinition.Defaults);
        var rlResult = solver.Solve(rl.Circuit, rl.SimulationOptions);
        var rlCurrent = rlResult.Samples[0].GetComponentCurrent(rl.GetComponentId(rlDefinition.DefaultSelectedComponent));
        Assert.Equal(5.0 * 0.000005 / (0.1 + (100.0 * 0.000005)), rlCurrent, 9);
    }

    [Fact]
    public void RectifierPresetProducesFinitePositiveSmoothedOutput()
    {
        var definition = DemoCatalog.All.Single(item => item.Id == "diode-rectifier");
        var instance = definition.Build(definition.Defaults);
        var result = new TransientSimulationSolver().Solve(instance.Circuit, instance.SimulationOptions);
        var capacitorId = instance.GetComponentId(definition.DefaultSelectedComponent);
        var output = result.Samples.Select(sample => sample.GetComponentVoltage(capacitorId)).ToArray();

        Assert.InRange(result.Samples.Count, 2000, 2001);
        Assert.Equal(0.1, result.Samples[^1].Time, 12);
        Assert.All(output, value => Assert.True(double.IsFinite(value)));
        Assert.True(output.Max() > 5.0);
        Assert.True(output[^1] > 0.0);
    }

    [Fact]
    public void SchematicHitTestUsesStablePlacementKeys()
    {
        var definition = DemoCatalog.All[0];
        var instance = definition.Build(definition.Defaults);
        var placement = instance.Schematic.Components[1];

        Assert.Equal(
            placement.Key,
            SchematicHitTest.FindNearest(instance.Schematic, placement.Position, tolerance: 1.0));
        Assert.Null(SchematicHitTest.FindNearest(
            instance.Schematic,
            new Point2(10_000, 10_000),
            tolerance: 20.0));
    }
}
