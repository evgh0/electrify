#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Core.Compilation
{

/// <summary>
/// Represents a circuit compilation failure with a complete structured validation report.
/// </summary>
internal sealed class CircuitCompilationException : Exception
{
    /// <summary>
    /// Initializes a new compilation exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="validationReport">The validation report that caused compilation to fail.</param>
    public CircuitCompilationException(string message, CircuitValidationReport validationReport)
        : base(message)
    {
        ValidationReport = validationReport ?? throw new ArgumentNullException(nameof(validationReport));
    }

    /// <summary>
    /// Initializes a new compilation exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="validationReport">The validation report that caused compilation to fail.</param>
    /// <param name="innerException">The inner exception.</param>
    public CircuitCompilationException(string message, CircuitValidationReport validationReport, Exception innerException)
        : base(message, innerException)
    {
        ValidationReport = validationReport ?? throw new ArgumentNullException(nameof(validationReport));
    }

    /// <summary>
    /// Gets the complete validation report.
    /// </summary>
    public CircuitValidationReport ValidationReport { get; }
}

}
