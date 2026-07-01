#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Numerics;

namespace CircuitSimulator.Core.Simulation
{

/// <summary>Solves affine Newton linearizations using the configured real linear-system solver.</summary>
internal sealed class NewtonRaphsonSolver
{
    private readonly ILinearSystemSolver _linearSolver;

    /// <summary>Initializes a Newton-Raphson solver.</summary>
    public NewtonRaphsonSolver(ILinearSystemSolver? linearSolver = null) =>
        _linearSolver = linearSolver ?? new MathNetLinearSystemSolver();

    /// <summary>Solves a nonlinear system represented by affine tangent systems.</summary>
    public double[] Solve(
        MnaVariableMap variableMap,
        Func<IReadOnlyList<double>, MnaLinearSystem> linearize,
        IReadOnlyList<double>? initialGuess = null,
        NewtonRaphsonOptions? options = null,
        Func<IReadOnlyList<double>, IReadOnlyList<double>, double[]>? limitStep = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(variableMap, nameof(variableMap));
        Guard.NotNull(linearize, nameof(linearize));
        options ??= NewtonRaphsonOptions.Default;
        var current = initialGuess?.ToArray() ?? new double[variableMap.Dimension];
        if (current.Length != variableMap.Dimension || current.Any(value => !Guard.IsFinite(value)))
        {
            throw new ArgumentException("Initial guess must contain one finite value per MNA variable.", nameof(initialGuess));
        }

        var maximumCorrection = double.PositiveInfinity;
        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var affine = linearize(current);
            if (affine.Dimension != variableMap.Dimension)
            {
                throw new SimulationException("Nonlinear linearization dimension does not match the variable map.");
            }

            var correctionSystem = BuildCorrectionSystem(affine, current);
            var correction = _linearSolver.Solve(correctionSystem);
            var candidate = new double[current.Length];
            for (var index = 0; index < candidate.Length; index++)
            {
                candidate[index] = current[index] + correction[index];
            }

            if (limitStep is not null)
            {
                candidate = limitStep(current, candidate);
            }

            if (candidate.Length != current.Length || candidate.Any(value => !Guard.IsFinite(value)))
            {
                throw new NonlinearConvergenceException(
                    $"Newton-Raphson produced a non-finite iterate at iteration {iteration}.",
                    iteration,
                    double.PositiveInfinity);
            }

            maximumCorrection = 0.0;
            var converged = true;
            for (var index = 0; index < candidate.Length; index++)
            {
                var absoluteCorrection = Math.Abs(candidate[index] - current[index]);
                maximumCorrection = Math.Max(maximumCorrection, absoluteCorrection);
                var variable = variableMap.Variables[index];
                var absoluteTolerance = variable.Kind == MnaVariableKind.NodeVoltage
                    ? options.VoltageAbsoluteTolerance
                    : options.CurrentAbsoluteTolerance;
                var scale = Math.Max(Math.Abs(current[index]), Math.Abs(candidate[index]));
                if (absoluteCorrection > absoluteTolerance + (options.RelativeTolerance * scale))
                {
                    converged = false;
                }
            }

            if (converged)
            {
                return candidate;
            }

            current = candidate;
        }

        throw new NonlinearConvergenceException(
            $"Newton-Raphson did not converge after {options.MaximumIterations} iterations; maximum correction was {maximumCorrection:R}.",
            options.MaximumIterations,
            maximumCorrection);
    }

    private static MnaLinearSystem BuildCorrectionSystem(MnaLinearSystem affine, IReadOnlyList<double> current)
    {
        var matrix = affine.Matrix;
        var residual = affine.RightHandSide;
        for (var row = 0; row < affine.Dimension; row++)
        {
            for (var column = 0; column < affine.Dimension; column++)
            {
                residual[row] -= matrix[row, column] * current[column];
            }
        }

        return new MnaLinearSystem(matrix, residual);
    }
}

}
