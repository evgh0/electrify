using System;

namespace CircuitSimulator.Unity
{
    /// <summary>One mapped result for a Unity circuit component.</summary>
    public readonly struct CircuitReading
    {
        /// <summary>Creates a component reading using the passive sign convention.</summary>
        public CircuitReading(double time, double voltage, double current)
        {
            Time = time;
            Voltage = voltage;
            Current = current;
            Power = voltage * current;
        }

        /// <summary>Gets simulation time in seconds.</summary>
        public double Time { get; }

        /// <summary>Gets positive-terminal minus negative-terminal voltage in volts.</summary>
        public double Voltage { get; }

        /// <summary>Gets positive-to-negative current in amperes.</summary>
        public double Current { get; }

        /// <summary>Gets instantaneous power in watts; positive values mean absorbed power.</summary>
        public double Power { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "t={0:G6}s, V={1:G6}V, I={2:G6}A, P={3:G6}W",
                Time,
                Voltage,
                Current,
                Power);
        }
    }
}
