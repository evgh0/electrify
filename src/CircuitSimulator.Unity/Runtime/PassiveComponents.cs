using System;
using CircuitSimulator.Core.Model;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>A two-terminal resistor configured in ohms.</summary>
    [DisallowMultipleComponent]
    public sealed class Resistor : TwoTerminalCircuitComponent
    {
        [SerializeField]
        [Min(float.Epsilon)]
        private double resistanceOhms = 1000.0;

        /// <summary>Gets or sets resistance in ohms.</summary>
        public double ResistanceOhms
        {
            get => resistanceOhms;
            set
            {
                ValidatePositiveFinite(value, nameof(value));
                if (resistanceOhms.Equals(value))
                {
                    return;
                }

                resistanceOhms = value;
                NotifyCircuitChanged();
            }
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddResistor(coreName, resistanceOhms);
        }

        private static void ValidatePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Resistance must be finite and greater than zero.");
            }
        }
    }

    /// <summary>A two-terminal capacitor configured in farads.</summary>
    [DisallowMultipleComponent]
    public sealed class Capacitor : TwoTerminalCircuitComponent
    {
        [SerializeField]
        [Min(float.Epsilon)]
        private double capacitanceFarads = 1e-6;

        /// <summary>Gets or sets capacitance in farads.</summary>
        public double CapacitanceFarads
        {
            get => capacitanceFarads;
            set
            {
                ValidatePositiveFinite(value, nameof(value), "Capacitance");
                if (capacitanceFarads.Equals(value))
                {
                    return;
                }

                capacitanceFarads = value;
                NotifyCircuitChanged();
            }
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddCapacitor(coreName, capacitanceFarads);
        }

        private static void ValidatePositiveFinite(double value, string parameterName, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, label + " must be finite and greater than zero.");
            }
        }
    }

    /// <summary>A two-terminal inductor configured in henries.</summary>
    [DisallowMultipleComponent]
    public sealed class Inductor : TwoTerminalCircuitComponent
    {
        [SerializeField]
        [Min(float.Epsilon)]
        private double inductanceHenries = 1e-3;

        /// <summary>Gets or sets inductance in henries.</summary>
        public double InductanceHenries
        {
            get => inductanceHenries;
            set
            {
                ValidatePositiveFinite(value, nameof(value));
                if (inductanceHenries.Equals(value))
                {
                    return;
                }

                inductanceHenries = value;
                NotifyCircuitChanged();
            }
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddInductor(coreName, inductanceHenries);
        }

        private static void ValidatePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Inductance must be finite and greater than zero.");
            }
        }
    }

    /// <summary>A two-terminal Shockley diode.</summary>
    [DisallowMultipleComponent]
    public sealed class Diode : TwoTerminalCircuitComponent
    {
        [SerializeField]
        [Min(float.Epsilon)]
        private double saturationCurrent = 1e-12;

        [SerializeField]
        [Min(float.Epsilon)]
        private double idealityFactor = 1.0;

        [SerializeField]
        [Min(float.Epsilon)]
        private double thermalVoltage = 0.025851999786;

        /// <summary>Gets or sets reverse saturation current in amperes.</summary>
        public double SaturationCurrent
        {
            get => saturationCurrent;
            set => SetPositiveFinite(ref saturationCurrent, value, nameof(value));
        }

        /// <summary>Gets or sets the emission ideality factor.</summary>
        public double IdealityFactor
        {
            get => idealityFactor;
            set => SetPositiveFinite(ref idealityFactor, value, nameof(value));
        }

        /// <summary>Gets or sets thermal voltage in volts.</summary>
        public double ThermalVoltage
        {
            get => thermalVoltage;
            set => SetPositiveFinite(ref thermalVoltage, value, nameof(value));
        }

        /// <summary>Configures all diode model values atomically.</summary>
        public void Configure(double newSaturationCurrent, double newIdealityFactor, double newThermalVoltage)
        {
            ValidatePositiveFinite(newSaturationCurrent, nameof(newSaturationCurrent));
            ValidatePositiveFinite(newIdealityFactor, nameof(newIdealityFactor));
            ValidatePositiveFinite(newThermalVoltage, nameof(newThermalVoltage));
            saturationCurrent = newSaturationCurrent;
            idealityFactor = newIdealityFactor;
            thermalVoltage = newThermalVoltage;
            NotifyCircuitChanged();
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddDiode(coreName, saturationCurrent, idealityFactor, thermalVoltage);
        }

        private void SetPositiveFinite(ref double field, double value, string parameterName)
        {
            ValidatePositiveFinite(value, parameterName);
            if (field.Equals(value))
            {
                return;
            }

            field = value;
            NotifyCircuitChanged();
        }

        private static void ValidatePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Diode parameters must be finite and greater than zero.");
            }
        }
    }
}
