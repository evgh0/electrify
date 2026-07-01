#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>
/// Defines an immutable time-domain independent-source waveform.
/// </summary>
internal abstract record SourceWaveform
{
    /// <summary>Gets whether this waveform is exactly zero for every supported time.</summary>
    internal abstract bool IsZeroForAllTime { get; }

    /// <summary>
    /// Evaluates the waveform at a finite time in seconds.
    /// </summary>
    public abstract double GetValue(double time);

    /// <summary>Validates a waveform evaluation time.</summary>
    protected static void ValidateTime(double time)
    {
        if (!Guard.IsFinite(time))
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Time must be finite.");
        }
    }
}

/// <summary>Defines a constant time-domain source waveform.</summary>
internal sealed record ConstantSourceWaveform : SourceWaveform
{
    /// <summary>Initializes a constant waveform.</summary>
    public ConstantSourceWaveform(double value)
    {
        if (!Guard.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Waveform value must be finite.");
        }

        Value = value;
    }

    /// <summary>Gets the constant waveform value.</summary>
    public double Value { get; }

    /// <inheritdoc />
    internal override bool IsZeroForAllTime => Value == 0.0;

    /// <inheritdoc />
    public override double GetValue(double time)
    {
        ValidateTime(time);
        return Value;
    }
}

/// <summary>Defines an offset sinusoid for time-domain simulation.</summary>
internal sealed record SinusoidalSourceWaveform : SourceWaveform
{
    /// <summary>Initializes a sinusoidal waveform.</summary>
    public SinusoidalSourceWaveform(double offset, double amplitude, double frequencyHz, double phaseRadians = 0.0)
    {
        if (!Guard.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be finite.");
        }

        if (!Guard.IsFinite(amplitude))
        {
            throw new ArgumentOutOfRangeException(nameof(amplitude), amplitude, "Amplitude must be finite.");
        }

        if (!Guard.IsFinite(frequencyHz) || frequencyHz <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(frequencyHz), frequencyHz, "Frequency must be finite and greater than zero.");
        }

        if (!Guard.IsFinite(phaseRadians))
        {
            throw new ArgumentOutOfRangeException(nameof(phaseRadians), phaseRadians, "Phase must be finite.");
        }

        Offset = offset;
        Amplitude = amplitude;
        FrequencyHz = frequencyHz;
        PhaseRadians = phaseRadians;
    }

    /// <summary>Gets the waveform offset.</summary>
    public double Offset { get; }

    /// <summary>Gets the peak amplitude.</summary>
    public double Amplitude { get; }

    /// <summary>Gets the frequency in hertz.</summary>
    public double FrequencyHz { get; }

    /// <summary>Gets the phase in radians.</summary>
    public double PhaseRadians { get; }

    /// <inheritdoc />
    internal override bool IsZeroForAllTime => Offset == 0.0 && Amplitude == 0.0;

    /// <inheritdoc />
    public override double GetValue(double time)
    {
        ValidateTime(time);
        return Offset + (Amplitude * Math.Sin((2.0 * Math.PI * FrequencyHz * time) + PhaseRadians));
    }
}

}
