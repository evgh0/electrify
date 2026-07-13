using System;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>One accepted voltage-probe result.</summary>
    public readonly struct VoltageProbeReading
    {
        /// <summary>Creates a voltage-probe reading.</summary>
        public VoltageProbeReading(double time, double voltage)
        {
            Time = time;
            Voltage = voltage;
        }

        /// <summary>Gets simulation time in seconds.</summary>
        public double Time { get; }

        /// <summary>Gets positive-terminal minus negative-terminal voltage in volts.</summary>
        public double Voltage { get; }
    }

    /// <summary>One accepted current-probe result.</summary>
    public readonly struct CurrentProbeReading
    {
        /// <summary>Creates a current-probe reading.</summary>
        public CurrentProbeReading(double time, double current)
        {
            Time = time;
            Current = current;
        }

        /// <summary>Gets simulation time in seconds.</summary>
        public double Time { get; }

        /// <summary>Gets target-component positive-to-negative current in amperes.</summary>
        public double Current { get; }
    }

    /// <summary>Base class for observation-only probes that never participate in circuit compilation.</summary>
    public abstract class CircuitProbe : CircuitElement
    {
        /// <summary>Clears any accepted reading when the probe becomes inactive.</summary>
        protected override void OnDisable()
        {
            ClearReading();
            base.OnDisable();
        }

        /// <summary>Clears stale output after Inspector edits without invalidating the electrical circuit.</summary>
        protected override void OnValidate()
        {
            ClearReading();
        }

        /// <summary>Clears the current observer value when its target is unavailable.</summary>
        protected internal abstract void ClearReading();
    }

    /// <summary>Measures voltage between two existing terminals without loading or changing the circuit.</summary>
    [DisallowMultipleComponent]
    public sealed class VoltageProbe : CircuitProbe
    {
        [SerializeField]
        private CircuitTerminal positive;

        [SerializeField]
        private CircuitTerminal negative;

        private VoltageProbeReading? latestReading;

        /// <summary>Raised after every accepted step for which both terminals are available.</summary>
        public event Action<VoltageProbeReading> ReadingChanged;

        /// <summary>Gets the positive/reference measurement terminal.</summary>
        public CircuitTerminal Positive => positive;

        /// <summary>Gets the negative measurement terminal.</summary>
        public CircuitTerminal Negative => negative;

        /// <summary>Gets whether the probe has a reading for its current terminal pair.</summary>
        public bool HasReading => latestReading.HasValue;

        /// <summary>Gets the latest accepted reading, or <see langword="null"/> when unavailable.</summary>
        public VoltageProbeReading? LatestReading => latestReading;

        /// <summary>Gets the latest measured voltage, or zero when unavailable.</summary>
        public double Voltage => latestReading?.Voltage ?? 0.0;

        /// <summary>Changes the measured terminal pair without rebuilding or restarting the simulation.</summary>
        public void SetTerminals(CircuitTerminal positiveTerminal, CircuitTerminal negativeTerminal)
        {
            if (positiveTerminal == null)
            {
                throw new ArgumentNullException(nameof(positiveTerminal));
            }

            if (negativeTerminal == null)
            {
                throw new ArgumentNullException(nameof(negativeTerminal));
            }

            if (ReferenceEquals(positiveTerminal, negativeTerminal))
            {
                throw new ArgumentException("A voltage probe requires two different terminals.", nameof(negativeTerminal));
            }

            if (ReferenceEquals(positive, positiveTerminal) && ReferenceEquals(negative, negativeTerminal))
            {
                return;
            }

            positive = positiveTerminal;
            negative = negativeTerminal;
            ClearReading();
        }

        internal void Publish(double time, double voltage)
        {
            var reading = new VoltageProbeReading(time, voltage);
            latestReading = reading;
            ReadingChanged?.Invoke(reading);
        }

        protected internal override void ClearReading()
        {
            latestReading = null;
        }
    }

    /// <summary>Observes an existing component's current without adding a measurement branch.</summary>
    [DisallowMultipleComponent]
    public sealed class CurrentProbe : CircuitProbe
    {
        [SerializeField]
        private CircuitComponent target;

        private CurrentProbeReading? latestReading;

        /// <summary>Raised after every accepted step for which the target component is available.</summary>
        public event Action<CurrentProbeReading> ReadingChanged;

        /// <summary>Gets the observed component.</summary>
        public CircuitComponent Target => target;

        /// <summary>Gets whether the probe has a reading for its current target.</summary>
        public bool HasReading => latestReading.HasValue;

        /// <summary>Gets the latest accepted reading, or <see langword="null"/> when unavailable.</summary>
        public CurrentProbeReading? LatestReading => latestReading;

        /// <summary>Gets the latest target-component current, or zero when unavailable.</summary>
        public double Current => latestReading?.Current ?? 0.0;

        /// <summary>Changes the observed component without rebuilding or restarting the simulation.</summary>
        public void SetTarget(CircuitComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (ReferenceEquals(target, component))
            {
                return;
            }

            target = component;
            ClearReading();
        }

        internal void Publish(double time, double current)
        {
            var reading = new CurrentProbeReading(time, current);
            latestReading = reading;
            ReadingChanged?.Invoke(reading);
        }

        protected internal override void ClearReading()
        {
            latestReading = null;
        }
    }
}
