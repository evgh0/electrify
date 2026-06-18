namespace CircuitSimulator.Core.Model;

/// <summary>
/// Identifies a component definition within a circuit.
/// </summary>
public readonly record struct ComponentId
{
    /// <summary>
    /// Gets the non-negative numeric identifier value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new component identifier.
    /// </summary>
    /// <param name="value">The non-negative identifier value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public ComponentId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Component identifiers cannot be negative.");
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => $"ComponentId({Value})";
}

/// <summary>
/// Identifies a physical terminal definition within a circuit.
/// </summary>
public readonly record struct TerminalId
{
    /// <summary>
    /// Gets the non-negative numeric identifier value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new terminal identifier.
    /// </summary>
    /// <param name="value">The non-negative identifier value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public TerminalId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Terminal identifiers cannot be negative.");
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => $"TerminalId({Value})";
}

/// <summary>
/// Identifies a compiled electrical node.
/// </summary>
public readonly record struct NodeId
{
    /// <summary>
    /// Gets the deterministic ground node identifier.
    /// </summary>
    public static NodeId Ground { get; } = new(0);

    /// <summary>
    /// Gets the non-negative numeric identifier value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new node identifier.
    /// </summary>
    /// <param name="value">The non-negative identifier value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public NodeId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Node identifiers cannot be negative.");
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => $"NodeId({Value})";
}

/// <summary>
/// Identifies a row or column in an assembled MNA linear system.
/// </summary>
public readonly record struct VariableIndex
{
    /// <summary>
    /// Gets the non-negative numeric index value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new MNA variable index.
    /// </summary>
    /// <param name="value">The non-negative index value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public VariableIndex(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "MNA variable indexes cannot be negative.");
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => $"VariableIndex({Value})";
}

/// <summary>
/// Identifies an allocated branch-current unknown.
/// </summary>
public readonly record struct BranchVariableId
{
    /// <summary>
    /// Gets the non-negative numeric identifier value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new branch-current identifier.
    /// </summary>
    /// <param name="value">The non-negative identifier value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
    public BranchVariableId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Branch variable identifiers cannot be negative.");
        }

        Value = value;
    }

    /// <inheritdoc />
    public override string ToString() => $"BranchVariableId({Value})";
}
