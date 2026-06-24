using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

            circuitSwitch.Close();

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
            button.NormallyClosed = true;
            Assert.That(button.IsElectricallyClosed, Is.True);

            button.Press();
            Assert.That(button.IsElectricallyClosed, Is.False);
        }
    }
}
