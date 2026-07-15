using System;
using CircuitSimulator.Core.Model;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Selects the transient waveform exposed by an independent source.</summary>
    public enum SourceWaveformMode
    {
        /// <summary>A time-invariant source value.</summary>
        Constant = 0,
        /// <summary>An offset sinusoidal source.</summary>
        Sinusoidal = 1,
        /// <summary>An offset, symmetric square-wave source with a 50-percent duty cycle.</summary>
        Square = 2,
        /// <summary>An offset, symmetric triangle-wave source.</summary>
        Triangle = 3
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

        /// <summary>Gets the periodic waveform offset.</summary>
        public double Offset => offset;

        /// <summary>Gets the periodic waveform peak amplitude.</summary>
        public double Amplitude => amplitude;

        /// <summary>Gets the periodic waveform frequency in hertz.</summary>
        public double FrequencyHz => frequencyHz;

        /// <summary>Gets the periodic waveform phase in radians.</summary>
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
            ConfigurePeriodic(
                SourceWaveformMode.Sinusoidal,
                newOffset,
                newAmplitude,
                newFrequencyHz,
                newPhaseRadians);
        }

        /// <summary>Configures an offset, symmetric square waveform with a 50-percent duty cycle.</summary>
        public void ConfigureSquare(
            double newOffset,
            double newAmplitude,
            double newFrequencyHz,
            double newPhaseRadians = 0.0)
        {
            ConfigurePeriodic(
                SourceWaveformMode.Square,
                newOffset,
                newAmplitude,
                newFrequencyHz,
                newPhaseRadians);
        }

        /// <summary>Configures an offset, symmetric triangle waveform.</summary>
        public void ConfigureTriangle(
            double newOffset,
            double newAmplitude,
            double newFrequencyHz,
            double newPhaseRadians = 0.0)
        {
            ConfigurePeriodic(
                SourceWaveformMode.Triangle,
                newOffset,
                newAmplitude,
                newFrequencyHz,
                newPhaseRadians);
        }

        private void ConfigurePeriodic(
            SourceWaveformMode mode,
            double newOffset,
            double newAmplitude,
            double newFrequencyHz,
            double newPhaseRadians)
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

            waveformMode = mode;
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

            if (waveformMode != SourceWaveformMode.Sinusoidal &&
                waveformMode != SourceWaveformMode.Square &&
                waveformMode != SourceWaveformMode.Triangle)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(WaveformMode),
                    waveformMode,
                    "The source waveform mode is not supported.");
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
        /// <summary>Sets a constant voltage in volts and schedules a circuit rebuild.</summary>
        public void SetVoltage(double voltage)
        {
            ConfigureConstant(voltage);
        }

        /// <summary>Sets an offset sinusoidal voltage waveform and schedules a circuit rebuild.</summary>
        public void SetSinusoidalVoltage(
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            ConfigureSinusoidal(offset, amplitude, frequencyHz, phaseRadians);
        }

        /// <summary>Sets a symmetric square voltage waveform and schedules a circuit rebuild.</summary>
        public void SetSquareVoltage(
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            ConfigureSquare(offset, amplitude, frequencyHz, phaseRadians);
        }

        /// <summary>Sets a symmetric triangle voltage waveform and schedules a circuit rebuild.</summary>
        public void SetTriangleVoltage(
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            ConfigureTriangle(offset, amplitude, frequencyHz, phaseRadians);
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            ValidateConfiguration();
            return WaveformMode switch
            {
                SourceWaveformMode.Constant => builder.AddVoltageSource(coreName, ConstantValue),
                SourceWaveformMode.Sinusoidal => builder.AddSinusoidalVoltageSource(
                    coreName, Offset, Amplitude, FrequencyHz, PhaseRadians),
                SourceWaveformMode.Square => builder.AddSquareVoltageSource(
                    coreName, Offset, Amplitude, FrequencyHz, PhaseRadians),
                SourceWaveformMode.Triangle => builder.AddTriangleVoltageSource(
                    coreName, Offset, Amplitude, FrequencyHz, PhaseRadians),
                _ => throw new ArgumentOutOfRangeException(nameof(WaveformMode), WaveformMode, "The source waveform mode is not supported.")
            };
        }
    }

    /// <summary>An independent two-terminal current source.</summary>
    [DisallowMultipleComponent]
    public sealed class CurrentSource : IndependentSource
    {
        /// <summary>Sets a constant current in amperes and schedules a circuit rebuild.</summary>
        public void SetCurrent(double current)
        {
            ConfigureConstant(current);
        }

        /// <summary>Sets an offset sinusoidal current waveform and schedules a circuit rebuild.</summary>
        public void SetSinusoidalCurrent(
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            ConfigureSinusoidal(offset, amplitude, frequencyHz, phaseRadians);
        }

        /// <summary>Sets a symmetric square current waveform and schedules a circuit rebuild.</summary>
        public void SetSquareCurrent(
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            ConfigureSquare(offset, amplitude, frequencyHz, phaseRadians);
        }

        /// <summary>Sets a symmetric triangle current waveform and schedules a circuit rebuild.</summary>
        public void SetTriangleCurrent(
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            ConfigureTriangle(offset, amplitude, frequencyHz, phaseRadians);
        }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            ValidateConfiguration();
            return WaveformMode switch
            {
                SourceWaveformMode.Constant => builder.AddCurrentSource(coreName, ConstantValue),
                SourceWaveformMode.Sinusoidal => builder.AddSinusoidalCurrentSource(
                    coreName, Offset, Amplitude, FrequencyHz, PhaseRadians),
                SourceWaveformMode.Square => builder.AddSquareCurrentSource(
                    coreName, Offset, Amplitude, FrequencyHz, PhaseRadians),
                SourceWaveformMode.Triangle => builder.AddTriangleCurrentSource(
                    coreName, Offset, Amplitude, FrequencyHz, PhaseRadians),
                _ => throw new ArgumentOutOfRangeException(nameof(WaveformMode), WaveformMode, "The source waveform mode is not supported.")
            };
        }
    }
}
