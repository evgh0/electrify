using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

    /// <summary>Severity of a Unity-facing circuit diagnostic.</summary>
    public enum CircuitDiagnosticSeverity
    {
        /// <summary>The circuit can run, but the condition should be reviewed.</summary>
        Warning,
        /// <summary>The circuit cannot be compiled or simulated.</summary>
        Error
    }

    /// <summary>A structured circuit diagnostic that does not expose Core implementation types.</summary>
    public sealed class CircuitDiagnostic
    {
        /// <summary>Creates an immutable diagnostic.</summary>
        public CircuitDiagnostic(string code, CircuitDiagnosticSeverity severity, string message)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Diagnostic code cannot be null or whitespace.", nameof(code));
            }

            Code = code;
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>Gets the stable diagnostic code.</summary>
        public string Code { get; }

        /// <summary>Gets whether this is a warning or error.</summary>
        public CircuitDiagnosticSeverity Severity { get; }

        /// <summary>Gets the user-facing diagnostic message.</summary>
        public string Message { get; }
    }

    /// <summary>Describes a caught compilation or simulation failure without exposing MNA details.</summary>
    public sealed class CircuitSimulationFailure
    {
        /// <summary>Creates an immutable failure snapshot.</summary>
        public CircuitSimulationFailure(Exception exception, IEnumerable<CircuitDiagnostic> validationIssues)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Message = exception.Message;
            ValidationIssues = new ReadOnlyCollection<CircuitDiagnostic>(
                new List<CircuitDiagnostic>(validationIssues ?? Array.Empty<CircuitDiagnostic>()));
        }

        /// <summary>Gets a user-facing diagnostic message.</summary>
        public string Message { get; }

        /// <summary>Gets the original domain exception.</summary>
        public Exception Exception { get; }

        /// <summary>Gets structured validation issues, when compilation produced them.</summary>
        public IReadOnlyList<CircuitDiagnostic> ValidationIssues { get; }
    }

    /// <summary>Describes a live ideal-contact state change.</summary>
    public sealed class CircuitControlStateChange
    {
        internal CircuitControlStateChange(ControlledSwitchComponent component, bool isClosed, double time)
        {
            Component = component ?? throw new ArgumentNullException(nameof(component));
            IsClosed = isClosed;
            Time = time;
        }

        /// <summary>Gets the switch or button whose electrical contact changed.</summary>
        public ControlledSwitchComponent Component { get; }

        /// <summary>Gets whether the contact is now electrically closed.</summary>
        public bool IsClosed { get; }

        /// <summary>Gets simulation time when the change was observed.</summary>
        public double Time { get; }
    }
}
