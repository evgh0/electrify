using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Electrical reading fields supported by threshold analysis.</summary>
    public enum CircuitMetric
    {
        /// <summary>Positive-terminal minus negative-terminal voltage.</summary>
        Voltage,
        /// <summary>Positive-to-negative current.</summary>
        Current,
        /// <summary>Passive-sign-convention power.</summary>
        Power
    }

    /// <summary>Direction in which a threshold condition becomes active.</summary>
    public enum CircuitThresholdDirection
    {
        /// <summary>The condition enters when the value rises to the enter threshold.</summary>
        Above,
        /// <summary>The condition enters when the value falls to the enter threshold.</summary>
        Below
    }

    /// <summary>Kinds of observations produced by <see cref="CircuitAnalyzer"/>.</summary>
    public enum CircuitAnalysisEventKind
    {
        /// <summary>The circuit compiled successfully.</summary>
        CircuitRebuilt,
        /// <summary>The simulation lifecycle state changed.</summary>
        SimulationStateChanged,
        /// <summary>Compilation or stepping failed.</summary>
        SimulationFailure,
        /// <summary>A switch or button contact changed.</summary>
        ControlStateChanged,
        /// <summary>A threshold condition became active.</summary>
        ThresholdEntered,
        /// <summary>A threshold condition became inactive.</summary>
        ThresholdExited
    }

    /// <summary>An immutable event suitable for circuit context history.</summary>
    public sealed class CircuitAnalysisEvent
    {
        internal CircuitAnalysisEvent(
            double time,
            CircuitAnalysisEventKind kind,
            string message,
            CircuitComponent component = null,
            CircuitMetric? metric = null,
            double? value = null)
        {
            Time = time;
            Kind = kind;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Component = component;
            Metric = metric;
            Value = value;
        }

        /// <summary>Gets simulation time associated with the event.</summary>
        public double Time { get; }
        /// <summary>Gets the event classification.</summary>
        public CircuitAnalysisEventKind Kind { get; }
        /// <summary>Gets the human-readable event description.</summary>
        public string Message { get; }
        /// <summary>Gets the affected component, when applicable.</summary>
        public CircuitComponent Component { get; }
        /// <summary>Gets the measured metric, when applicable.</summary>
        public CircuitMetric? Metric { get; }
        /// <summary>Gets the signed measured value, when applicable.</summary>
        public double? Value { get; }
    }

    /// <summary>A per-component threshold with hysteresis.</summary>
    public sealed class CircuitThresholdRule
    {
        internal CircuitThresholdRule(
            CircuitComponent component,
            CircuitMetric metric,
            CircuitThresholdDirection direction,
            double enterValue,
            double exitValue)
        {
            Component = component;
            Metric = metric;
            Direction = direction;
            EnterValue = enterValue;
            ExitValue = exitValue;
        }

        /// <summary>Gets the observed component.</summary>
        public CircuitComponent Component { get; }
        /// <summary>Gets the observed metric.</summary>
        public CircuitMetric Metric { get; }
        /// <summary>Gets the comparison direction.</summary>
        public CircuitThresholdDirection Direction { get; }
        /// <summary>Gets the value that activates the condition.</summary>
        public double EnterValue { get; }
        /// <summary>Gets the value that deactivates the condition.</summary>
        public double ExitValue { get; }
        /// <summary>Gets whether the condition is currently active.</summary>
        public bool IsActive { get; internal set; }
        internal Action<CircuitReading> Handler { get; set; }
    }

    /// <summary>Observes circuit lifecycle, controls, and configured electrical thresholds.</summary>
    [DisallowMultipleComponent]
    public sealed class CircuitAnalyzer : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        private int historyCapacity = 64;

        [SerializeField]
        private CircuitSimulation simulation;

        private readonly List<CircuitAnalysisEvent> history = new List<CircuitAnalysisEvent>();
        private readonly List<CircuitThresholdRule> rules = new List<CircuitThresholdRule>();
        private bool subscribed;
        private bool readingsSubscribed;
        private CircuitSimulation subscribedSimulation;

        /// <summary>Raised whenever an event is appended to history.</summary>
        public event Action<CircuitAnalysisEvent> EventRecorded;

        /// <summary>Gets the observed simulation.</summary>
        public CircuitSimulation Simulation => simulation;

        /// <summary>Gets or sets the maximum number of retained events.</summary>
        public int HistoryCapacity
        {
            get => historyCapacity;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "History capacity must be at least one.");
                }

                historyCapacity = value;
                TrimHistory();
            }
        }

        /// <summary>Gets an immutable copy of retained events, oldest first.</summary>
        public IReadOnlyList<CircuitAnalysisEvent> RecentEvents =>
            new ReadOnlyCollection<CircuitAnalysisEvent>(history.ToArray());

        /// <summary>Gets an immutable copy of configured threshold rules.</summary>
        public IReadOnlyList<CircuitThresholdRule> ThresholdRules =>
            new ReadOnlyCollection<CircuitThresholdRule>(rules.ToArray());

        /// <summary>Associates this analyser with a simulation.</summary>
        public void SetSimulation(CircuitSimulation circuitSimulation)
        {
            if (circuitSimulation == null)
            {
                throw new ArgumentNullException(nameof(circuitSimulation));
            }

            if (ReferenceEquals(simulation, circuitSimulation))
            {
                return;
            }
            if (rules.Count > 0)
            {
                throw new InvalidOperationException("Remove threshold rules before changing the analysed simulation.");
            }

            Unsubscribe();
            simulation = circuitSimulation;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Adds an above/below rule with explicit hysteresis.</summary>
        public CircuitThresholdRule AddThreshold(
            CircuitComponent component,
            CircuitMetric metric,
            CircuitThresholdDirection direction,
            double enterValue,
            double exitValue)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }
            ValidateFinite(enterValue, nameof(enterValue));
            ValidateFinite(exitValue, nameof(exitValue));
            if (!Enum.IsDefined(typeof(CircuitMetric), metric))
            {
                throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown circuit metric.");
            }
            if (!Enum.IsDefined(typeof(CircuitThresholdDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown threshold direction.");
            }
            if (simulation == null || !ReferenceEquals(component.Simulation, simulation))
            {
                throw new ArgumentException("The component must belong to this analyser's circuit simulation.", nameof(component));
            }
            if (direction == CircuitThresholdDirection.Above && exitValue > enterValue)
            {
                throw new ArgumentException("An above-threshold exit value cannot exceed its enter value.", nameof(exitValue));
            }
            if (direction == CircuitThresholdDirection.Below && exitValue < enterValue)
            {
                throw new ArgumentException("A below-threshold exit value cannot be less than its enter value.", nameof(exitValue));
            }

            var rule = new CircuitThresholdRule(component, metric, direction, enterValue, exitValue);
            rules.Add(rule);
            rule.Handler = reading => Evaluate(rule, reading);
            if (readingsSubscribed)
            {
                component.ReadingChanged += rule.Handler;
            }
            return rule;
        }

        /// <summary>Removes a rule. Removed rules ignore any already-attached reading callback.</summary>
        public bool RemoveThreshold(CircuitThresholdRule rule)
        {
            if (rule == null)
            {
                return false;
            }
            rule.IsActive = false;
            if (!rules.Remove(rule))
            {
                return false;
            }
            if (readingsSubscribed && rule.Component != null)
            {
                rule.Component.ReadingChanged -= rule.Handler;
            }
            return true;
        }

        /// <summary>Removes all retained events without removing rules.</summary>
        public void ClearHistory() => history.Clear();

        private void OnEnable()
        {
            if (simulation == null)
            {
                simulation = GetComponent<CircuitSimulation>() ?? GetComponentInParent<CircuitSimulation>();
            }
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy()
        {
            for (var index = rules.Count - 1; index >= 0; index--)
            {
                RemoveThreshold(rules[index]);
            }
        }

        private void OnValidate()
        {
            if (historyCapacity < 1)
            {
                historyCapacity = 1;
            }
            TrimHistory();
            if (subscribed && !ReferenceEquals(subscribedSimulation, simulation))
            {
                Unsubscribe();
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (subscribed || simulation == null)
            {
                return;
            }
            simulation.CircuitRebuilt += OnCircuitRebuilt;
            simulation.StateChanged += OnStateChanged;
            simulation.SimulationFailed += OnFailure;
            simulation.ControlStateChanged += OnControlStateChanged;
            subscribedSimulation = simulation;
            subscribed = true;
            for (var index = 0; index < rules.Count; index++)
            {
                if (rules[index].Component != null && ReferenceEquals(rules[index].Component.Simulation, simulation))
                {
                    rules[index].Component.ReadingChanged += rules[index].Handler;
                }
            }
            readingsSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }
            if (readingsSubscribed)
            {
                for (var index = 0; index < rules.Count; index++)
                {
                    if (rules[index].Component != null)
                    {
                        rules[index].Component.ReadingChanged -= rules[index].Handler;
                    }
                }
                readingsSubscribed = false;
            }
            if (subscribedSimulation != null)
            {
                subscribedSimulation.CircuitRebuilt -= OnCircuitRebuilt;
                subscribedSimulation.StateChanged -= OnStateChanged;
                subscribedSimulation.SimulationFailed -= OnFailure;
                subscribedSimulation.ControlStateChanged -= OnControlStateChanged;
            }
            subscribedSimulation = null;
            subscribed = false;
        }

        private void OnCircuitRebuilt()
        {
            for (var index = 0; index < rules.Count; index++)
            {
                rules[index].IsActive = false;
            }
            Record(new CircuitAnalysisEvent(simulation.CurrentTime, CircuitAnalysisEventKind.CircuitRebuilt, "Circuit rebuilt successfully."));
        }

        private void OnStateChanged(CircuitSimulationState state) => Record(new CircuitAnalysisEvent(
            simulation.CurrentTime,
            CircuitAnalysisEventKind.SimulationStateChanged,
            "Simulation state changed to " + state + "."));

        private void OnFailure(CircuitSimulationFailure failure) => Record(new CircuitAnalysisEvent(
            simulation.CurrentTime,
            CircuitAnalysisEventKind.SimulationFailure,
            failure.Message));

        private void OnControlStateChanged(CircuitControlStateChange change) => Record(new CircuitAnalysisEvent(
            change.Time,
            CircuitAnalysisEventKind.ControlStateChanged,
            change.Component.DisplayName + " contact " + (change.IsClosed ? "closed." : "opened."),
            change.Component));

        private void Evaluate(CircuitThresholdRule rule, CircuitReading reading)
        {
            if (!rules.Contains(rule))
            {
                return;
            }
            var value = GetValue(rule.Metric, reading);
            var enter = rule.Direction == CircuitThresholdDirection.Above
                ? value >= rule.EnterValue
                : value <= rule.EnterValue;
            var exit = rule.Direction == CircuitThresholdDirection.Above
                ? value <= rule.ExitValue
                : value >= rule.ExitValue;

            if (!rule.IsActive && enter)
            {
                rule.IsActive = true;
                RecordThreshold(rule, reading.Time, value, CircuitAnalysisEventKind.ThresholdEntered, "entered");
            }
            else if (rule.IsActive && exit)
            {
                rule.IsActive = false;
                RecordThreshold(rule, reading.Time, value, CircuitAnalysisEventKind.ThresholdExited, "exited");
            }
        }

        private void RecordThreshold(
            CircuitThresholdRule rule,
            double time,
            double value,
            CircuitAnalysisEventKind kind,
            string transition)
        {
            var message = string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} {2} threshold ({3:R}; value={4:R}).",
                rule.Component.DisplayName,
                transition,
                rule.Metric,
                kind == CircuitAnalysisEventKind.ThresholdEntered ? rule.EnterValue : rule.ExitValue,
                value);
            Record(new CircuitAnalysisEvent(time, kind, message, rule.Component, rule.Metric, value));
        }

        private void Record(CircuitAnalysisEvent item)
        {
            history.Add(item);
            TrimHistory();
            EventRecorded?.Invoke(item);
        }

        private void TrimHistory()
        {
            while (history.Count > historyCapacity)
            {
                history.RemoveAt(0);
            }
        }

        private static double GetValue(CircuitMetric metric, CircuitReading reading)
        {
            switch (metric)
            {
                case CircuitMetric.Voltage: return reading.Voltage;
                case CircuitMetric.Current: return reading.Current;
                case CircuitMetric.Power: return reading.Power;
                default: throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown circuit metric.");
            }
        }

        private static void ValidateFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name, value, "Thresholds must be finite.");
            }
        }
    }
}
