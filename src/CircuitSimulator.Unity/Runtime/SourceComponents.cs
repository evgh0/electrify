using System;
using CircuitSimulator.Core.Model;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Selects the transient waveform exposed by an independent source.</summary>
    public enum SourceWaveformMode
    {
        /// <summary>A time-invariant source value.</summary>
        Constant,
        /// <summary>An offset sinusoidal source.</summary>
        Sinusoidal
    }

    /// <summary>Shared inspector and runtime configuration for independent sources.</summary>
    public abstract class IndependentSource : TwoTerminalCircuitComponent
    {
        [SerializeField]
        private SourceWaveformMode waveformMode;

        [SerializeField]
        private double constantValue;

        [SerializeField]
        private double offset;

        [SerializeField]
        private double amplitude = 1.0;

        [SerializeField]
        [Min(float.Epsilon)]
        private double frequencyHz = 1.0;

        [SerializeField]
        private double phaseRadians;

        /// <summary>Gets the configured waveform mode.</summary>
        public SourceWaveformMode WaveformMode => waveformMode;

        /// <summary>Gets the constant source value.</summary>
        public double ConstantValue => constantValue;

        /// <summary>Gets the sinusoidal DC offset.</summary>
        public double Offset => offset;

        /// <summary>Gets the sinusoidal peak amplitude.</summary>
        public double Amplitude => amplitude;

        /// <summary>Gets sinusoidal frequency in hertz.</summary>
        public double FrequencyHz => frequencyHz;

        /// <summary>Gets sinusoidal phase in radians.</summary>
        public double PhaseRadians => phaseRadians;

        /// <summary>Configures a constant source value.</summary>
        public void ConfigureConstant(double value)
        {
            ValidateFinite(value, nameof(value));
            waveformMode = SourceWaveformMode.Constant;
            constantValue = value;
            NotifyCircuitChanged();
        }

        /// <summary>Configures an offset sinusoidal waveform.</summary>
        public void ConfigureSinusoidal(
            double newOffset,
            double newAmplitude,
            double newFrequencyHz,
            double newPhaseRadians = 0.0)
        {
            ValidateFinite(newOffset, nameof(newOffset));
            ValidateFinite(newAmplitude, nameof(newAmplitude));
            ValidateFinite(newPhaseRadians, nameof(newPhaseRadians));
            if (double.IsNaN(newFrequencyHz) || double.IsInfinity(newFrequencyHz) || newFrequencyHz <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newFrequencyHz),
                    newFrequencyHz,
                    "Frequency must be finite and greater than zero.");
            }

            waveformMode = SourceWaveformMode.Sinusoidal;
            offset = newOffset;
            amplitude = newAmplitude;
            frequencyHz = newFrequencyHz;
            phaseRadians = newPhaseRadians;
            NotifyCircuitChanged();
        }

        /// <summary>Validates serialized source values before building Core parameters.</summary>
        protected void ValidateConfiguration()
        {
            if (waveformMode == SourceWaveformMode.Constant)
            {
                ValidateFinite(constantValue, nameof(ConstantValue));
                return;
            }

            ValidateFinite(offset, nameof(Offset));
            ValidateFinite(amplitude, nameof(Amplitude));
            ValidateFinite(phaseRadians, nameof(PhaseRadians));
            if (double.IsNaN(frequencyHz) || double.IsInfinity(frequencyHz) || frequencyHz <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(FrequencyHz), frequencyHz, "Frequency must be finite and greater than zero.");
            }
        }

        private static void ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Source values must be finite.");
            }
        }
    }

    /// <summary>An independent two-terminal voltage source.</summary>
    [DisallowMultipleComponent]
    public sealed class VoltageSource : IndependentSource
    {
        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            ValidateConfiguration();
            return WaveformMode == SourceWaveformMode.Constant
                ? builder.AddVoltageSource(coreName, ConstantValue)
                : builder.AddSinusoidalVoltageSource(coreName, Offset, Amplitude, FrequencyHz, PhaseRadians);
        }
    }

    /// <summary>An independent two-terminal current source.</summary>
    [DisallowMultipleComponent]
    public sealed class CurrentSource : IndependentSource
    {
        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            ValidateConfiguration();
            return WaveformMode == SourceWaveformMode.Constant
                ? builder.AddCurrentSource(coreName, ConstantValue)
                : builder.AddSinusoidalCurrentSource(coreName, Offset, Amplitude, FrequencyHz, PhaseRadians);
        }
    }
}
