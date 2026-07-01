using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Core.Tests;

public sealed class ReactiveComponentTests
{
    [Fact]
    public void BuilderAddsReactiveAndDiodeComponentsWithValidatedParameters()
    {
        var builder = new CircuitBuilder();
        var capacitor = builder.AddCapacitor("C1", 1e-6);
        var inductor = builder.AddInductor("L1", 2e-3);
        var diode = builder.AddDiode("D1");
        var led = builder.AddLed("LED1");
        var circuit = builder.Build();

        Assert.IsType<CapacitorParameters>(circuit.GetComponent(capacitor.ComponentId).Parameters);
        Assert.IsType<InductorParameters>(circuit.GetComponent(inductor.ComponentId).Parameters);
        Assert.IsType<DiodeParameters>(circuit.GetComponent(diode.ComponentId).Parameters);
        Assert.IsType<LedParameters>(circuit.GetComponent(led.ComponentId).Parameters);
        Assert.Equal("A", circuit.GetTerminal(diode.Positive).Name);
        Assert.Equal("K", circuit.GetTerminal(diode.Negative).Name);
        Assert.Equal("A", circuit.GetTerminal(led.Positive).Name);
        Assert.Equal("K", circuit.GetTerminal(led.Negative).Name);
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapacitorParameters(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InductorParameters(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiodeParameters(-1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedParameters(referenceCurrent: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedParameters(nominalForwardVoltage: 1_000.0));
    }

    [Fact]
    public void LedParametersDeriveShockleySaturationCurrentFromNominalOperatingPoint()
    {
        var parameters = new LedParameters();
        var nominal = DiodeModel.Evaluate(parameters, parameters.NominalForwardVoltage);

        Assert.Equal(ComponentKind.Led, parameters.Kind);
        Assert.True(double.IsFinite(parameters.SaturationCurrent));
        Assert.True(parameters.SaturationCurrent > 0.0);
        AssertEx.NearlyEqual(parameters.ReferenceCurrent, nominal.Current, 1e-10);
    }

    [Fact]
    public void CompilerRejectsLedKindWithIncompatibleParameters()
    {
        var componentId = new ComponentId(0);
        var positive = new TerminalId(0);
        var negative = new TerminalId(1);
        var circuit = new Circuit(
            new[]
            {
                new ComponentDefinition(componentId, "LED1", [positive, negative], new FakeLedParameters())
            },
            new[]
            {
                new TerminalDefinition(positive, componentId, 0, "A"),
                new TerminalDefinition(negative, componentId, 1, "K")
            },
            [],
            [negative]);

        var exception = Assert.Throws<CircuitCompilationException>(() => new CircuitCompiler().Compile(circuit));

        Assert.Contains(
            exception.ValidationReport.Issues,
            issue => issue.Code == ValidationCodes.ComponentInvalidParameter);
    }

    [Fact]
    public void SinusoidalSourcesPreserveDcValueAndEvaluateTransientWaveform()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddSinusoidalVoltageSource("V1", 2.0, 3.0, 10.0, Math.PI / 2.0);
        var parameters = Assert.IsType<VoltageSourceParameters>(
            builder.Build().GetComponent(source.ComponentId).Parameters);

        AssertEx.NearlyEqual(2.0, parameters.Voltage);
        AssertEx.NearlyEqual(2.0, parameters.DcValue);
        AssertEx.NearlyEqual(5.0, parameters.TransientWaveform.GetValue(0.0));
        AssertEx.NearlyEqual(-1.0, parameters.TransientWaveform.GetValue(0.05));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SinusoidalSourceWaveform(0.0, 1.0, 0.0));
    }

    [Fact]
    public void VariableMapOrdersVoltageSourcesBeforeInductors()
    {
        var builder = new CircuitBuilder();
        var inductor = builder.AddInductor("L1", 1.0);
        var source = builder.AddVoltageSource("V1", 1.0);
        builder.Connect(inductor.Negative, source.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(inductor.Positive, source.Positive);

        var compiled = new CircuitCompiler().Compile(builder.Build());

        Assert.Equal(new VariableIndex(1), compiled.VariableMap.GetBranchCurrentIndex(source.ComponentId));
        Assert.Equal(new VariableIndex(2), compiled.VariableMap.GetBranchCurrentIndex(inductor.ComponentId));
    }

    [Fact]
    public void CapacitorTransientStampUsesBackwardEulerHistory()
    {
        var builder = new CircuitBuilder();
        var capacitor = builder.AddCapacitor("C1", 1.0);
        builder.MarkAsGround(capacitor.Negative);
        var compiled = new CircuitCompiler().Compile(builder.Build());
        var initial = new TransientInitialConditions(
            capacitorVoltages: new Dictionary<ComponentId, double> { [capacitor.ComponentId] = 2.0 });
        var state = SimulationState.Create(compiled, initial);

        var system = new MnaAssembler().AssembleTransient(
            new TransientStampContext(compiled, state, time: 0.5, timeStep: 0.5));

        AssertEx.NearlyEqual(2.0, system.GetMatrixEntry(0, 0));
        AssertEx.NearlyEqual(4.0, system.GetRightHandSideEntry(0));
    }

    [Fact]
    public void InductorTransientStampUsesBranchEquationAndHistory()
    {
        var builder = new CircuitBuilder();
        var inductor = builder.AddInductor("L1", 2.0);
        builder.MarkAsGround(inductor.Negative);
        var compiled = new CircuitCompiler().Compile(builder.Build());
        var initial = new TransientInitialConditions(
            inductorCurrents: new Dictionary<ComponentId, double> { [inductor.ComponentId] = 0.5 });
        var state = SimulationState.Create(compiled, initial);

        var system = new MnaAssembler().AssembleTransient(
            new TransientStampContext(compiled, state, time: 0.25, timeStep: 0.25));

        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(0, 1));
        AssertEx.NearlyEqual(1.0, system.GetMatrixEntry(1, 0));
        AssertEx.NearlyEqual(-8.0, system.GetMatrixEntry(1, 1));
        AssertEx.NearlyEqual(-4.0, system.GetRightHandSideEntry(1));
    }

    [Fact]
    public void DcTreatsCapacitorAsOpenAndInductorAsShort()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", 1.0);
        var resistor = builder.AddResistor("R1", 1.0);
        var inductor = builder.AddInductor("L1", 1.0);
        var capacitor = builder.AddCapacitor("C1", 1.0);
        builder.Connect(source.Negative, inductor.Negative, capacitor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive, capacitor.Positive);
        builder.Connect(resistor.Negative, inductor.Positive);

        var result = new DcOperatingPointSolver().Solve(builder.Build());

        AssertEx.NearlyEqual(0.0, result.GetTerminalVoltage(inductor.Positive));
        AssertEx.NearlyEqual(1.0, result.GetComponentCurrent(inductor.ComponentId));
        AssertEx.NearlyEqual(0.0, result.GetComponentCurrent(capacitor.ComponentId));
    }

    [Fact]
    public void DcReportsNodesThatFloatOnlyThroughAnOpenCapacitor()
    {
        var builder = new CircuitBuilder();
        var capacitor = builder.AddCapacitor("C1", 1.0);
        builder.MarkAsGround(capacitor.Negative);

        var exception = Assert.Throws<SimulationException>(() =>
            new DcOperatingPointSolver().Solve(builder.Build()));

        Assert.Contains("capacitors", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NodeId(1)", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FakeLedParameters : IComponentParameters
    {
        public ComponentKind Kind => ComponentKind.Led;
    }
}
