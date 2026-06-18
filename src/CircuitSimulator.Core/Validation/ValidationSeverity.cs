namespace CircuitSimulator.Core.Validation;

/// <summary>
/// Describes the severity of a circuit validation issue.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational validation output.
    /// </summary>
    Info,

    /// <summary>
    /// A non-fatal validation warning.
    /// </summary>
    Warning,

    /// <summary>
    /// A fatal validation error.
    /// </summary>
    Error
}
