namespace CircuitSimulator.Core.Numerics;

/// <summary>
/// Represents a singular or numerically unusable linear system.
/// </summary>
public sealed class SingularMatrixException : Exception
{
    /// <summary>
    /// Initializes a new singular matrix exception.
    /// </summary>
    public SingularMatrixException()
    {
    }

    /// <summary>
    /// Initializes a new singular matrix exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public SingularMatrixException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new singular matrix exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SingularMatrixException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
