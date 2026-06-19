using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Unity
{
    /// <summary>Externally visible lifecycle state of a Unity circuit simulation.</summary>
    public enum CircuitSimulationState
    {
        /// <summary>No compiled realtime session exists yet.</summary>
        Uninitialized,
        /// <summary>The simulation advances from Unity Update or explicit ticks.</summary>
        Running,
        /// <summary>The compiled simulation is retained but automatic advancement is paused.</summary>
        Paused,
        /// <summary>The latest rebuild or numerical step failed.</summary>
        Faulted
    }

    /// <summary>Describes a caught compilation or simulation failure without exposing MNA details.</summary>
    public sealed class CircuitSimulationFailure
    {
        /// <summary>Creates an immutable failure snapshot.</summary>
        public CircuitSimulationFailure(Exception exception, IEnumerable<CircuitValidationIssue> validationIssues)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Message = exception.Message;
            ValidationIssues = new ReadOnlyCollection<CircuitValidationIssue>(
                new List<CircuitValidationIssue>(validationIssues ?? Array.Empty<CircuitValidationIssue>()));
        }

        /// <summary>Gets a user-facing diagnostic message.</summary>
        public string Message { get; }

        /// <summary>Gets the original domain exception.</summary>
        public Exception Exception { get; }

        /// <summary>Gets structured Core validation issues, when compilation produced them.</summary>
        public IReadOnlyList<CircuitValidationIssue> ValidationIssues { get; }
    }
}
