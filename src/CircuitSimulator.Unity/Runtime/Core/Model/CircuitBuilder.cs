#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Components;

namespace CircuitSimulator.Core.Model
{

/// <summary>
/// Builds immutable circuit snapshots with deterministic component and terminal identifiers.
/// </summary>
/// <remarks>
/// All two-terminal components use terminal 0 as positive/reference and terminal 1 as negative.
/// Self-wires are treated as no-ops, and duplicate wires are accepted because topology compilation collapses them deterministically.
/// </remarks>
internal sealed class CircuitBuilder
{
    private readonly List<ComponentDefinition> _components = new();
    private readonly List<TerminalDefinition> _terminals = new();
    private readonly List<WireDefinition> _wires = new();
    private readonly HashSet<TerminalId> _groundTerminals = new();
    private readonly HashSet<string> _componentNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds a two-terminal resistor.
    /// </summary>
    /// <param name="name">The unique component name.</param>
    /// <param name="resistance">The finite, strictly positive resistance in ohms.</param>
    /// <returns>A handle containing the component identifier and ordered terminals.</returns>
    public TwoTerminalComponentHandle AddResistor(string name, double resistance) =>
        AddTwoTerminalComponent(name, new ResistorParameters(resistance));

    /// <summary>
    /// Adds a two-terminal independent current source.
    /// </summary>
    /// <param name="name">The unique component name.</param>
    /// <param name="current">The finite current in amperes, positive from positive terminal to negative terminal.</param>
    /// <returns>A handle containing the component identifier and ordered terminals.</returns>
    public TwoTerminalComponentHandle AddCurrentSource(string name, double current) =>
        AddTwoTerminalComponent(name, new CurrentSourceParameters(current));

    /// <summary>
    /// Adds a two-terminal independent voltage source.
    /// </summary>
    /// <param name="name">The unique component name.</param>
    /// <param name="voltage">The finite voltage in volts, positive terminal minus negative terminal.</param>
    /// <returns>A handle containing the component identifier and ordered terminals.</returns>
    public TwoTerminalComponentHandle AddVoltageSource(string name, double voltage) =>
        AddTwoTerminalComponent(name, new VoltageSourceParameters(voltage));

    /// <summary>Adds an independent sinusoidal voltage source.</summary>
    public TwoTerminalComponentHandle AddSinusoidalVoltageSource(
        string name,
        double offset,
        double amplitude,
        double frequencyHz,
        double phaseRadians = 0.0) =>
        AddTwoTerminalComponent(
            name,
            new VoltageSourceParameters(
                new SinusoidalSourceWaveform(offset, amplitude, frequencyHz, phaseRadians)));

    /// <summary>Adds an independent sinusoidal current source.</summary>
    public TwoTerminalComponentHandle AddSinusoidalCurrentSource(
        string name,
        double offset,
        double amplitude,
        double frequencyHz,
        double phaseRadians = 0.0) =>
        AddTwoTerminalComponent(
            name,
            new CurrentSourceParameters(
                new SinusoidalSourceWaveform(offset, amplitude, frequencyHz, phaseRadians)));

    /// <summary>Adds a two-terminal capacitor.</summary>
    public TwoTerminalComponentHandle AddCapacitor(string name, double capacitance) =>
        AddTwoTerminalComponent(name, new CapacitorParameters(capacitance));

    /// <summary>Adds a two-terminal inductor.</summary>
    public TwoTerminalComponentHandle AddInductor(string name, double inductance) =>
        AddTwoTerminalComponent(name, new InductorParameters(inductance));

    /// <summary>Adds a two-terminal Shockley diode.</summary>
    public TwoTerminalComponentHandle AddDiode(
        string name,
        double saturationCurrent = 1e-12,
        double idealityFactor = 1.0,
        double thermalVoltage = DiodeParameters.DefaultThermalVoltage) =>
        AddTwoTerminalComponent(
            name,
            new DiodeParameters(saturationCurrent, idealityFactor, thermalVoltage),
            "A",
            "K");

