using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Simulation;

namespace CircuitSimulator.Core.Mna;

/// <summary>Assembles DC and backward-Euler transient MNA systems.</summary>
public sealed class MnaAssembler
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
            [ComponentKind.Diode] = new DiodeMnaStamp()
        };
    }

    /// <summary>Assembles a linear DC system.</summary>
    /// <exception cref="MnaAssemblyException">Thrown when the circuit contains a nonlinear component.</exception>
    public MnaLinearSystem AssembleDc(CompiledCircuit circuit) => AssembleDcCore(circuit, null);

    /// <summary>Assembles an affine DC system with nonlinear devices linearized around an iterate.</summary>
    public MnaLinearSystem AssembleDcLinearized(CompiledCircuit circuit, NonlinearStampContext nonlinearContext)
    {
        Guard.NotNull(nonlinearContext, nameof(nonlinearContext));
        if (!ReferenceEquals(circuit, nonlinearContext.Circuit))
        {
            throw new ArgumentException("Nonlinear context belongs to a different compiled circuit.", nameof(nonlinearContext));
        }

        return AssembleDcCore(circuit, nonlinearContext);
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

    private MnaLinearSystem AssembleDcCore(CompiledCircuit circuit, NonlinearStampContext? nonlinearContext)
    {
        Guard.NotNull(circuit, nameof(circuit));
        var builder = new DenseMnaSystemBuilder(circuit.VariableMap.Dimension);
        var context = new MnaStampContext(circuit);

        foreach (var component in circuit.Components)
        {
            GetHandler(component).StampDc(component, context, nonlinearContext, builder);
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
        void StampDc(
            CompiledComponent component,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder);

        void StampTransient(
            CompiledComponent component,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder);
    }

    private abstract class ComponentMnaStamp<TParameters> : IComponentMnaStamp
        where TParameters : class, IComponentParameters
    {
        public void StampDc(
            CompiledComponent component,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder)
        {
            EnsureTwoTerminal(component);
            StampDc(component, RequireParameters(component), context, nonlinearContext, builder);
        }

        public void StampTransient(
            CompiledComponent component,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder)
        {
            EnsureTwoTerminal(component);
            StampTransient(component, RequireParameters(component), context, transientContext, builder);
        }

        protected abstract void StampDc(
            CompiledComponent component,
            TParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder);

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
        protected override void StampDc(
            CompiledComponent component,
            ResistorParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder) => Stamp(component, parameters, context, builder);

        protected override void StampTransient(
            CompiledComponent component,
            ResistorParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder) => Stamp(component, parameters, context, builder);

        private static void Stamp(
            CompiledComponent component,
            ResistorParameters parameters,
            MnaStampContext context,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            MnaStamps.StampConductance(builder, nodes.Positive, nodes.Negative, 1.0 / parameters.Resistance);
        }
    }

    private sealed class CurrentSourceMnaStamp : ComponentMnaStamp<CurrentSourceParameters>
    {
        protected override void StampDc(
            CompiledComponent component,
            CurrentSourceParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder) => Stamp(component, context, builder, parameters.DcValue);

        protected override void StampTransient(
            CompiledComponent component,
            CurrentSourceParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder) =>
            Stamp(component, context, builder, parameters.TransientWaveform.GetValue(transientContext.Time));

        private static void Stamp(
            CompiledComponent component,
            MnaStampContext context,
            IMnaSystemBuilder builder,
            double current)
        {
            var nodes = GetNodes(component, context);
            MnaStamps.StampCurrentSource(builder, nodes.Positive, nodes.Negative, current);
        }
    }

    private sealed class VoltageSourceMnaStamp : ComponentMnaStamp<VoltageSourceParameters>
    {
        protected override void StampDc(
            CompiledComponent component,
            VoltageSourceParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder) => Stamp(component, context, builder, parameters.DcValue);

        protected override void StampTransient(
            CompiledComponent component,
            VoltageSourceParameters parameters,
            MnaStampContext context,
            TransientStampContext transientContext,
            IMnaSystemBuilder builder) =>
            Stamp(component, context, builder, parameters.TransientWaveform.GetValue(transientContext.Time));

        private static void Stamp(
            CompiledComponent component,
            MnaStampContext context,
            IMnaSystemBuilder builder,
            double voltage)
        {
            var nodes = GetNodes(component, context);
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            MnaStamps.StampVoltageSource(builder, nodes.Positive, nodes.Negative, branch, voltage);
        }
    }

    private sealed class CapacitorMnaStamp : ComponentMnaStamp<CapacitorParameters>
    {
        protected override void StampDc(
            CompiledComponent component,
            CapacitorParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder)
        {
            // A capacitor is an open circuit in a DC operating point.
        }

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
        protected override void StampDc(
            CompiledComponent component,
            InductorParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder)
        {
            var nodes = GetNodes(component, context);
            var branch = context.Variables.GetBranchCurrentIndex(component.ComponentId);
            MnaStamps.StampVoltageSource(builder, nodes.Positive, nodes.Negative, branch, 0.0);
        }

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
        protected override void StampDc(
            CompiledComponent component,
            DiodeParameters parameters,
            MnaStampContext context,
            NonlinearStampContext? nonlinearContext,
            IMnaSystemBuilder builder)
        {
            if (nonlinearContext is null)
            {
                throw new MnaAssemblyException(
                    $"Diode '{component.Name}' ({component.ComponentId}) requires a nonlinear solution estimate.");
            }

            Stamp(component, parameters, context, nonlinearContext.SolutionEstimate, builder);
        }

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
