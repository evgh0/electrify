using System;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Results;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Base class for a simulated physical component with mapped results.</summary>
    public abstract class CircuitComponent : CircuitElement
    {
        [SerializeField]
        private string displayName;

        private CircuitReading? latestReading;

        /// <summary>Raised after every accepted numerical step.</summary>
        public event Action<CircuitReading> ReadingChanged;

        /// <summary>Gets or sets the user-facing component label.</summary>
        public string DisplayName
        {
            get => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
            set
            {
                var normalized = value ?? string.Empty;
                if (displayName == normalized)
                {
                    return;
                }

                displayName = normalized;
                NotifyCircuitChanged();
            }
        }

        /// <summary>Gets whether an accepted sample has been mapped since the last rebuild.</summary>
        public bool HasReading => latestReading.HasValue;

        /// <summary>Gets the latest reading, or <see langword="null"/> before the first accepted step.</summary>
        public CircuitReading? LatestReading => latestReading;

        /// <summary>Gets latest component voltage, or zero when no reading exists.</summary>
        public double Voltage => latestReading?.Voltage ?? 0.0;

        /// <summary>Gets latest positive-to-negative current, or zero when no reading exists.</summary>
        public double Current => latestReading?.Current ?? 0.0;

        /// <summary>Gets latest instantaneous power, or zero when no reading exists.</summary>
        public double Power => latestReading?.Power ?? 0.0;

        internal abstract TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName);

        internal void Publish(TransientSample sample, ComponentId componentId)
        {
            var reading = new CircuitReading(
                sample.Time,
                sample.GetComponentVoltage(componentId),
                sample.GetComponentCurrent(componentId));
            latestReading = reading;
            ReadingChanged?.Invoke(reading);
        }

        internal void ClearReading()
        {
            latestReading = null;
        }
    }

    /// <summary>Base class for components whose terminal 0 is positive and terminal 1 is negative.</summary>
    public abstract class TwoTerminalCircuitComponent : CircuitComponent
    {
        [SerializeField]
        private CircuitTerminal positive;

        [SerializeField]
        private CircuitTerminal negative;

        /// <summary>Gets terminal 0, the positive/reference terminal.</summary>
        public CircuitTerminal Positive => positive;

        /// <summary>Gets terminal 1, the negative terminal.</summary>
        public CircuitTerminal Negative => negative;

        /// <summary>Assigns pre-existing terminal objects.</summary>
        public void SetTerminals(CircuitTerminal positiveTerminal, CircuitTerminal negativeTerminal)
        {
            positive = positiveTerminal ?? throw new ArgumentNullException(nameof(positiveTerminal));
            negative = negativeTerminal ?? throw new ArgumentNullException(nameof(negativeTerminal));
            if (ReferenceEquals(positive, negative))
            {
                throw new ArgumentException("Positive and negative terminals must be different objects.");
            }

            NotifyCircuitChanged();
        }

        /// <summary>Creates missing positive and negative terminal child objects.</summary>
        public void EnsureTerminals()
        {
            if (positive == null)
            {
                positive = CreateTerminal("Positive (+)");
            }

            if (negative == null)
            {
                negative = CreateTerminal("Negative (-)");
            }

            NotifyCircuitChanged();
        }

        private CircuitTerminal CreateTerminal(string terminalName)
        {
            var terminalObject = new GameObject(terminalName);
            terminalObject.transform.SetParent(transform, false);
            return terminalObject.AddComponent<CircuitTerminal>();
        }
    }
}
