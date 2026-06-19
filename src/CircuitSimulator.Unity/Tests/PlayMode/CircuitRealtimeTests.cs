using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CircuitSimulator.Unity.Tests
{
    public sealed class CircuitRealtimeTests
    {
        [UnityTest]
        public IEnumerator ManualRcStepMapsReadingAndRaisesEvent()
        {
            var root = new GameObject("Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 1.0);
            var resistor = simulation.AddResistor("R1", 1.0);
            var capacitor = simulation.AddCapacitor("C1", 1.0);
            simulation.Connect(source.Negative, capacitor.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, capacitor.Positive);
            var events = 0;
            capacitor.ReadingChanged += _ => events++;

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Tick(1.0), Is.EqualTo(1));
            Assert.That(capacitor.HasReading, Is.True);
            Assert.That(capacitor.Voltage, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(capacitor.Current, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(capacitor.Power, Is.EqualTo(0.25).Within(1e-9));
            Assert.That(events, Is.EqualTo(1));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseAndParameterEditResetTheSession()
        {
            var root = new GameObject("Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 1.0);
            var resistor = simulation.AddResistor("R1", 1.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Tick(1.0), Is.EqualTo(1));
            simulation.PauseSimulation();
            Assert.That(simulation.Tick(10.0), Is.Zero);
            resistor.ResistanceOhms = 2.0;
            Assert.That(simulation.ResumeSimulation(), Is.True);
            Assert.That(simulation.CurrentTime, Is.Zero);

            Object.Destroy(root);
            yield return null;
        }
    }
}
