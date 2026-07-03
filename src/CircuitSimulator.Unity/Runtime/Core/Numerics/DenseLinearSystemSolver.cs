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
/// Solves dense MNA systems using Gaussian elimination with partial pivoting.
/// </summary>
internal sealed class DenseLinearSystemSolver : ILinearSystemSolver
{
    /// <summary>
    /// Initializes a dense linear system solver.
    /// </summary>
    /// <param name="pivotTolerance">The non-negative pivot tolerance for singular detection.</param>
    public DenseLinearSystemSolver(double pivotTolerance = NumericalConstants.DefaultPivotTolerance)
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
        Eliminate(matrix, rightHandSide);
        return BackSubstitute(matrix, rightHandSide);
    }

    private void Eliminate(double[,] matrix, double[] rightHandSide)
    {
        var dimension = rightHandSide.Length;
        for (var pivot = 0; pivot < dimension; pivot++)
        {
            var pivotRow = FindPivotRow(matrix, pivot, dimension);
            var pivotMagnitude = Math.Abs(matrix[pivotRow, pivot]);
            if (pivotMagnitude <= PivotTolerance)
            {
                throw new SingularMatrixException(
                    $"MNA matrix is singular at pivot {pivot}. Absolute pivot value {pivotMagnitude:R} is below or equal to tolerance {PivotTolerance:R}.");
            }

            if (pivotRow != pivot)
            {
                SwapRows(matrix, rightHandSide, pivot, pivotRow, dimension);
            }

            var pivotValue = matrix[pivot, pivot];
            for (var row = pivot + 1; row < dimension; row++)
            {
                var factor = matrix[row, pivot] / pivotValue;
                matrix[row, pivot] = 0.0;
                rightHandSide[row] -= factor * rightHandSide[pivot];
                EnsureFinite(rightHandSide[row], row, "right-hand side");

                for (var column = pivot + 1; column < dimension; column++)
                {
                    matrix[row, column] -= factor * matrix[pivot, column];
                    EnsureFinite(matrix[row, column], row, column);
                }
            }
        }
    }

    private static int FindPivotRow(double[,] matrix, int pivot, int dimension)
    {
        var pivotRow = pivot;
        var pivotMagnitude = Math.Abs(matrix[pivot, pivot]);

        for (var row = pivot + 1; row < dimension; row++)
        {
            var candidateMagnitude = Math.Abs(matrix[row, pivot]);
            if (candidateMagnitude > pivotMagnitude)
            {
                pivotMagnitude = candidateMagnitude;
                pivotRow = row;
            }
        }

        return pivotRow;
    }

    private static double[] BackSubstitute(double[,] matrix, double[] rightHandSide)
    {
        var dimension = rightHandSide.Length;
        var solution = new double[dimension];
        for (var row = dimension - 1; row >= 0; row--)
        {
            var sum = rightHandSide[row];
            for (var column = row + 1; column < dimension; column++)
            {
                sum -= matrix[row, column] * solution[column];
                EnsureFinite(sum, row, "back-substitution sum");
            }

            var value = sum / matrix[row, row];
            if (!Guard.IsFinite(value))
            {
                throw new SingularMatrixException($"Dense solver returned a non-finite solution value at index {row}.");
            }

            solution[row] = value;
        }

        return solution;
    }

    private static void SwapRows(double[,] matrix, double[] rightHandSide, int firstRow, int secondRow, int dimension)
    {
        for (var column = 0; column < dimension; column++)
        {
            (matrix[firstRow, column], matrix[secondRow, column]) = (matrix[secondRow, column], matrix[firstRow, column]);
        }

        (rightHandSide[firstRow], rightHandSide[secondRow]) = (rightHandSide[secondRow], rightHandSide[firstRow]);
    }

    private static void EnsureFinite(double value, int row, int column)
    {
        if (!Guard.IsFinite(value))
        {
            throw new SingularMatrixException($"Dense solver produced a non-finite matrix value at row {row}, column {column}.");
        }
    }

    private static void EnsureFinite(double value, int row, string valueName)
    {
        if (!Guard.IsFinite(value))
        {
            throw new SingularMatrixException($"Dense solver produced a non-finite {valueName} value at row {row}.");
        }
    }
}

}
