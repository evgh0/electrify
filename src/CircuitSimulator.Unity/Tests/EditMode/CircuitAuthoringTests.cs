using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CircuitSimulator.Unity.Tests
{
    public sealed class CircuitAuthoringTests
    {
        private GameObject root;
        private CircuitSimulation simulation;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Circuit");
            simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.PauseSimulation();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void TypedObjectsCompileAndParameterChangesMarkDirty()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(simulation.IsDirty, Is.False);

            resistor.ResistanceOhms = 2000.0;

            Assert.That(simulation.IsDirty, Is.True);
        }

        [Test]
        public void ExplicitPassiveSettersUpdateValuesMarkDirtyAndValidateInput()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            var capacitor = simulation.AddCapacitor("C1", 1e-6);
            var inductor = simulation.AddInductor("L1", 1e-3);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Negative, capacitor.Negative);
            simulation.Connect(source.Negative, inductor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(source.Positive, capacitor.Positive);
            simulation.Connect(source.Positive, inductor.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(simulation.IsDirty, Is.False);

            resistor.SetResistance(2000.0);
            Assert.That(resistor.ResistanceOhms, Is.EqualTo(2000.0));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            capacitor.SetCapacitance(2e-6);
            Assert.That(capacitor.CapacitanceFarads, Is.EqualTo(2e-6));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            inductor.SetInductance(2e-3);
            Assert.That(inductor.InductanceHenries, Is.EqualTo(2e-3));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => resistor.SetResistance(0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => capacitor.SetCapacitance(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => inductor.SetInductance(double.PositiveInfinity));
        }

        [Test]
        public void ExplicitSourceSettersUpdateWaveformsMarkDirtyAndValidateInput()
        {
            var voltageSource = simulation.AddVoltageSource("V1", 5.0);
            var currentSource = simulation.AddCurrentSource("I1", 0.001);
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(voltageSource.Negative);
            simulation.Connect(voltageSource.Negative, currentSource.Negative);
            simulation.Connect(voltageSource.Negative, load.Negative);
            simulation.Connect(voltageSource.Positive, currentSource.Positive);
            simulation.Connect(voltageSource.Positive, load.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(simulation.IsDirty, Is.False);

            voltageSource.SetVoltage(3.3);
            Assert.That(voltageSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Constant));
            Assert.That(voltageSource.ConstantValue, Is.EqualTo(3.3));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            voltageSource.SetSinusoidalVoltage(1.0, 2.0, 60.0, 0.25);
            Assert.That(voltageSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Sinusoidal));
            Assert.That(voltageSource.Offset, Is.EqualTo(1.0));
            Assert.That(voltageSource.Amplitude, Is.EqualTo(2.0));
            Assert.That(voltageSource.FrequencyHz, Is.EqualTo(60.0));
            Assert.That(voltageSource.PhaseRadians, Is.EqualTo(0.25));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            currentSource.SetCurrent(0.002);
            Assert.That(currentSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Constant));
            Assert.That(currentSource.ConstantValue, Is.EqualTo(0.002));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            currentSource.SetSinusoidalCurrent(0.001, 0.002, 120.0, 0.5);
            Assert.That(currentSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Sinusoidal));
            Assert.That(currentSource.Offset, Is.EqualTo(0.001));
            Assert.That(currentSource.Amplitude, Is.EqualTo(0.002));
            Assert.That(currentSource.FrequencyHz, Is.EqualTo(120.0));
            Assert.That(currentSource.PhaseRadians, Is.EqualTo(0.5));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => voltageSource.SetVoltage(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => voltageSource.SetSinusoidalVoltage(0.0, 1.0, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => currentSource.SetCurrent(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => currentSource.SetSinusoidalCurrent(0.0, 1.0, -1.0));
        }

        [Test]
        public void ExplicitDiodeAndLedSettersUpdateValuesMarkDirtyAndValidateInput()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var diode = simulation.AddDiode("D1");
            var led = simulation.AddLed("LED1");
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, diode.Negative);
            simulation.Connect(source.Negative, led.Negative);
            simulation.Connect(source.Positive, diode.Positive);
            simulation.Connect(source.Positive, led.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(simulation.IsDirty, Is.False);

            diode.SetSaturationCurrent(2e-12);
            Assert.That(diode.SaturationCurrent, Is.EqualTo(2e-12));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            diode.SetIdealityFactor(1.5);
            Assert.That(diode.IdealityFactor, Is.EqualTo(1.5));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            diode.SetThermalVoltage(0.026);
            Assert.That(diode.ThermalVoltage, Is.EqualTo(0.026));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            diode.SetModel(3e-12, 1.8, 0.027);
            Assert.That(diode.SaturationCurrent, Is.EqualTo(3e-12));
            Assert.That(diode.IdealityFactor, Is.EqualTo(1.8));
            Assert.That(diode.ThermalVoltage, Is.EqualTo(0.027));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            led.SetNominalForwardVoltage(2.1);
            Assert.That(led.NominalForwardVoltage, Is.EqualTo(2.1));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            led.SetReferenceCurrent(0.015);
            Assert.That(led.ReferenceCurrent, Is.EqualTo(0.015));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            led.SetIdealityFactor(2.2);
            Assert.That(led.IdealityFactor, Is.EqualTo(2.2));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            led.SetThermalVoltage(0.026);
            Assert.That(led.ThermalVoltage, Is.EqualTo(0.026));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            led.SetModel(2.2, 0.012, 2.4, 0.027);
            Assert.That(led.NominalForwardVoltage, Is.EqualTo(2.2));
            Assert.That(led.ReferenceCurrent, Is.EqualTo(0.012));
            Assert.That(led.IdealityFactor, Is.EqualTo(2.4));
            Assert.That(led.ThermalVoltage, Is.EqualTo(0.027));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => diode.SetSaturationCurrent(0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => diode.SetModel(1e-12, double.NaN, 0.026));
            Assert.Throws<ArgumentOutOfRangeException>(() => led.SetNominalForwardVoltage(0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => led.SetModel(2.0, 0.02, 2.0, double.PositiveInfinity));
        }

        [Test]
        public void MissingGroundProducesStructuredFailure()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation rebuild failed"));

            Assert.That(simulation.Rebuild(), Is.False);
            Assert.That(simulation.State, Is.EqualTo(CircuitSimulationState.Faulted));
            Assert.That(simulation.ValidationIssues, Is.Not.Empty);
        }

        [Test]
        public void ConstantVoltageSourceSelfLoopProducesStructuredFailure()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            simulation.Connect(source.Positive, source.Negative);
            simulation.SetGround(source.Negative);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation rebuild failed"));

            Assert.That(simulation.Rebuild(), Is.False);
            Assert.That(simulation.State, Is.EqualTo(CircuitSimulationState.Faulted));
            Assert.That(
                simulation.ValidationIssues,
                Has.Some.Matches<CircuitDiagnostic>(
                    issue => issue.Code == "VOLTAGE_SOURCE_SELF_LOOP_NONZERO"));
        }

        [Test]
        public void SinusoidalVoltageSourceSelfLoopProducesStructuredFailure()
        {
            var source = simulation.AddSinusoidalVoltageSource("V1", 0.0, 5.0, 1.0);
            simulation.Connect(source.Positive, source.Negative);
            simulation.SetGround(source.Negative);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation rebuild failed"));

            Assert.That(simulation.Rebuild(), Is.False);
            Assert.That(simulation.State, Is.EqualTo(CircuitSimulationState.Faulted));
            Assert.That(
                simulation.ValidationIssues,
                Has.Some.Matches<CircuitDiagnostic>(
                    issue => issue.Code == "VOLTAGE_SOURCE_SELF_LOOP_NONZERO"));
        }

        [Test]
        public void DeleteComponentCascadesAttachedWires()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            var wire = simulation.Connect(source.Positive, resistor.Positive);

            simulation.DeleteComponent(resistor);

            Assert.That(resistor == null, Is.True);
            Assert.That(wire == null, Is.True);
            Assert.That(simulation.IsDirty, Is.True);
        }

        [Test]
        public void SwitchFactoryChangesLiveStateWithoutMarkingTopologyDirty()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var circuitSwitch = simulation.AddSwitch("S1");
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, circuitSwitch.Positive);
            simulation.Connect(circuitSwitch.Negative, load.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(circuitSwitch.IsClosed, Is.False);

            circuitSwitch.SetClosed(true);

            Assert.That(circuitSwitch.IsClosed, Is.True);
            Assert.That(simulation.IsDirty, Is.False);
        }

        [Test]
        public void ButtonsSupportNormallyOpenAndNormallyClosedContacts()
        {
            var button = simulation.AddButton("PB1");
            Assert.That(button.IsElectricallyClosed, Is.False);

            button.Press();
            Assert.That(button.IsPressed, Is.True);
            Assert.That(button.IsElectricallyClosed, Is.True);

            button.Release();
            button.SetNormallyClosed(true);
            Assert.That(button.IsElectricallyClosed, Is.True);

            button.Press();
            Assert.That(button.IsElectricallyClosed, Is.False);
        }

        [Test]
        public void ButtonNormalStateSetterChangesLiveStateWithoutMarkingTopologyDirty()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var button = simulation.AddButton("PB1");
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, button.Positive);
            simulation.Connect(button.Negative, load.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(button.IsElectricallyClosed, Is.False);

            button.SetNormallyClosed(true);

            Assert.That(button.NormallyClosed, Is.True);
            Assert.That(button.IsElectricallyClosed, Is.True);
            Assert.That(simulation.IsDirty, Is.False);
        }

        [Test]
        public void LedFactoryPublishesElectricalReadingAndParameterChangesMarkDirty()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 150.0);
            var led = simulation.AddLed("LED1");
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, led.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, led.Positive);

            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(led.HasReading, Is.True);
            Assert.That(double.IsFinite(led.Voltage), Is.True);
            Assert.That(double.IsFinite(led.Current), Is.True);
            Assert.That(led.Current, Is.GreaterThan(0.0));
            Assert.That(simulation.IsDirty, Is.False);

            led.NominalForwardVoltage = 2.1;

            Assert.That(simulation.IsDirty, Is.True);
        }

        [Test]
        public void JumperFactoryCreatesReadableComponentAndRebuilds()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var jumper = simulation.AddJumper("J1");
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, jumper.Positive);
            simulation.Connect(jumper.Negative, load.Positive);

            Assert.That(jumper.Positive, Is.Not.Null);
            Assert.That(jumper.Negative, Is.Not.Null);
            Assert.That(simulation.Rebuild(), Is.True);
            Assert.That(simulation.IsDirty, Is.False);
        }

        [Test]
        public void JumperEndpointFactoryCreatesAttachedConnections()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var load = simulation.AddResistor("R1", 1000.0);
            var jumper = simulation.AddJumper("J1", source.Positive, load.Positive);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);

            var wires = simulation.GetComponentsInChildren<CircuitWire>(true);

            Assert.That(jumper, Is.Not.Null);
            Assert.That(wires.Length, Is.EqualTo(3));
            Assert.That(simulation.Rebuild(), Is.True);
        }

        [Test]
        public void JumperSelfLoopProducesStructuredFailure()
        {
            var jumper = simulation.AddJumper("J1");
            simulation.Connect(jumper.Positive, jumper.Negative);
            simulation.SetGround(jumper.Positive);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation rebuild failed"));

            Assert.That(simulation.Rebuild(), Is.False);
            Assert.That(simulation.State, Is.EqualTo(CircuitSimulationState.Faulted));
            Assert.That(
                simulation.ValidationIssues,
                Has.Some.Matches<CircuitDiagnostic>(
                    issue => issue.Code == "MNA_SINGULAR_SYSTEM"));
        }

        [Test]
        public void DeleteJumperCascadesAttachedWires()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var jumper = simulation.AddJumper("J1");
            var load = simulation.AddResistor("R1", 1000.0);
            var firstWire = simulation.Connect(source.Positive, jumper.Positive);
            var secondWire = simulation.Connect(jumper.Negative, load.Positive);

            simulation.DeleteComponent(jumper);

            Assert.That(jumper == null, Is.True);
            Assert.That(firstWire == null, Is.True);
            Assert.That(secondWire == null, Is.True);
            Assert.That(simulation.IsDirty, Is.True);
        }
    }
}
