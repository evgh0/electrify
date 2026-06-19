using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Numerics;
using CircuitSimulator.Core.Results;

namespace CircuitSimulator.Core.Simulation;

/// <summary>
/// Solves linear DC operating points for compiled circuits.
/// </summary>
public sealed class DcOperatingPointSolver
{
    private readonly ILinearSystemSolver _linearSystemSolver;
    private readonly MnaAssembler _assembler;
    private readonly NewtonRaphsonOptions _newtonOptions;

    /// <summary>
    /// Initializes a DC operating-point solver.
    /// </summary>
    /// <param name="linearSystemSolver">The linear solver. When omitted, Math.NET is used.</param>
    /// <param name="assembler">The MNA assembler. When omitted, the built-in DC assembler is used.</param>
    /// <param name="newtonOptions">Nonlinear convergence options. When omitted, production defaults are used.</param>
    public DcOperatingPointSolver(
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null,
        NewtonRaphsonOptions? newtonOptions = null)
    {
        _linearSystemSolver = linearSystemSolver ?? new MathNetLinearSystemSolver();
        _assembler = assembler ?? new MnaAssembler();
        _newtonOptions = newtonOptions ?? NewtonRaphsonOptions.Default;
    }

    /// <summary>
    /// Compiles and solves a physical circuit.
    /// </summary>
    /// <param name="circuit">The physical circuit.</param>
    /// <returns>The DC operating-point result.</returns>
    public DcOperatingPointResult Solve(Circuit circuit)
    {
        Guard.NotNull(circuit, nameof(circuit));

        var compiledCircuit = new CircuitCompiler().Compile(circuit);
        return Solve(compiledCircuit);
    }

    /// <summary>
    /// Solves an already compiled circuit.
    /// </summary>
    /// <param name="circuit">The compiled circuit.</param>
    /// <returns>The DC operating-point result.</returns>
    public DcOperatingPointResult Solve(CompiledCircuit circuit)
    {
        Guard.NotNull(circuit, nameof(circuit));
        DcAnalysisValidator.Validate(circuit);

        MnaLinearSystem linearSystem;
        double[] solution;
        if (NonlinearCircuitUtilities.ContainsNonlinearComponents(circuit))
        {
            var nonlinearSolver = new NewtonRaphsonSolver(_linearSystemSolver);
            solution = nonlinearSolver.Solve(
                circuit.VariableMap,
                estimate => _assembler.AssembleDcLinearized(
                    circuit,
                    new NonlinearStampContext(circuit, estimate)),
                options: _newtonOptions,
                limitStep: (current, candidate) => NonlinearCircuitUtilities.LimitDiodeVoltageStep(
                    circuit,
                    current,
                    candidate,
                    _newtonOptions.MaximumDiodeVoltageStep));
            linearSystem = _assembler.AssembleDcLinearized(
                circuit,
                new NonlinearStampContext(circuit, solution));
        }
        else
        {
            linearSystem = _assembler.AssembleDc(circuit);
            solution = _linearSystemSolver.Solve(linearSystem);
        }

        return new DcOperatingPointResult(circuit, linearSystem, solution);
    }
}
