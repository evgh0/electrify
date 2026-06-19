using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Tests.Support;

namespace CircuitSimulator.Core.Tests;

public sealed class NonlinearSimulationTests
{
    [Fact]
    public void DiodeLinearizationMatchesFiniteDifferenceAndRemainsFiniteAtHighVoltage()
    {
        var parameters = new DiodeParameters();
        const double voltage = 0.55;
        const double epsilon = 1e-7;
        var linearization = DiodeModel.Evaluate(parameters, voltage);
        var lower = DiodeModel.Evaluate(parameters, voltage - epsilon).Current;
        var upper = DiodeModel.Evaluate(parameters, voltage + epsilon).Current;
        var numericalDerivative = (upper - lower) / (2.0 * epsilon);

        AssertEx.NearlyEqual(numericalDerivative, linearization.Conductance, 1e-6);
        AssertEx.NearlyEqual(
            linearization.Current - (linearization.Conductance * voltage),
            linearization.EquivalentCurrent);
        var highVoltage = DiodeModel.Evaluate(parameters, 10.0);
        Assert.True(double.IsFinite(highVoltage.Current));
        Assert.True(double.IsFinite(highVoltage.Conductance));
    }

    [Fact]
    public void SolvesResistorDiodeDcOperatingPoint()
    {
        var (circuit, diode, resistor) = CreateDiodeCircuit(5.0, 1_000.0);

        var result = new DcOperatingPointSolver().Solve(circuit);
        var diodeVoltage = result.GetComponentVoltage(diode.ComponentId);
        var resistorCurrent = result.GetComponentCurrent(resistor.ComponentId);
        var diodeCurrent = result.GetComponentCurrent(diode.ComponentId);

        Assert.InRange(diodeVoltage, 0.5, 0.8);
        AssertEx.NearlyEqual(resistorCurrent, diodeCurrent, 1e-6);
        AssertEx.NearlyEqual((5.0 - diodeVoltage) / 1_000.0, diodeCurrent, 1e-6);
    }

    [Fact]
    public void ReportsNonlinearConvergenceFailure()
    {
        var (circuit, _, _) = CreateDiodeCircuit(5.0, 1_000.0);
        var options = new NewtonRaphsonOptions(maximumIterations: 1);

        var exception = Assert.Throws<NonlinearConvergenceException>(() =>
            new DcOperatingPointSolver(newtonOptions: options).Solve(circuit));

        Assert.Equal(1, exception.IterationCount);
        Assert.True(exception.MaximumCorrection > 0.0);
    }

    [Fact]
    public void GenericNewtonSolverConvergesUsingAbsoluteAndRelativeCorrection()
    {
        var builder = new CircuitBuilder();
        var resistor = builder.AddResistor("R1", 1.0);
        builder.MarkAsGround(resistor.Negative);
        var compiled = new CircuitCompiler().Compile(builder.Build());
        var solver = new NewtonRaphsonSolver();

        var solution = solver.Solve(
            compiled.VariableMap,
            estimate =>
            {
                var x = estimate[0];
                return new MnaLinearSystem(
                    new[,] { { 2.0 * x } },
                    new[] { (x * x) + 2.0 });
            },
            initialGuess: new[] { 1.0 });

        AssertEx.NearlyEqual(Math.Sqrt(2.0), solution[0], 1e-7);
    }

    [Fact]
    public void SolvesNonlinearTransientWithCapacitorAndSinusoidalSource()
    {
        var builder = new CircuitBuilder();
        var source = builder.AddSinusoidalVoltageSource("V1", 0.0, 1.0, 1.0, Math.PI / 2.0);
        var resistor = builder.AddResistor("R1", 100.0);
        var diode = builder.AddDiode("D1");
        var capacitor = builder.AddCapacitor("C1", 1e-3);
        builder.Connect(source.Negative, diode.Negative, capacitor.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, diode.Positive, capacitor.Positive);

        var result = new TransientSimulationSolver().Solve(
            builder.Build(),
            new TransientSimulationOptions(0.0, 0.02, 0.01));

        Assert.Equal(2, result.Samples.Count);
        Assert.All(result.Samples, sample =>
        {
            Assert.True(double.IsFinite(sample.GetComponentVoltage(capacitor.ComponentId)));
            Assert.True(double.IsFinite(sample.GetComponentCurrent(diode.ComponentId)));
        });
        Assert.True(result.Samples[^1].GetComponentVoltage(capacitor.ComponentId) > 0.0);
    }

    [Fact]
    public void TransientConvergenceFailureReportsTheRejectedStepTime()
    {
        var (circuit, _, _) = CreateDiodeCircuit(5.0, 1_000.0);
        var options = new TransientSimulationOptions(
            0.0,
            0.1,
            0.1,
            newtonOptions: new NewtonRaphsonOptions(maximumIterations: 1));

        var exception = Assert.Throws<NonlinearConvergenceException>(() =>
            new TransientSimulationSolver().Solve(circuit, options));

        AssertEx.NearlyEqual(0.1, exception.SimulationTime!.Value);
        Assert.NotNull(exception.InnerException);
    }

    private static (
        Circuit Circuit,
        TwoTerminalComponentHandle Diode,
        TwoTerminalComponentHandle Resistor) CreateDiodeCircuit(double voltage, double resistance)
    {
        var builder = new CircuitBuilder();
        var source = builder.AddVoltageSource("V1", voltage);
        var resistor = builder.AddResistor("R1", resistance);
        var diode = builder.AddDiode("D1");
        builder.Connect(source.Negative, diode.Negative);
        builder.MarkAsGround(source.Negative);
        builder.Connect(source.Positive, resistor.Positive);
        builder.Connect(resistor.Negative, diode.Positive);
        return (builder.Build(), diode, resistor);
    }
}
