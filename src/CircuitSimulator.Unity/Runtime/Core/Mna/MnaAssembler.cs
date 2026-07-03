#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Simulation;

namespace CircuitSimulator.Core.Mna
{

/// <summary>Assembles one backward-Euler transient MNA system.</summary>
internal sealed class MnaAssembler
{
    private readonly IReadOnlyDictionary<ComponentKind, IComponentMnaStamp> _stampHandlers;

    /// <summary>Initializes an assembler with the built-in component handlers.</summary>
    public MnaAssembler()
    {
        _stampHandlers = new Dictionary<ComponentKind, IComponentMnaStamp>
        {
            [ComponentKind.Resistor] = new ResistorMnaStamp(),
            [ComponentKind.CurrentSource] = new CurrentSourceMnaStamp(),
            [ComponentKind.VoltageSource] = new VoltageSourceMnaStamp(),
            [ComponentKind.Capacitor] = new CapacitorMnaStamp(),
            [ComponentKind.Inductor] = new InductorMnaStamp(),
            [ComponentKind.Diode] = new DiodeMnaStamp(),
            [ComponentKind.Led] = new LedMnaStamp(),
            [ComponentKind.Switch] = new SwitchMnaStamp(),
            [ComponentKind.Jumper] = new JumperMnaStamp()
        };
    }

    /// <summary>Assembles one backward-Euler transient system.</summary>
    public MnaLinearSystem AssembleTransient(TransientStampContext transientContext)
    {
        Guard.NotNull(transientContext, nameof(transientContext));
        var circuit = transientContext.Circuit;
        var builder = new DenseMnaSystemBuilder(circuit.VariableMap.Dimension);
        var context = new MnaStampContext(circuit);

        foreach (var component in circuit.Components)
        {
            GetHandler(component).StampTransient(component, context, transientContext, builder);
        }

        return builder.Build();
    }

    private IComponentMnaStamp GetHandler(CompiledComponent component) =>
        _stampHandlers.TryGetValue(component.Kind, out var handler)
            ? handler
            : throw new MnaAssemblyException(
                $"Component '{component.Name}' ({component.ComponentId}) has unsupported kind {component.Kind}.");

    private interface IComponentMnaStamp
    {
        void StampTransient(
            CompiledComponent component,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder);
    }

    private abstract class ComponentMnaStamp<TParameters> : IComponentMnaStamp
        where TParameters : class, IComponentParameters
    {
        public void StampTransient(
            CompiledComponent component,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            EnsureTwoTerminal(component);
            StampTransient(component, RequireParameters(component), context, transientContext, builder);
        }

        protected abstract void StampTransient(
            CompiledComponent component,
            TParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder);

        protected static (Model.VariableIndex? Positive, Model.VariableIndex? Negative) GetNodes(
            CompiledComponent component,
            MnaStampContext context) =>
            (context.Variables.GetNodeVoltageIndex(component.Nodes[0]),
                context.Variables.GetNodeVoltageIndex(component.Nodes[1]));

        private static TParameters RequireParameters(CompiledComponent component) =>
            component.Parameters as TParameters
            ?? throw InvalidParameters(component);
    }

