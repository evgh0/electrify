using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Assembles DC MNA systems for compiled linear circuits.
/// </summary>
public sealed class MnaAssembler
{
    private readonly IReadOnlyDictionary<ComponentKind, IComponentMnaStamp> _stampHandlers;

    /// <summary>
    /// Initializes a new MNA assembler with the built-in linear component stamp handlers.
    /// </summary>
    public MnaAssembler()
    {
        _stampHandlers = new Dictionary<ComponentKind, IComponentMnaStamp>
        {
            [ComponentKind.Resistor] = new ResistorMnaStamp(),
            [ComponentKind.CurrentSource] = new CurrentSourceMnaStamp(),
            [ComponentKind.VoltageSource] = new VoltageSourceMnaStamp()
        };
    }

    /// <summary>
    /// Assembles a DC linear system for a compiled circuit.
    /// </summary>
    /// <param name="circuit">The compiled circuit.</param>
    /// <returns>The assembled MNA linear system.</returns>
    public MnaLinearSystem AssembleDc(CompiledCircuit circuit)
    {
        ArgumentNullException.ThrowIfNull(circuit);

        var builder = new DenseMnaSystemBuilder(circuit.VariableMap.Dimension);
        var context = new MnaStampContext(circuit);

        foreach (var component in circuit.Components)
        {
            if (!_stampHandlers.TryGetValue(component.Kind, out var handler))
            {
                throw new MnaAssemblyException($"Component '{component.Name}' ({component.ComponentId}) has unsupported kind {component.Kind}.");
            }

            handler.Stamp(component, context, builder);
        }

        return builder.Build();
    }

    private interface IComponentMnaStamp
    {
        void Stamp(CompiledComponent component, MnaStampContext context, IMnaSystemBuilder builder);
    }

    private sealed class ResistorMnaStamp : IComponentMnaStamp
    {
        public void Stamp(CompiledComponent component, MnaStampContext context, IMnaSystemBuilder builder)
        {
            EnsureTwoTerminal(component);
            if (component.Parameters is not ResistorParameters parameters)
            {
                throw InvalidParameters(component);
            }

            var positive = context.Variables.GetNodeVoltageIndex(component.Nodes[0]);
            var negative = context.Variables.GetNodeVoltageIndex(component.Nodes[1]);
            MnaStamps.StampConductance(builder, positive, negative, 1.0 / parameters.Resistance);
        }
    }

    private sealed class CurrentSourceMnaStamp : IComponentMnaStamp
    {
        public void Stamp(CompiledComponent component, MnaStampContext context, IMnaSystemBuilder builder)
        {
            EnsureTwoTerminal(component);
            if (component.Parameters is not CurrentSourceParameters parameters)
            {
                throw InvalidParameters(component);
            }

            var positive = context.Variables.GetNodeVoltageIndex(component.Nodes[0]);
            var negative = context.Variables.GetNodeVoltageIndex(component.Nodes[1]);
            MnaStamps.StampCurrentSource(builder, positive, negative, parameters.Current);
        }
    }

    private sealed class VoltageSourceMnaStamp : IComponentMnaStamp
    {
        public void Stamp(CompiledComponent component, MnaStampContext context, IMnaSystemBuilder builder)
        {
            EnsureTwoTerminal(component);
            if (component.Parameters is not VoltageSourceParameters parameters)
            {
                throw InvalidParameters(component);
            }

            var positive = context.Variables.GetNodeVoltageIndex(component.Nodes[0]);
            var negative = context.Variables.GetNodeVoltageIndex(component.Nodes[1]);
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            MnaStamps.StampVoltageSource(builder, positive, negative, branch, parameters.Voltage);
        }
    }

    private static void EnsureTwoTerminal(CompiledComponent component)
    {
        if (component.Nodes.Count != 2)
        {
            throw new MnaAssemblyException(
                $"Component '{component.Name}' ({component.ComponentId}) requires exactly two compiled nodes but has {component.Nodes.Count}.");
        }
    }

    private static MnaAssemblyException InvalidParameters(CompiledComponent component) =>
        new($"Component '{component.Name}' ({component.ComponentId}) has parameter type incompatible with kind {component.Kind}.");
}
