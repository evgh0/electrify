using System.Collections.ObjectModel;

namespace CircuitSimulator.Core.Validation;

/// <summary>
/// Immutable validation report produced during circuit compilation.
/// </summary>
public sealed class CircuitValidationReport
{
    /// <summary>
    /// Gets an empty validation report.
    /// </summary>
    public static CircuitValidationReport Empty { get; } = new([]);

    /// <summary>
    /// Initializes a new validation report.
    /// </summary>
    /// <param name="issues">The validation issues.</param>
    public CircuitValidationReport(IEnumerable<CircuitValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var issueArray = issues.ToArray();
        if (issueArray.Any(static issue => issue is null))
        {
            throw new ArgumentException("Validation reports cannot contain null issues.", nameof(issues));
        }

        Issues = new ReadOnlyCollection<CircuitValidationIssue>(issueArray);
    }

    /// <summary>
    /// Gets the immutable validation issues.
    /// </summary>
    public IReadOnlyList<CircuitValidationIssue> Issues { get; }

    /// <summary>
    /// Gets a value indicating whether the report contains at least one fatal error.
    /// </summary>
    public bool HasErrors => Issues.Any(static issue => issue.Severity == ValidationSeverity.Error);
}
