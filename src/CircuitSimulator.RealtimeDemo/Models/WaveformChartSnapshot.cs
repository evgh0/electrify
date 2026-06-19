using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Results;

namespace CircuitSimulator.RealtimeDemo.Models;

/// <summary>One selected-component voltage, current, and power point.</summary>
public readonly record struct WaveformPoint(double Time, double Voltage, double Current, double Power);

/// <summary>Immutable rolling waveform data prepared for one selected component.</summary>
public sealed class WaveformChartSnapshot
{
    private WaveformChartSnapshot(
        DemoComponentDescriptor component,
        double startTime,
        double stopTime,
        IReadOnlyList<WaveformPoint> points)
    {
        Component = component;
        StartTime = startTime;
        StopTime = stopTime;
        Points = points;
    }

    /// <summary>Gets the selected component.</summary>
    public DemoComponentDescriptor Component { get; }

    /// <summary>Gets the left edge of the fixed-width chart window.</summary>
    public double StartTime { get; }

    /// <summary>Gets the right edge of the fixed-width chart window.</summary>
    public double StopTime { get; }

    /// <summary>Gets waveform points in ascending simulation-time order.</summary>
    public IReadOnlyList<WaveformPoint> Points { get; }

    /// <summary>Gets the latest retained measurement when available.</summary>
    public WaveformPoint? Latest => Points.Count == 0 ? null : Points[^1];

    /// <summary>Builds a selected-component snapshot from immutable transient samples.</summary>
    public static WaveformChartSnapshot Create(
        RealtimeDemoCircuit circuit,
        ComponentId selectedComponentId,
        IReadOnlyList<TransientSample> samples,
        double duration)
    {
        ArgumentNullException.ThrowIfNull(circuit);
        ArgumentNullException.ThrowIfNull(samples);
        if (!double.IsFinite(duration) || duration <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be finite and greater than zero.");
        }

        var component = circuit.GetComponent(selectedComponentId);
        var points = new WaveformPoint[samples.Count];
        for (var index = 0; index < samples.Count; index++)
        {
            var voltage = samples[index].GetComponentVoltage(selectedComponentId);
            var current = samples[index].GetComponentCurrent(selectedComponentId);
            var power = voltage * current;
            if (!double.IsFinite(voltage) || !double.IsFinite(current) || !double.IsFinite(power))
            {
                throw new InvalidOperationException(
                    $"Component {component.Name} produced a non-finite chart measurement at {samples[index].Time:R} s.");
            }

            points[index] = new WaveformPoint(samples[index].Time, voltage, current, power);
        }

        var latestTime = points.Length == 0 ? 0.0 : points[^1].Time;
        var startTime = Math.Max(0.0, latestTime - duration);
        return new WaveformChartSnapshot(
            component,
            startTime,
            startTime + duration,
            new ReadOnlyCollection<WaveformPoint>(points));
    }
}
