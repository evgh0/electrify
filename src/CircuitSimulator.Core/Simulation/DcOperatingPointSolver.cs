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

    /// <summary>
    /// Initializes a DC operating-point solver.
    /// </summary>
    /// <param name="linearSystemSolver">The linear solver. When omitted, Math.NET is used.</param>
    /// <param name="assembler">The MNA assembler. When omitted, the built-in DC assembler is used.</param>
    public DcOperatingPointSolver(
        ILinearSystemSolver? linearSystemSolver = null,
        MnaAssembler? assembler = null)
    {
        _linearSystemSolver = linearSystemSolver ?? new MathNetLinearSystemSolver();
        _assembler = assembler ?? new MnaAssembler();
    }

    /// <summary>
    /// Compiles and solves a physical circuit.
    /// </summary>
    /// <param name="circuit">The physical circuit.</param>
    /// <returns>The DC operating-point result.</returns>
    public DcOperatingPointResult Solve(Circuit circuit)
    {
        ArgumentNullException.ThrowIfNull(circuit);

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
        ArgumentNullException.ThrowIfNull(circuit);

        var linearSystem = _assembler.AssembleDc(circuit);
        var solution = _linearSystemSolver.Solve(linearSystem);
        return new DcOperatingPointResult(circuit, linearSystem, solution);
    }
}
