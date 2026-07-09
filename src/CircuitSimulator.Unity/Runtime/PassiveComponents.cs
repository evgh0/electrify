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
            set => SetResistance(value);
        }

        /// <summary>Sets resistance in ohms and schedules a circuit rebuild when it changes.</summary>
        public void SetResistance(double resistanceOhms)
        {
            ValidatePositiveFinite(resistanceOhms, nameof(resistanceOhms));
            if (this.resistanceOhms.Equals(resistanceOhms))
            {
                return;
            }

            this.resistanceOhms = resistanceOhms;
            NotifyCircuitChanged();
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
            set => SetCapacitance(value);
        }

        /// <summary>Sets capacitance in farads and schedules a circuit rebuild when it changes.</summary>
        public void SetCapacitance(double capacitanceFarads)
        {
            ValidatePositiveFinite(capacitanceFarads, nameof(capacitanceFarads), "Capacitance");
            if (this.capacitanceFarads.Equals(capacitanceFarads))
            {
                return;
            }

            this.capacitanceFarads = capacitanceFarads;
            NotifyCircuitChanged();
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
            set => SetInductance(value);
        }

        /// <summary>Sets inductance in henries and schedules a circuit rebuild when it changes.</summary>
        public void SetInductance(double inductanceHenries)
        {
            ValidatePositiveFinite(inductanceHenries, nameof(inductanceHenries));
            if (this.inductanceHenries.Equals(inductanceHenries))
            {
                return;
            }

            this.inductanceHenries = inductanceHenries;
            NotifyCircuitChanged();
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
            set => SetSaturationCurrent(value);
        }

        /// <summary>Gets or sets the emission ideality factor.</summary>
        public double IdealityFactor
        {
            get => idealityFactor;
            set => SetIdealityFactor(value);
        }

        /// <summary>Gets or sets thermal voltage in volts.</summary>
        public double ThermalVoltage
        {
            get => thermalVoltage;
            set => SetThermalVoltage(value);
        }

        /// <summary>Sets reverse saturation current in amperes and schedules a circuit rebuild when it changes.</summary>
        public void SetSaturationCurrent(double saturationCurrent)
        {
            SetPositiveFinite(ref this.saturationCurrent, saturationCurrent, nameof(saturationCurrent));
        }

        /// <summary>Sets the emission ideality factor and schedules a circuit rebuild when it changes.</summary>
        public void SetIdealityFactor(double idealityFactor)
        {
            SetPositiveFinite(ref this.idealityFactor, idealityFactor, nameof(idealityFactor));
        }

        /// <summary>Sets thermal voltage in volts and schedules a circuit rebuild when it changes.</summary>
        public void SetThermalVoltage(double thermalVoltage)
        {
            SetPositiveFinite(ref this.thermalVoltage, thermalVoltage, nameof(thermalVoltage));
        }

        /// <summary>Sets all diode model values atomically.</summary>
        public void SetModel(double saturationCurrent, double idealityFactor, double thermalVoltage)
        {
            Configure(saturationCurrent, idealityFactor, thermalVoltage);
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

    /// <summary>A two-terminal LED modeled as a Shockley diode fitted to a nominal forward operating point.</summary>
    [DisallowMultipleComponent]
    public sealed class Led : TwoTerminalCircuitComponent
    {
        [SerializeField]
        [Min(float.Epsilon)]
        private double nominalForwardVoltage = 2.0;

        [SerializeField]
        [Min(float.Epsilon)]
        private double referenceCurrent = 0.02;

        [SerializeField]
        [Min(float.Epsilon)]
        private double idealityFactor = 2.0;

        [SerializeField]
        [Min(float.Epsilon)]
        private double thermalVoltage = 0.025851999786;

        /// <summary>Gets or sets nominal forward voltage in volts.</summary>
        public double NominalForwardVoltage
        {
            get => nominalForwardVoltage;
            set => SetNominalForwardVoltage(value);
        }

        /// <summary>Gets or sets reference forward current in amperes.</summary>
        public double ReferenceCurrent
        {
            get => referenceCurrent;
            set => SetReferenceCurrent(value);
        }

        /// <summary>Gets or sets the LED emission ideality factor.</summary>
        public double IdealityFactor
        {
            get => idealityFactor;
            set => SetIdealityFactor(value);
        }

        /// <summary>Gets or sets thermal voltage in volts.</summary>
        public double ThermalVoltage
        {
            get => thermalVoltage;
            set => SetThermalVoltage(value);
        }

        /// <summary>Sets nominal forward voltage in volts and schedules a circuit rebuild when it changes.</summary>
        public void SetNominalForwardVoltage(double nominalForwardVoltage)
        {
            SetPositiveFinite(ref this.nominalForwardVoltage, nominalForwardVoltage, nameof(nominalForwardVoltage));
        }

        /// <summary>Sets reference forward current in amperes and schedules a circuit rebuild when it changes.</summary>
        public void SetReferenceCurrent(double referenceCurrent)
        {
            SetPositiveFinite(ref this.referenceCurrent, referenceCurrent, nameof(referenceCurrent));
        }

        /// <summary>Sets the LED emission ideality factor and schedules a circuit rebuild when it changes.</summary>
        public void SetIdealityFactor(double idealityFactor)
        {
            SetPositiveFinite(ref this.idealityFactor, idealityFactor, nameof(idealityFactor));
        }

        /// <summary>Sets thermal voltage in volts and schedules a circuit rebuild when it changes.</summary>
        public void SetThermalVoltage(double thermalVoltage)
        {
            SetPositiveFinite(ref this.thermalVoltage, thermalVoltage, nameof(thermalVoltage));
        }

        /// <summary>Sets all LED model values atomically.</summary>
        public void SetModel(
            double nominalForwardVoltage,
            double referenceCurrent,
            double idealityFactor,
            double thermalVoltage)
        {
            Configure(nominalForwardVoltage, referenceCurrent, idealityFactor, thermalVoltage);
        }

        /// <summary>Configures all LED model values atomically.</summary>
        public void Configure(
            double newNominalForwardVoltage,
            double newReferenceCurrent,
            double newIdealityFactor,
            double newThermalVoltage)
        {
            ValidatePositiveFinite(newNominalForwardVoltage, nameof(newNominalForwardVoltage));
            ValidatePositiveFinite(newReferenceCurrent, nameof(newReferenceCurrent));
            ValidatePositiveFinite(newIdealityFactor, nameof(newIdealityFactor));
            ValidatePositiveFinite(newThermalVoltage, nameof(newThermalVoltage));
            nominalForwardVoltage = newNominalForwardVoltage;
            referenceCurrent = newReferenceCurrent;
            idealityFactor = newIdealityFactor;
            thermalVoltage = newThermalVoltage;
            NotifyCircuitChanged();
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddLed(
                coreName,
                nominalForwardVoltage,
                referenceCurrent,
                idealityFactor,
                thermalVoltage);
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
                throw new ArgumentOutOfRangeException(parameterName, value, "LED parameters must be finite and greater than zero.");
            }
        }
    }

    /// <summary>A readable two-terminal ideal jumper wire.</summary>
    [DisallowMultipleComponent]
    public sealed class Jumper : TwoTerminalCircuitComponent
    {
        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddJumper(coreName);
        }
    }
}
