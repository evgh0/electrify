using System.Collections.ObjectModel;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.RealtimeDemo.Models;

/// <summary>Describes one selectable component in the fixed realtime demonstration.</summary>
public sealed record DemoComponentDescriptor(ComponentId ComponentId, string Name, ComponentKind Kind)
{
    /// <inheritdoc />
    public override string ToString() => $"{Name} · {Kind}";
}

/// <summary>Contains the immutable predefined two-stage RC low-pass circuit.</summary>
public sealed class RealtimeDemoCircuit
{
    /// <summary>Gets the sinusoidal source frequency in hertz.</summary>
    public const double SourceFrequencyHz = 1.0;

    /// <summary>Gets the fixed integration step in seconds.</summary>
    public const double TimeStep = 5e-3;

    /// <summary>Gets the rolling chart duration in seconds.</summary>
    public const double HistoryDuration = 10.0;

    private RealtimeDemoCircuit(
        Circuit circuit,
        TwoTerminalComponentHandle source,
        TwoTerminalComponentHandle firstResistor,
        TwoTerminalComponentHandle firstCapacitor,
        TwoTerminalComponentHandle secondResistor,
        TwoTerminalComponentHandle secondCapacitor)
    {
        Circuit = circuit;
        SourceId = source.ComponentId;
        FirstResistorId = firstResistor.ComponentId;
        FirstCapacitorId = firstCapacitor.ComponentId;
        SecondResistorId = secondResistor.ComponentId;
        SecondCapacitorId = secondCapacitor.ComponentId;
        DefaultSelectedComponentId = SecondCapacitorId;
        Components = new ReadOnlyCollection<DemoComponentDescriptor>(
            circuit.Components.Select(component =>
                new DemoComponentDescriptor(component.Id, component.Name, component.Kind)).ToArray());
    }

    /// <summary>Gets the physical circuit snapshot.</summary>
    public Circuit Circuit { get; }

    /// <summary>Gets selectable components in deterministic circuit order.</summary>
    public IReadOnlyList<DemoComponentDescriptor> Components { get; }

    /// <summary>Gets the sinusoidal voltage-source identifier.</summary>
    public ComponentId SourceId { get; }

    /// <summary>Gets the first resistor identifier.</summary>
    public ComponentId FirstResistorId { get; }

    /// <summary>Gets the first shunt-capacitor identifier.</summary>
    public ComponentId FirstCapacitorId { get; }

    /// <summary>Gets the second resistor identifier.</summary>
    public ComponentId SecondResistorId { get; }

    /// <summary>Gets the second shunt-capacitor identifier.</summary>
    public ComponentId SecondCapacitorId { get; }

    /// <summary>Gets the component selected when the application opens.</summary>
    public ComponentId DefaultSelectedComponentId { get; }

    /// <summary>Builds the predefined 1 Hz two-stage RC low-pass circuit.</summary>
    public static RealtimeDemoCircuit Create()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddSinusoidalVoltageSource(
            "V1",
            offset: 0.0,
            amplitude: 5.0,
            frequencyHz: SourceFrequencyHz);
        var firstResistor = builder.AddResistor("R1", 1_000.0);
        var firstCapacitor = builder.AddCapacitor("C1", 100e-6);
        var secondResistor = builder.AddResistor("R2", 1_000.0);
        var secondCapacitor = builder.AddCapacitor("C2", 220e-6);

        builder.Connect(source.Negative, firstCapacitor.Negative, secondCapacitor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, firstResistor.Positive);
        builder.Connect(firstResistor.Negative, firstCapacitor.Positive, secondResistor.Positive);
        builder.Connect(secondResistor.Negative, secondCapacitor.Positive);

        return new RealtimeDemoCircuit(
            builder.Build(),
            source,
            firstResistor,
            firstCapacitor,
            secondResistor,
            secondCapacitor);
    }

    /// <summary>Gets a selectable descriptor by typed component identifier.</summary>
    public DemoComponentDescriptor GetComponent(ComponentId componentId) =>
        Components.FirstOrDefault(component => component.ComponentId == componentId)
        ?? throw new KeyNotFoundException($"The realtime demo has no component {componentId}.");
}
