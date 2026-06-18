namespace CircuitSimulator.Core.Model;

/// <summary>
/// Represents an error in the physical circuit model or builder API.
/// </summary>
public class CircuitModelException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitModelException"/> class.
    /// </summary>
    public CircuitModelException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitModelException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public CircuitModelException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitModelException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CircuitModelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
