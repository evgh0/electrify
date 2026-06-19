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

        [UnityTest]
        public IEnumerator SwitchChangesPreserveRealtimeHistoryAndReadings()
        {
            var root = new GameObject("Switched RC");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 1.0);
            var resistor = simulation.AddResistor("R1", 1.0);
            var circuitSwitch = simulation.AddSwitch("S1", initiallyClosed: true);
            var capacitor = simulation.AddCapacitor("C1", 1.0);
            simulation.Connect(source.Negative, capacitor.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, circuitSwitch.Positive);
            simulation.Connect(circuitSwitch.Negative, capacitor.Positive);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Tick(1.0), Is.EqualTo(1));
            Assert.That(capacitor.Voltage, Is.EqualTo(0.5).Within(1e-9));

            circuitSwitch.Open();
            Assert.That(simulation.IsDirty, Is.False);
            Assert.That(capacitor.HasReading, Is.True);
            Assert.That(simulation.Tick(1.0), Is.EqualTo(1));
            Assert.That(capacitor.Voltage, Is.EqualTo(0.5).Within(1e-9));

            circuitSwitch.Close();
            Assert.That(simulation.Tick(1.0), Is.EqualTo(1));
            Assert.That(capacitor.Voltage, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(simulation.CurrentTime, Is.EqualTo(3.0).Within(1e-9));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MomentaryButtonAppliesOnTheNextStepWithoutReset()
        {
            var root = new GameObject("Button Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 0.1;
            var source = simulation.AddVoltageSource("V1", 5.0);
            var button = simulation.AddButton("PB1");
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.Connect(source.Negative, load.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, button.Positive);
            simulation.Connect(button.Negative, load.Positive);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(load.Voltage, Is.Zero.Within(1e-9));

            button.Press();
            Assert.That(simulation.Step(), Is.True);
            Assert.That(load.Voltage, Is.EqualTo(5.0).Within(1e-9));

            button.Release();
            Assert.That(simulation.Step(), Is.True);
            Assert.That(load.Voltage, Is.Zero.Within(1e-9));
            Assert.That(simulation.CurrentTime, Is.EqualTo(0.3).Within(1e-9));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosingAfterSingularOpenStateRetriesTheSameSession()
        {
            var root = new GameObject("Recoverable Switch");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 0.25;
            var source = simulation.AddVoltageSource("V1", 1.0);
            var resistor = simulation.AddResistor("R1", 1.0);
            var circuitSwitch = simulation.AddSwitch("S1", initiallyClosed: true);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Negative, circuitSwitch.Positive);
            simulation.SetGround(circuitSwitch.Negative);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            circuitSwitch.Open();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation step failed"));
            Assert.That(simulation.Step(), Is.False);
            Assert.That(simulation.CurrentTime, Is.EqualTo(0.25).Within(1e-9));

            circuitSwitch.Close();
            Assert.That(simulation.IsDirty, Is.False);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.CurrentTime, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(simulation.LastFailure, Is.Null);

            Object.Destroy(root);
            yield return null;
        }
    }
}
