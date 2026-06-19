namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Immutable dense representation of an assembled MNA linear system using A * x = z.
/// </summary>
public sealed class MnaLinearSystem
{
    private readonly double[,] _matrix;
    private readonly double[] _rightHandSide;

    /// <summary>
    /// Initializes a new MNA linear system snapshot.
    /// </summary>
    /// <param name="matrix">The square matrix A.</param>
    /// <param name="rightHandSide">The right-hand side z.</param>
    public MnaLinearSystem(double[,] matrix, double[] rightHandSide)
    {
        Guard.NotNull(matrix, nameof(matrix));
        Guard.NotNull(rightHandSide, nameof(rightHandSide));

        var rowCount = matrix.GetLength(0);
        var columnCount = matrix.GetLength(1);
        if (rowCount != columnCount)
        {
            throw new ArgumentException("MNA matrix must be square.", nameof(matrix));
        }

        if (rightHandSide.Length != rowCount)
        {
            throw new ArgumentException("Right-hand side length must match the matrix dimension.", nameof(rightHandSide));
        }

        for (var row = 0; row < rowCount; row++)
        {
            if (!Guard.IsFinite(rightHandSide[row]))
            {
                throw new ArgumentException("Right-hand side entries must be finite.", nameof(rightHandSide));
            }

            for (var column = 0; column < columnCount; column++)
            {
                if (!Guard.IsFinite(matrix[row, column]))
                {
                    throw new ArgumentException("Matrix entries must be finite.", nameof(matrix));
                }
            }
        }

        _matrix = (double[,])matrix.Clone();
        _rightHandSide = (double[])rightHandSide.Clone();
    }

    /// <summary>
    /// Gets the square matrix dimension.
    /// </summary>
    public int Dimension => _rightHandSide.Length;

    /// <summary>
    /// Gets a defensive copy of the matrix A.
    /// </summary>
    public double[,] Matrix => (double[,])_matrix.Clone();

    /// <summary>
    /// Gets a defensive copy of the right-hand side z.
    /// </summary>
    public double[] RightHandSide => (double[])_rightHandSide.Clone();

    /// <summary>
    /// Gets one matrix entry.
    /// </summary>
    /// <param name="row">The zero-based row.</param>
    /// <param name="column">The zero-based column.</param>
    /// <returns>The matrix entry.</returns>
    public double GetMatrixEntry(int row, int column) => _matrix[row, column];

    /// <summary>
    /// Gets one right-hand-side entry.
    /// </summary>
    /// <param name="row">The zero-based row.</param>
    /// <returns>The right-hand-side entry.</returns>
    public double GetRightHandSideEntry(int row) => _rightHandSide[row];
}
