#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Mna;

namespace CircuitSimulator.Core.Numerics
{

/// <summary>
/// Solves immutable MNA linear systems.
/// </summary>
internal interface ILinearSystemSolver
{
    /// <summary>
    /// Solves the linear system A * x = z.
    /// </summary>
    /// <param name="system">The linear system.</param>
    /// <returns>A new solution vector.</returns>
    double[] Solve(MnaLinearSystem system);
}

}
