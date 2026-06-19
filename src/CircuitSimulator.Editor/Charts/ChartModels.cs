using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Results;
using CircuitSimulator.Editor.Contracts.Documents;
using CircuitSimulator.Editor.Documents;

namespace CircuitSimulator.Editor.Charts;

/// <summary>Identifies a physical quantity chart lane.</summary>
public enum ChartLaneKind
{
    /// <summary>Component voltage.</summary>
    Voltage,
    /// <summary>Positive-to-negative component current.</summary>
    Current,
    /// <summary>Instantaneous component power.</summary>
    Power
}

/// <summary>One scalar time-series point.</summary>
public readonly record struct ChartPoint(double Time, double Value);

/// <summary>One named chart series.</summary>
public sealed class ChartSeries
{
    /// <summary>Initializes an immutable chart series.</summary>
    public ChartSeries(string name, string unit, IEnumerable<ChartPoint> points, bool isReference = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(points);
        Name = name;
        Unit = unit;
        Points = new ReadOnlyCollection<ChartPoint>(points.ToArray());
        IsReference = isReference;
    }

    /// <summary>Gets the series display name.</summary>
    public string Name { get; }

    /// <summary>Gets its SI unit.</summary>
    public string Unit { get; }

    /// <summary>Gets time-series points.</summary>
    public IReadOnlyList<ChartPoint> Points { get; }

    /// <summary>Gets whether this is a dashed contextual reference.</summary>
    public bool IsReference { get; }
}

/// <summary>One independently scaled waveform chart lane.</summary>
public sealed class ChartLane
{
    /// <summary>Initializes a chart lane and calculates a padded finite range.</summary>
    public ChartLane(ChartLaneKind kind, string unit, IEnumerable<ChartSeries> series)
    {
        Kind = kind;
        Unit = unit;
        Series = new ReadOnlyCollection<ChartSeries>(series.ToArray());
        var values = Series.SelectMany(item => item.Points).Select(point => point.Value).ToArray();
        var minimum = values.Length == 0 ? -1.0 : values.Min();
        var maximum = values.Length == 0 ? 1.0 : values.Max();
        if (Math.Abs(maximum - minimum) < 1e-15)
        {
            var padding = Math.Max(1.0, Math.Abs(maximum) * 0.1);
            Minimum = minimum - padding;
            Maximum = maximum + padding;
        }
        else
        {
            var padding = (maximum - minimum) * 0.08;
            Minimum = minimum - padding;
            Maximum = maximum + padding;
        }
    }

    /// <summary>Gets the quantity kind.</summary>
    public ChartLaneKind Kind { get; }

    /// <summary>Gets the lane unit.</summary>
    public string Unit { get; }

    /// <summary>Gets series rendered in this lane.</summary>
    public IReadOnlyList<ChartSeries> Series { get; }

    /// <summary>Gets the padded lower range.</summary>
    public double Minimum { get; }

    /// <summary>Gets the padded upper range.</summary>
    public double Maximum { get; }
}

/// <summary>Values at a shared chart cursor sample.</summary>
public sealed record ChartCursorSample(double Time, double Voltage, double Current, double Power, int SampleIndex);

/// <summary>Prepared selected-component data for the custom waveform control.</summary>
public sealed class TransientChartData
{
    private readonly TransientSimulationResult _result;
    private readonly ComponentId _selectedComponentId;

    private TransientChartData(
        DemoComponentKey selectedComponent,
        string selectedComponentName,
        TransientSimulationResult result,
        ComponentId selectedComponentId,
        IReadOnlyList<ChartLane> lanes)
    {
        SelectedComponent = selectedComponent;
        SelectedComponentName = selectedComponentName;
        _result = result;
        _selectedComponentId = selectedComponentId;
        Lanes = lanes;
        StartTime = result.Samples.Count == 0 ? 0.0 : result.Samples[0].Time;
        StopTime = result.Samples.Count == 0 ? 1.0 : result.Samples[^1].Time;
    }

    /// <summary>Gets the selected logical component.</summary>
    public DemoComponentKey SelectedComponent { get; }

    /// <summary>Gets the selected component name.</summary>
    public string SelectedComponentName { get; }

    /// <summary>Gets chart lanes in voltage/current/power order.</summary>
    public IReadOnlyList<ChartLane> Lanes { get; }

    /// <summary>Gets the first plotted sample time.</summary>
    public double StartTime { get; }

