using CircuitSimulator.Core.Mna;

namespace CircuitSimulator.Core.Numerics;

/// <summary>
/// Solves immutable MNA linear systems.
/// </summary>
public interface ILinearSystemSolver
{
    /// <summary>
    /// Solves the linear system A * x = z.
    /// </summary>
    /// <param name="system">The linear system.</param>
    /// <returns>A new solution vector.</returns>
    double[] Solve(MnaLinearSystem system);
}
