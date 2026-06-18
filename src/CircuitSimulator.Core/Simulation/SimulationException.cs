namespace CircuitSimulator.Core.Simulation;

/// <summary>
/// Represents a simulation failure outside of topology compilation.
/// </summary>
public class SimulationException : Exception
{
    /// <summary>
    /// Initializes a new simulation exception.
    /// </summary>
    public SimulationException()
    {
    }

    /// <summary>
    /// Initializes a new simulation exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public SimulationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new simulation exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SimulationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
