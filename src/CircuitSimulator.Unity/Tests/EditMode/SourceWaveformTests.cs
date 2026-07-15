using System;
using CircuitSimulator.Core.Components;
using NUnit.Framework;

namespace CircuitSimulator.Unity.Tests
{
    public sealed class SourceWaveformTests
    {
        [Test]
        public void SquareWaveUsesSymmetricLevelsAndHalfOpenDutyCycle()
        {
            var waveform = new SquareSourceWaveform(2.0, 3.0, 1.0);

            Assert.That(waveform.GetValue(0.0), Is.EqualTo(5.0).Within(1e-12));
            Assert.That(waveform.GetValue(0.25), Is.EqualTo(5.0).Within(1e-12));
            Assert.That(waveform.GetValue(0.5), Is.EqualTo(-1.0).Within(1e-12));
            Assert.That(waveform.GetValue(0.75), Is.EqualTo(-1.0).Within(1e-12));
            Assert.That(waveform.GetValue(1.0), Is.EqualTo(5.0).Within(1e-12));
        }

        [Test]
        public void TriangleWaveStartsAtOffsetAndTraversesBothPeaks()
        {
            var waveform = new TriangleSourceWaveform(2.0, 3.0, 1.0);

            Assert.That(waveform.GetValue(0.0), Is.EqualTo(2.0).Within(1e-12));
            Assert.That(waveform.GetValue(0.25), Is.EqualTo(5.0).Within(1e-12));
            Assert.That(waveform.GetValue(0.5), Is.EqualTo(2.0).Within(1e-12));
            Assert.That(waveform.GetValue(0.75), Is.EqualTo(-1.0).Within(1e-12));
            Assert.That(waveform.GetValue(1.0), Is.EqualTo(2.0).Within(1e-12));
        }

        [Test]
        public void PeriodicPhaseAdvancesSquareAndTriangleWaves()
        {
            var square = new SquareSourceWaveform(0.0, 1.0, 1.0, Math.PI);
            var triangle = new TriangleSourceWaveform(0.0, 1.0, 1.0, Math.PI / 2.0);

            Assert.That(square.GetValue(0.0), Is.EqualTo(-1.0).Within(1e-12));
            Assert.That(triangle.GetValue(0.0), Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void SquareAndTriangleReportOnlyZeroOffsetAndAmplitudeAsAlwaysZero()
        {
            Assert.That(new SquareSourceWaveform(0.0, 0.0, 1.0).IsZeroForAllTime, Is.True);
            Assert.That(new TriangleSourceWaveform(0.0, 0.0, 1.0).IsZeroForAllTime, Is.True);
            Assert.That(new SquareSourceWaveform(1.0, 0.0, 1.0).IsZeroForAllTime, Is.False);
            Assert.That(new TriangleSourceWaveform(0.0, 1.0, 1.0).IsZeroForAllTime, Is.False);
        }

        [Test]
        public void PeriodicWaveformsRejectInvalidParametersAndEvaluationTimes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SquareSourceWaveform(double.NaN, 1.0, 1.0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SquareSourceWaveform(0.0, double.PositiveInfinity, 1.0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TriangleSourceWaveform(0.0, 1.0, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TriangleSourceWaveform(0.0, 1.0, 1.0, double.NegativeInfinity));

            var waveform = new SquareSourceWaveform(0.0, 1.0, 1.0);
            Assert.Throws<ArgumentOutOfRangeException>(() => waveform.GetValue(double.NaN));
        }
    }
}