    private sealed class ResistorMnaStamp : ComponentMnaStamp<ResistorParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            ResistorParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            MnaStamps.StampConductance(builder, nodes.Positive, nodes.Negative, 1.0 / parameters.Resistance);
        }
    }

    private sealed class CurrentSourceMnaStamp : ComponentMnaStamp<CurrentSourceParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            CurrentSourceParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            MnaStamps.StampCurrentSource(
                builder,
                nodes.Positive,
                nodes.Negative,
                parameters.TransientWaveform.GetValue(transientContext.Time));
        }
    }

    private sealed class VoltageSourceMnaStamp : ComponentMnaStamp<VoltageSourceParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            VoltageSourceParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            MnaStamps.StampVoltageSource(
                builder,
                nodes.Positive,
                nodes.Negative,
                branch,
                parameters.TransientWaveform.GetValue(transientContext.Time));
        }
    }

    private sealed class CapacitorMnaStamp : ComponentMnaStamp<CapacitorParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            CapacitorParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var conductance = parameters.Capacitance / transientContext.TimeStep;
            var previousVoltage = transientContext.State.GetCapacitorState(component.ComponentId).PreviousVoltage;
            MnaStamps.StampConductance(builder, nodes.Positive, nodes.Negative, conductance);
            MnaStamps.StampCurrentSource(
                builder,
                nodes.Positive,
                nodes.Negative,
                -conductance * previousVoltage);
        }
    }

    private sealed class InductorMnaStamp : ComponentMnaStamp<InductorParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            InductorParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            var coefficient = parameters.Inductance / transientContext.TimeStep;
            var previousCurrent = transientContext.State.GetInductorState(component.ComponentId).PreviousCurrent;
            MnaStamps.StampVoltageSource(
                builder,
                nodes.Positive,
                nodes.Negative,
                branch,
                -coefficient * previousCurrent);
            builder.AddMatrix(branch, branch, -coefficient);
        }
    }

    private sealed class DiodeMnaStamp : ComponentMnaStamp<DiodeParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            DiodeParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            if (transientContext.SolutionEstimate is null)
            {
                throw new MnaAssemblyException(
                    $"Diode '{component.Name}' ({component.ComponentId}) requires a nonlinear solution estimate.");
            }

            Stamp(component, parameters, context, transientContext.SolutionEstimate, builder);
        }

        private static void Stamp(
            CompiledComponent component,
            DiodeParameters parameters,
            MnaStampContext context,
            IReadOnlyList<double> solutionEstimate,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var voltage = GetValue(nodes.Positive, solutionEstimate) - GetValue(nodes.Negative, solutionEstimate);
            var linearization = DiodeModel.Evaluate(parameters, voltage);
            MnaStamps.StampConductance(builder, nodes.Positive, nodes.Negative, linearization.Conductance);
            MnaStamps.StampCurrentSource(builder, nodes.Positive, nodes.Negative, linearization.EquivalentCurrent);
        }

        private static double GetValue(Model.VariableIndex? index, IReadOnlyList<double> solution) =>
            index is null ? 0.0 : solution[index.Value.Value];
    }

    private sealed class LedMnaStamp : ComponentMnaStamp<LedParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            LedParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            if (transientContext.SolutionEstimate is null)
            {
                throw new MnaAssemblyException(
                    $"LED '{component.Name}' ({component.ComponentId}) requires a nonlinear solution estimate.");
            }

            Stamp(component, parameters, context, transientContext.SolutionEstimate, builder);
        }

        private static void Stamp(
            CompiledComponent component,
            LedParameters parameters,
            MnaStampContext context,
            IReadOnlyList<double> solutionEstimate,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var voltage = GetValue(nodes.Positive, solutionEstimate) - GetValue(nodes.Negative, solutionEstimate);
            var linearization = DiodeModel.Evaluate(parameters, voltage);
            MnaStamps.StampConductance(builder, nodes.Positive, nodes.Negative, linearization.Conductance);
            MnaStamps.StampCurrentSource(builder, nodes.Positive, nodes.Negative, linearization.EquivalentCurrent);
        }

        private static double GetValue(Model.VariableIndex? index, IReadOnlyList<double> solution) =>
            index is null ? 0.0 : solution[index.Value.Value];
    }

    private sealed class SwitchMnaStamp : ComponentMnaStamp<SwitchParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            SwitchParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            if (!transientContext.GetSwitchState(component.ComponentId))
            {
                builder.AddMatrix(branch, branch, 1.0);
                return;
            }

            var nodes = GetNodes(component, context);
            MnaStamps.StampVoltageSource(builder, nodes.Positive, nodes.Negative, branch, 0.0);
        }
    }

    private sealed class JumperMnaStamp : ComponentMnaStamp<JumperParameters>
    {
        protected override void StampTransient(
            CompiledComponent component,
            JumperParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            MnaStamps.StampVoltageSource(builder, nodes.Positive, nodes.Negative, branch, 0.0);
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

}
