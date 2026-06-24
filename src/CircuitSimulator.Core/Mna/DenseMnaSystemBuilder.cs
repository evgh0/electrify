using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Dense in-memory MNA system builder.
/// </summary>
public sealed class DenseMnaSystemBuilder : IMnaSystemBuilder
{
    private readonly double[,] _matrix;
    private readonly double[] _rightHandSide;

    /// <summary>
    /// Initializes a dense MNA builder.
    /// </summary>
    /// <param name="dimension">The non-negative matrix dimension.</param>
    public DenseMnaSystemBuilder(int dimension)
    {
        if (dimension < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "MNA dimension cannot be negative.");
        }

        Dimension = dimension;
        _matrix = new double[dimension, dimension];
        _rightHandSide = new double[dimension];
    }

    /// <inheritdoc />
    public int Dimension { get; }

    /// <inheritdoc />
    public void AddMatrix(VariableIndex row, VariableIndex column, double value)
    {
        ValidateIndex(row, nameof(row));
        ValidateIndex(column, nameof(column));
        ValidateFinite(value, nameof(value));

        _matrix[row.Value, column.Value] += value;
    }

    /// <inheritdoc />
    public void AddRightHandSide(VariableIndex row, double value)
    {
        ValidateIndex(row, nameof(row));
        ValidateFinite(value, nameof(value));

        _rightHandSide[row.Value] += value;
    }

    /// <inheritdoc />
    public MnaLinearSystem Build() => new(_matrix, _rightHandSide);

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!Guard.IsFinite(value))
        {
            throw new MnaAssemblyException($"MNA contribution '{parameterName}' must be finite.");
        }
    }

    private void ValidateIndex(VariableIndex index, string parameterName)
    {
        if (index.Value >= Dimension)
        {
            throw new MnaAssemblyException($"MNA index {index} for '{parameterName}' is outside dimension {Dimension}.");
        }
    }
}
