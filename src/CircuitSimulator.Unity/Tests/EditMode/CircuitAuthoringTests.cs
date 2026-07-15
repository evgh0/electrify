using System;
using System.Linq;
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
            voltageSource.SetSquareVoltage(2.0, 3.0, 30.0, 0.75);
            Assert.That(voltageSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Square));
            Assert.That(voltageSource.Offset, Is.EqualTo(2.0));
            Assert.That(voltageSource.Amplitude, Is.EqualTo(3.0));
            Assert.That(voltageSource.FrequencyHz, Is.EqualTo(30.0));
            Assert.That(voltageSource.PhaseRadians, Is.EqualTo(0.75));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            voltageSource.SetTriangleVoltage(3.0, 4.0, 15.0, 1.0);
            Assert.That(voltageSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Triangle));
            Assert.That(voltageSource.Offset, Is.EqualTo(3.0));
            Assert.That(voltageSource.Amplitude, Is.EqualTo(4.0));
            Assert.That(voltageSource.FrequencyHz, Is.EqualTo(15.0));
            Assert.That(voltageSource.PhaseRadians, Is.EqualTo(1.0));
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

            Assert.That(simulation.Rebuild(), Is.True);
            currentSource.SetSquareCurrent(0.003, 0.004, 90.0, 1.25);
            Assert.That(currentSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Square));
            Assert.That(currentSource.Offset, Is.EqualTo(0.003));
            Assert.That(currentSource.Amplitude, Is.EqualTo(0.004));
            Assert.That(currentSource.FrequencyHz, Is.EqualTo(90.0));
            Assert.That(currentSource.PhaseRadians, Is.EqualTo(1.25));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.That(simulation.Rebuild(), Is.True);
            currentSource.SetTriangleCurrent(0.005, 0.006, 45.0, 1.5);
            Assert.That(currentSource.WaveformMode, Is.EqualTo(SourceWaveformMode.Triangle));
            Assert.That(currentSource.Offset, Is.EqualTo(0.005));
            Assert.That(currentSource.Amplitude, Is.EqualTo(0.006));
            Assert.That(currentSource.FrequencyHz, Is.EqualTo(45.0));
            Assert.That(currentSource.PhaseRadians, Is.EqualTo(1.5));
            Assert.That(simulation.IsDirty, Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => voltageSource.SetVoltage(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => voltageSource.SetSinusoidalVoltage(0.0, 1.0, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => voltageSource.SetSquareVoltage(0.0, 1.0, -1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => voltageSource.SetTriangleVoltage(0.0, 1.0, 1.0, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => currentSource.SetCurrent(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => currentSource.SetSinusoidalCurrent(0.0, 1.0, -1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => currentSource.SetSquareCurrent(0.0, double.NaN, 1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => currentSource.SetTriangleCurrent(0.0, 1.0, 0.0));

            Assert.That(simulation.GetNetlist().ToNetlistText(), Does.Contain("waveform=Triangle"));
            Assert.That(simulation.GetNetlist().ToNetlistText(), Does.Contain("phase_radians=1.5"));
        }

        [Test]
        public void SquareAndTriangleFactoriesConfigureVoltageAndCurrentSources()
        {
            var squareVoltage = simulation.AddSquareVoltageSource("Square V", 1.0, 2.0, 3.0, 0.25);
            var triangleVoltage = simulation.AddTriangleVoltageSource("Triangle V", 4.0, 5.0, 6.0, 0.5);
            var squareCurrent = simulation.AddSquareCurrentSource("Square I", 7.0, 8.0, 9.0, 0.75);
            var triangleCurrent = simulation.AddTriangleCurrentSource("Triangle I", 10.0, 11.0, 12.0, 1.0);

            Assert.That(squareVoltage.WaveformMode, Is.EqualTo(SourceWaveformMode.Square));
            Assert.That(squareVoltage.Offset, Is.EqualTo(1.0));
            Assert.That(triangleVoltage.WaveformMode, Is.EqualTo(SourceWaveformMode.Triangle));
            Assert.That(triangleVoltage.Amplitude, Is.EqualTo(5.0));
            Assert.That(squareCurrent.WaveformMode, Is.EqualTo(SourceWaveformMode.Square));
            Assert.That(squareCurrent.FrequencyHz, Is.EqualTo(9.0));
            Assert.That(triangleCurrent.WaveformMode, Is.EqualTo(SourceWaveformMode.Triangle));
            Assert.That(triangleCurrent.PhaseRadians, Is.EqualTo(1.0));
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

        [TestCase(SourceWaveformMode.Square)]
        [TestCase(SourceWaveformMode.Triangle)]
        public void NewPeriodicVoltageSourceSelfLoopProducesStructuredFailure(SourceWaveformMode mode)
        {
            var source = simulation.AddVoltageSource("V1", 0.0);
            if (mode == SourceWaveformMode.Square)
            {
                source.SetSquareVoltage(0.0, 5.0, 1.0);
            }
            else
            {
                source.SetTriangleVoltage(0.0, 5.0, 1.0);
            }

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
        public void ProbeFactoriesDoNotDirtySessionOrEnterNetlist()
        {
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 5.0);
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, load.Positive);

            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.CurrentTime, Is.EqualTo(1.0).Within(1e-12));
            var originalNetlist = simulation.GetNetlist().ToNetlistText();

            var voltageProbe = simulation.AddVoltageProbe(
                "Load voltage",
                load.Positive,
                load.Negative);
            var currentProbe = simulation.AddCurrentProbe("Load current", load);

            Assert.That(simulation.IsDirty, Is.False);
            Assert.That(voltageProbe.HasReading, Is.False);
            Assert.That(currentProbe.HasReading, Is.False);
            Assert.That(simulation.GetNetlist().ToNetlistText(), Is.EqualTo(originalNetlist));

            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.CurrentTime, Is.EqualTo(2.0).Within(1e-12));
            Assert.That(voltageProbe.Voltage, Is.EqualTo(5.0).Within(1e-12));
            Assert.That(currentProbe.Current, Is.EqualTo(0.005).Within(1e-12));
            Assert.That(simulation.GetNetlist().Components.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProbeRetargetingIsLiveAndPreservesSignedConventions()
        {
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 5.0);
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, load.Positive);
            var voltageProbe = simulation.AddVoltageProbe(
                "Voltage",
                source.Positive,
                source.Negative);
            var currentProbe = simulation.AddCurrentProbe("Current", load);

            Assert.That(simulation.Step(), Is.True);
            Assert.That(voltageProbe.Voltage, Is.EqualTo(5.0).Within(1e-12));
            Assert.That(currentProbe.Current, Is.EqualTo(0.005).Within(1e-12));

            voltageProbe.SetTerminals(source.Negative, source.Positive);
            currentProbe.SetTarget(source);

            Assert.That(simulation.IsDirty, Is.False);
            Assert.That(voltageProbe.HasReading, Is.False);
            Assert.That(currentProbe.HasReading, Is.False);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.CurrentTime, Is.EqualTo(2.0).Within(1e-12));
            Assert.That(voltageProbe.Voltage, Is.EqualTo(-5.0).Within(1e-12));
            Assert.That(currentProbe.Current, Is.EqualTo(-0.005).Within(1e-12));

            voltageProbe.SetTerminals(load.Positive, source.Positive);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(voltageProbe.Voltage, Is.Zero.Within(1e-12));
            Assert.Throws<ArgumentException>(() =>
                voltageProbe.SetTerminals(source.Positive, source.Positive));
            Assert.Throws<ArgumentNullException>(() => currentProbe.SetTarget(null));
        }

        [Test]
        public void CrossCircuitProbeTargetsRemainUnavailableWithoutFaultingCircuit()
        {
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 5.0);
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, load.Positive);

            var foreignRoot = new GameObject("Foreign Circuit");
            try
            {
                var foreignSimulation = foreignRoot.AddComponent<CircuitSimulation>();
                foreignSimulation.AutomaticStepping = false;
                var foreignComponent = foreignSimulation.AddResistor("Foreign", 1.0);
                var voltageProbe = simulation.AddVoltageProbe(
                    "Foreign voltage",
                    source.Positive,
                    foreignComponent.Negative);
                var currentProbe = simulation.AddCurrentProbe("Foreign current", foreignComponent);

                Assert.That(simulation.Step(), Is.True);
                Assert.That(simulation.State, Is.Not.EqualTo(CircuitSimulationState.Faulted));
                Assert.That(voltageProbe.HasReading, Is.False);
                Assert.That(currentProbe.HasReading, Is.False);
                Assert.That(source.HasReading, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(foreignRoot);
            }
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

        [Test]
        public void NetlistProvidesDeterministicNodesTextAndUnityMappings()
        {
            var source = simulation.AddVoltageSource("Source \"A\"", 5.0);
            var resistor = simulation.AddResistor("Load", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);

            var first = simulation.GetNetlist();
            var second = simulation.GetNetlist();

            Assert.That(first.Nodes[0].Id, Is.EqualTo("N0"));
            Assert.That(first.Nodes[0].IsGround, Is.True);
            Assert.That(first.Components.Count, Is.EqualTo(2));
            Assert.That(first.ToNetlistText(), Is.EqualTo(second.ToNetlistText()));
            Assert.That(first.ToNetlistText(), Does.Contain("name=\"Source \\\"A\\\"\""));
            Assert.That(first.ToNetlistText(), Does.Contain("resistance_ohms=1000"));

            var resistorId = first.GetContextId(resistor);
            Assert.That(first.GetComponent(resistorId), Is.SameAs(resistor));
            Assert.That(first.GetGameObject(resistorId), Is.SameAs(resistor.gameObject));
            Assert.That(first.TryGetComponent(resistorId, out var mapped), Is.True);
            Assert.That(mapped, Is.SameAs(resistor));
            Assert.That(first.TryGetSceneObject(resistorId, out var sceneObject), Is.True);
            Assert.That(sceneObject, Is.SameAs(resistor.gameObject));
            Assert.That(first.TryGetSceneObject("unknown", out sceneObject), Is.False);
            Assert.That(sceneObject, Is.Null);
            Assert.That(first.TryGetSceneObject(null, out sceneObject), Is.False);
            Assert.That(sceneObject, Is.Null);
        }

        [Test]
        public void TryGetSceneObjectReturnsFalseAfterMappedObjectIsDestroyed()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);

            var netlist = simulation.GetNetlist();
            var resistorId = netlist.GetContextId(resistor);

            Object.DestroyImmediate(resistor.gameObject);

            Assert.That(netlist.TryGetSceneObject(resistorId, out var sceneObject), Is.False);
            Assert.That(sceneObject, Is.Null);
        }

        [Test]
        public void NetlistMapsComponentToExplicitSceneObjectAndCapturesItPerSnapshot()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            var firstVisual = new GameObject("First resistor visual");
            var secondVisual = new GameObject("Second resistor visual");
            firstVisual.transform.SetParent(root.transform, false);
            secondVisual.transform.SetParent(root.transform, false);

            resistor.SetSceneObject(firstVisual);
            var first = simulation.GetNetlist();
            var resistorId = first.GetContextId(resistor);
            resistor.SetSceneObject(secondVisual);
            var second = simulation.GetNetlist();

            Assert.That(first.GetGameObject(resistorId), Is.SameAs(firstVisual));
            Assert.That(first.TryGetSceneObject(resistorId, out var firstTarget), Is.True);
            Assert.That(firstTarget, Is.SameAs(firstVisual));
            Assert.That(second.GetGameObject(resistorId), Is.SameAs(secondVisual));

            resistor.SetSceneObject(null);
            Assert.That(simulation.GetNetlist().GetGameObject(resistorId), Is.SameAs(resistor.gameObject));
        }

        [Test]
        public void TryGetSceneObjectReturnsFalseAfterExplicitSceneObjectIsDestroyed()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            var visual = new GameObject("Resistor visual");
            resistor.SetSceneObject(visual);
            var netlist = simulation.GetNetlist();
            var resistorId = netlist.GetContextId(resistor);

            Object.DestroyImmediate(visual);

            Assert.That(netlist.TryGetSceneObject(resistorId, out var sceneObject), Is.False);
            Assert.That(sceneObject, Is.Null);
        }

        [Test]
        public void ContextBuilderCreatesStablePromptAndDefensiveReadingSnapshot()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            Assert.That(simulation.Step(), Is.True);

            var context = new CircuitContextBuilder(simulation).Build();
            var resistorId = context.Netlist.GetContextId(resistor);
            var captured = context.Readings[resistorId];

            source.SetVoltage(2.0);
            Assert.That(simulation.Step(), Is.True);

            Assert.That(captured.Voltage, Is.EqualTo(5.0).Within(1e-9));
            Assert.That(context.Readings[resistorId].Voltage, Is.EqualTo(5.0).Within(1e-9));
            Assert.That(context.ToPromptText(), Does.Contain("CIRCUIT CONTEXT"));
            Assert.That(context.ToPromptText(), Does.Contain("component").Or.Contain("COMPONENT"));
            Assert.That(context.ToPromptText(), Does.Contain("voltage_v=5"));
        }

        [Test]
        public void ContextBuilderCanBuildReturnsFalseForEmptyCircuitWithoutThrowing()
        {
            var builder = new CircuitContextBuilder(simulation);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation rebuild failed"));

            Assert.That(builder.CanBuild(), Is.False);
        }

        [Test]
        public void ContextBuilderCanBuildReturnsTrueForValidCircuit()
        {
            var source = simulation.AddVoltageSource("V1", 5.0);
            var resistor = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, resistor.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            var builder = new CircuitContextBuilder(simulation);

            Assert.That(builder.CanBuild(), Is.True);
            Assert.DoesNotThrow(() => builder.Build());
        }

        [Test]
        public void AnalyzerRecordsCompilationFailureAndRejectsInvalidRules()
        {
            var analyzer = root.AddComponent<CircuitAnalyzer>();
            analyzer.SetSimulation(simulation);
            var resistor = simulation.AddResistor("R1", 1000.0);

            Assert.Throws<ArgumentOutOfRangeException>(() => analyzer.AddThreshold(
                resistor, CircuitMetric.Voltage, CircuitThresholdDirection.Above, double.NaN, 1.0));
            Assert.Throws<ArgumentException>(() => analyzer.AddThreshold(
                resistor, CircuitMetric.Voltage, CircuitThresholdDirection.Above, 1.0, 2.0));

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation rebuild failed"));
            Assert.That(simulation.Rebuild(), Is.False);
            Assert.That(
                analyzer.RecentEvents,
                Has.Some.Matches<CircuitAnalysisEvent>(item => item.Kind == CircuitAnalysisEventKind.SimulationFailure));
        }

        [Test]
        public void NetlistClassifiesEverySupportedComponentKind()
        {
            var source = simulation.AddVoltageSource("V", 5.0);
            var resistor = simulation.AddResistor("R", 1000.0);
            var capacitor = simulation.AddCapacitor("C", 1e-6);
            var inductor = simulation.AddInductor("L", 1e-3);
            var diode = simulation.AddDiode("D");
            var led = simulation.AddLed("LED");
            var current = simulation.AddCurrentSource("I", 0.001);
            var circuitSwitch = simulation.AddSwitch("S", true);
            var jumper = simulation.AddJumper("J");
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, capacitor.Positive);
            simulation.Connect(capacitor.Negative, inductor.Positive);
            simulation.Connect(inductor.Negative, diode.Positive);
            simulation.Connect(diode.Negative, led.Positive);
            simulation.Connect(led.Negative, current.Positive);
            simulation.Connect(current.Negative, circuitSwitch.Positive);
            simulation.Connect(circuitSwitch.Negative, jumper.Positive);
            simulation.Connect(jumper.Negative, source.Negative);

            var kinds = new System.Collections.Generic.HashSet<CircuitComponentKind>(
                simulation.GetNetlist().Components.Select(component => component.Kind));

            Assert.That(kinds, Is.EquivalentTo(new[]
            {
                CircuitComponentKind.Resistor,
                CircuitComponentKind.CurrentSource,
                CircuitComponentKind.VoltageSource,
                CircuitComponentKind.Capacitor,
                CircuitComponentKind.Inductor,
                CircuitComponentKind.Diode,
                CircuitComponentKind.Led,
                CircuitComponentKind.Switch,
                CircuitComponentKind.Jumper
            }));
        }
    }
}
