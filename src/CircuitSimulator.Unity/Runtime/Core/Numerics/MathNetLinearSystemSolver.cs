#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Mna;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CircuitSimulator.Core.Numerics
{

/// <summary>
/// Solves MNA systems through Math.NET Numerics while preserving domain-layer array immutability.
/// </summary>
internal sealed class MathNetLinearSystemSolver : ILinearSystemSolver
{
    /// <summary>
    /// Initializes a Math.NET linear system solver.
    /// </summary>
    /// <param name="pivotTolerance">The non-negative pivot tolerance for singular detection.</param>
    public MathNetLinearSystemSolver(double pivotTolerance = NumericalConstants.DefaultPivotTolerance)
    {
        if (!Guard.IsFinite(pivotTolerance) || pivotTolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(pivotTolerance), pivotTolerance, "Pivot tolerance must be finite and non-negative.");
        }

        PivotTolerance = pivotTolerance;
    }

    /// <summary>
    /// Gets the pivot tolerance.
    /// </summary>
    public double PivotTolerance { get; }

    /// <inheritdoc />
    public double[] Solve(MnaLinearSystem system)
    {
        Guard.NotNull(system, nameof(system));

        if (system.Dimension == 0)
        {
            return Array.Empty<double>();
        }

        var matrix = system.Matrix;
        var rightHandSide = system.RightHandSide;
        EnsureUsablePivots(matrix);

        try
        {
            Matrix<double> mathNetMatrix = DenseMatrix.OfArray(matrix);
            Vector<double> mathNetVector = DenseVector.OfArray(rightHandSide);
            var solution = mathNetMatrix.Solve(mathNetVector).ToArray();

            for (var index = 0; index < solution.Length; index++)
            {
                if (!Guard.IsFinite(solution[index]))
                {
                    throw new SingularMatrixException($"Math.NET returned a non-finite solution value at index {index}.");
                }
            }

            return solution;
        }
        catch (SingularMatrixException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new SingularMatrixException("Math.NET failed to solve the linear system.", ex);
        }
    }

    private void EnsureUsablePivots(double[,] matrix)
    {
        var dimension = matrix.GetLength(0);
        var work = (double[,])matrix.Clone();

        for (var pivot = 0; pivot < dimension; pivot++)
        {
            var pivotRow = pivot;
            var pivotMagnitude = Math.Abs(work[pivot, pivot]);

            for (var row = pivot + 1; row < dimension; row++)
            {
                var candidateMagnitude = Math.Abs(work[row, pivot]);
                if (candidateMagnitude > pivotMagnitude)
                {
                    pivotMagnitude = candidateMagnitude;
                    pivotRow = row;
                }
            }

            if (pivotMagnitude <= PivotTolerance)
            {
                throw new SingularMatrixException(
                    $"MNA matrix is singular at pivot {pivot}. Absolute pivot value {pivotMagnitude:R} is below or equal to tolerance {PivotTolerance:R}.");
            }

            if (pivotRow != pivot)
            {
                SwapRows(work, pivot, pivotRow, dimension);
            }

            for (var row = pivot + 1; row < dimension; row++)
            {
                var factor = work[row, pivot] / work[pivot, pivot];
                work[row, pivot] = 0.0;

                for (var column = pivot + 1; column < dimension; column++)
                {
                    work[row, column] -= factor * work[pivot, column];
                }
            }
        }
    }

    private static void SwapRows(double[,] matrix, int firstRow, int secondRow, int dimension)
    {
        for (var column = 0; column < dimension; column++)
        {
            (matrix[firstRow, column], matrix[secondRow, column]) = (matrix[secondRow, column], matrix[firstRow, column]);
        }
    }
}

}