    /// <summary>Adds a two-terminal LED modeled as a Shockley diode fitted to a nominal forward operating point.</summary>
    public TwoTerminalComponentHandle AddLed(
        string name,
        double nominalForwardVoltage = LedParameters.DefaultNominalForwardVoltage,
        double referenceCurrent = LedParameters.DefaultReferenceCurrent,
        double idealityFactor = LedParameters.DefaultIdealityFactor,
        double thermalVoltage = DiodeParameters.DefaultThermalVoltage) =>
        AddTwoTerminalComponent(
            name,
            new LedParameters(nominalForwardVoltage, referenceCurrent, idealityFactor, thermalVoltage),
            "A",
            "K");

    /// <summary>Adds a controllable ideal two-terminal switch.</summary>
    /// <param name="name">The unique component name.</param>
    /// <param name="initiallyClosed">
    /// <see langword="true"/> to start as an ideal short; <see langword="false"/> to start open.
    /// </param>
    /// <returns>A handle containing the component identifier and ordered terminals.</returns>
    public TwoTerminalComponentHandle AddSwitch(string name, bool initiallyClosed = false) =>
        AddTwoTerminalComponent(name, new SwitchParameters(initiallyClosed));

    /// <summary>
    /// Connects two terminals with an ideal wire.
    /// </summary>
    /// <param name="first">The first terminal.</param>
    /// <param name="second">The second terminal.</param>
    /// <exception cref="CircuitModelException">Thrown when either terminal is not owned by this builder.</exception>
    public void Connect(TerminalId first, TerminalId second)
    {
        EnsureTerminalExists(first);
        EnsureTerminalExists(second);

        if (first == second)
        {
            return;
        }

        _wires.Add(new WireDefinition(first, second));
    }

    /// <summary>
    /// Connects a set of terminals onto the same ideal net.
    /// </summary>
    /// <param name="terminals">The terminals to connect.</param>
    /// <exception cref="CircuitModelException">Thrown when any terminal is not owned by this builder.</exception>
    public void Connect(params TerminalId[] terminals)
    {
        Guard.NotNull(terminals, nameof(terminals));

        if (terminals.Length < 2)
        {
            return;
        }

        var first = terminals[0];
        EnsureTerminalExists(first);

        for (var index = 1; index < terminals.Length; index++)
        {
            Connect(first, terminals[index]);
        }
    }

    /// <summary>
    /// Marks a terminal as the circuit ground reference.
    /// </summary>
    /// <param name="terminal">The terminal to mark as ground.</param>
    /// <exception cref="CircuitModelException">Thrown when the terminal is not owned by this builder.</exception>
    public void MarkAsGround(TerminalId terminal)
    {
        EnsureTerminalExists(terminal);
        _groundTerminals.Add(terminal);
    }

    /// <summary>
    /// Builds an immutable circuit snapshot that is unaffected by later builder mutations.
    /// </summary>
    /// <returns>The immutable circuit snapshot.</returns>
    public Circuit Build() =>
        new(_components, _terminals, _wires, _groundTerminals);

    private TwoTerminalComponentHandle AddTwoTerminalComponent(
        string name,
        IComponentParameters parameters,
        string positiveName = "+",
        string negativeName = "-")
    {
        Guard.NotNull(name, nameof(name));
        Guard.NotNull(parameters, nameof(parameters));

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Component name cannot be empty or whitespace.", nameof(name));
        }

        if (!_componentNames.Add(name))
        {
            throw new CircuitModelException($"A component named '{name}' already exists.");
        }

        var componentId = new ComponentId(_components.Count);
        var positive = new TerminalId(_terminals.Count);
        var negative = new TerminalId(_terminals.Count + 1);

        _terminals.Add(new TerminalDefinition(positive, componentId, 0, positiveName));
        _terminals.Add(new TerminalDefinition(negative, componentId, 1, negativeName));
        _components.Add(new ComponentDefinition(
            componentId,
            name,
            new[] { positive, negative },
            parameters));

        return new TwoTerminalComponentHandle(componentId, positive, negative);
    }

    private void EnsureTerminalExists(TerminalId terminal)
    {
        if (terminal.Value >= _terminals.Count || _terminals[terminal.Value].Id != terminal)
        {
            throw new CircuitModelException($"Terminal {terminal} is not owned by this builder.");
        }
    }
}

}
