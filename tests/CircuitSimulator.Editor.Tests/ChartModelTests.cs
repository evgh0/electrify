using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Editor.Charts;
using CircuitSimulator.Editor.Documents;

namespace CircuitSimulator.Editor.Tests;

public sealed class ChartModelTests
{
    [Fact]
    public void BuildsVoltageCurrentAndPowerLanesWithNearestCursor()
    {
        var definition = DemoCatalog.All[0];
        var instance = definition.Build(definition.Defaults);
        var result = new TransientSimulationSolver().Solve(instance.Circuit, instance.SimulationOptions);
        var chart = TransientChartData.Build(instance, result, definition.DefaultSelectedComponent);

        Assert.Equal(new[] { ChartLaneKind.Voltage, ChartLaneKind.Current, ChartLaneKind.Power },
            chart.Lanes.Select(lane => lane.Kind));
        Assert.Equal(2, chart.Lanes[0].Series.Count);
        Assert.True(chart.Lanes[0].Series[1].IsReference);
        var cursor = chart.FindNearest(result.Samples[10].Time + 1e-12);
        Assert.Equal(10, cursor.SampleIndex);
        Assert.Equal(cursor.Voltage * cursor.Current, cursor.Power, 12);
    }

    [Fact]
    public void LaneRangeHandlesConstantZeroAndDecimationRetainsExtrema()
    {
        var lane = new ChartLane(
            ChartLaneKind.Current,
            "A",
            [new ChartSeries("zero", "A", [new ChartPoint(0, 0), new ChartPoint(1, 0)])]);
        Assert.True(lane.Minimum < 0.0);
        Assert.True(lane.Maximum > 0.0);

        var points = Enumerable.Range(0, 1000)
            .Select(index => new ChartPoint(index, index == 500 ? 100.0 : Math.Sin(index)))
            .ToArray();
        var reduced = TransientChartData.Decimate(points, 40);
        Assert.Equal(points[0], reduced[0]);
        Assert.Equal(points[^1], reduced[^1]);
        Assert.Contains(reduced, point => point.Value == 100.0);
        Assert.True(reduced.Count <= 82);
    }
}