    /// <summary>Gets the last plotted sample time.</summary>
    public double StopTime { get; }

    /// <summary>Builds chart data from an immutable transient result.</summary>
    public static TransientChartData Build(
        DemoCircuitInstance instance,
        TransientSimulationResult result,
        DemoComponentKey selectedComponent)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(result);
        var selectedId = instance.GetComponentId(selectedComponent);
        var selectedName = instance.Circuit.GetComponent(selectedId).Name;
        var voltage = result.Samples.Select(sample =>
            new ChartPoint(sample.Time, sample.GetComponentVoltage(selectedId))).ToArray();
        var current = result.Samples.Select(sample =>
            new ChartPoint(sample.Time, sample.GetComponentCurrent(selectedId))).ToArray();
        var power = voltage.Zip(current, (v, i) => new ChartPoint(v.Time, v.Value * i.Value)).ToArray();
        var voltageSeries = new List<ChartSeries>
        {
            new($"V({selectedName})", "V", voltage)
        };

        if (instance.ReferenceVoltageComponent != selectedComponent)
        {
            var referenceId = instance.GetComponentId(instance.ReferenceVoltageComponent);
            var referenceName = instance.Circuit.GetComponent(referenceId).Name;
            voltageSeries.Add(new ChartSeries(
                $"V({referenceName})",
                "V",
                result.Samples.Select(sample =>
                    new ChartPoint(sample.Time, sample.GetComponentVoltage(referenceId))),
                isReference: true));
        }

        var lanes = new ReadOnlyCollection<ChartLane>(
        [
            new ChartLane(ChartLaneKind.Voltage, "V", voltageSeries),
            new ChartLane(ChartLaneKind.Current, "A", [new ChartSeries($"I({selectedName})", "A", current)]),
            new ChartLane(ChartLaneKind.Power, "W", [new ChartSeries($"P({selectedName})", "W", power)])
        ]);
        return new TransientChartData(selectedComponent, selectedName, result, selectedId, lanes);
    }

    /// <summary>Returns values at the sample nearest a requested time.</summary>
    public ChartCursorSample FindNearest(double time)
    {
        if (_result.Samples.Count == 0)
        {
            throw new InvalidOperationException("Chart data contains no samples.");
        }

        var low = 0;
        var high = _result.Samples.Count - 1;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (_result.Samples[middle].Time < time)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        var index = low;
        if (index > 0 &&
            Math.Abs(_result.Samples[index - 1].Time - time) <= Math.Abs(_result.Samples[index].Time - time))
        {
            index--;
        }

        var sample = _result.Samples[index];
        var voltage = sample.GetComponentVoltage(_selectedComponentId);
        var current = sample.GetComponentCurrent(_selectedComponentId);
        return new ChartCursorSample(sample.Time, voltage, current, voltage * current, index);
    }

    /// <summary>Reduces dense points to ordered per-pixel extrema while retaining endpoints.</summary>
    public static IReadOnlyList<ChartPoint> Decimate(IReadOnlyList<ChartPoint> points, int pixelWidth)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth));
        }

        if (points.Count <= pixelWidth * 2)
        {
            return points.ToArray();
        }

        var reduced = new List<ChartPoint>(pixelWidth * 2 + 2) { points[0] };
        var bucketSize = (double)(points.Count - 2) / pixelWidth;
        for (var bucket = 0; bucket < pixelWidth; bucket++)
        {
            var start = 1 + (int)Math.Floor(bucket * bucketSize);
            var end = Math.Min(points.Count - 1, 1 + (int)Math.Floor((bucket + 1) * bucketSize));
            if (end <= start)
            {
                continue;
            }

            var minIndex = start;
            var maxIndex = start;
            for (var index = start + 1; index < end; index++)
            {
                if (points[index].Value < points[minIndex].Value)
                {
                    minIndex = index;
                }

                if (points[index].Value > points[maxIndex].Value)
                {
                    maxIndex = index;
                }
            }

            if (minIndex <= maxIndex)
            {
                reduced.Add(points[minIndex]);
                if (maxIndex != minIndex)
                {
                    reduced.Add(points[maxIndex]);
                }
            }
            else
            {
                reduced.Add(points[maxIndex]);
                reduced.Add(points[minIndex]);
            }
        }

        reduced.Add(points[^1]);
        return reduced;
    }
}
