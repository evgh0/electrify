using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Accumulates matrix and right-hand-side contributions for an MNA linear system.
/// </summary>
public interface IMnaSystemBuilder
{
    /// <summary>
    /// Gets the square matrix dimension.
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// Adds a matrix contribution.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="value">The finite contribution to add.</param>
    void AddMatrix(VariableIndex row, VariableIndex column, double value);

    /// <summary>
    /// Adds a right-hand-side contribution.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="value">The finite contribution to add.</param>
    void AddRightHandSide(VariableIndex row, double value);

    /// <summary>
    /// Builds an immutable linear system snapshot.
    /// </summary>
    /// <returns>The assembled linear system.</returns>
    MnaLinearSystem Build();
}
