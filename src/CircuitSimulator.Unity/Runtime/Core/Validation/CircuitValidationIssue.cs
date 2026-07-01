#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Validation
{

/// <summary>
/// Describes one structured validation issue.
/// </summary>
internal sealed class CircuitValidationIssue
{
    /// <summary>
    /// Initializes a structured validation issue.
    /// </summary>
    /// <param name="code">The stable validation issue code.</param>
    /// <param name="severity">The issue severity.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="componentId">The related component identifier, when available.</param>
    /// <param name="terminalId">The related terminal identifier, when available.</param>
    /// <param name="nodeId">The related compiled node identifier, when available.</param>
    public CircuitValidationIssue(
        string code,
        ValidationSeverity severity,
        string message,
        ComponentId? componentId = null,
        TerminalId? terminalId = null,
        NodeId? nodeId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Validation issue code cannot be null or whitespace.", nameof(code));
        }
        Guard.NotNull(message, nameof(message));

        Code = code;
        Severity = severity;
        Message = message;
        ComponentId = componentId;
        TerminalId = terminalId;
        NodeId = nodeId;
    }

    /// <summary>
    /// Gets the stable validation issue code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the issue severity.
    /// </summary>
    public ValidationSeverity Severity { get; }

    /// <summary>
    /// Gets the human-readable validation message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the related component identifier, when available.
    /// </summary>
    public ComponentId? ComponentId { get; }

    /// <summary>
    /// Gets the related terminal identifier, when available.
    /// </summary>
    public TerminalId? TerminalId { get; }

    /// <summary>
    /// Gets the related compiled node identifier, when available.
    /// </summary>
    public NodeId? NodeId { get; }
}

}
