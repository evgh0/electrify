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
        public IEnumerator ProbesPublishOnlyAcceptedSamplesWithoutChangingRcHistory()
        {
            var root = new GameObject("Probed RC Circuit");
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
            var voltageProbe = simulation.AddVoltageProbe(
                "Capacitor voltage",
                capacitor.Positive,
                capacitor.Negative);
            var currentProbe = simulation.AddCurrentProbe("Resistor current", resistor);
            var voltageEvents = 0;
            var currentEvents = 0;
            voltageProbe.ReadingChanged += _ => voltageEvents++;
            currentProbe.ReadingChanged += _ => currentEvents++;

            Assert.That(simulation.Step(), Is.True);
            Assert.That(voltageProbe.Voltage, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(currentProbe.Current, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(voltageProbe.LatestReading.Value.Time, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(currentProbe.LatestReading.Value.Time, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(voltageEvents, Is.EqualTo(1));
            Assert.That(currentEvents, Is.EqualTo(1));

            currentProbe.gameObject.SetActive(false);
            Assert.That(simulation.IsDirty, Is.False);
            Assert.That(currentProbe.HasReading, Is.False);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.CurrentTime, Is.EqualTo(2.0).Within(1e-12));
            Assert.That(capacitor.Voltage, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(voltageProbe.Voltage, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(voltageEvents, Is.EqualTo(2));
            Assert.That(currentEvents, Is.EqualTo(1));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualRlStepMapsInductorReading()
        {
            var root = new GameObject("RL Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 1.0);
            var resistor = simulation.AddResistor("R1", 1.0);
            var inductor = simulation.AddInductor("L1", 1.0);
            simulation.Connect(source.Negative, inductor.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, inductor.Positive);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(inductor.Current, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(simulation.Step(), Is.True);
            Assert.That(inductor.Current, Is.EqualTo(0.75).Within(1e-9));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SinusoidalVoltageSourceEvaluatesAtAcceptedStepTime()
        {
            var root = new GameObject("Sinusoidal Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1.0;
            var source = simulation.AddSinusoidalVoltageSource("V1", 0.0, 1.0, 0.25);
            var load = simulation.AddResistor("R1", 1.0);
            simulation.Connect(source.Negative, load.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, load.Positive);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(load.Voltage, Is.EqualTo(1.0).Within(1e-9));
            Assert.That(load.Current, Is.EqualTo(1.0).Within(1e-9));

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

        [UnityTest]
        public IEnumerator FailedSwitchedCapacitorStepDoesNotCommitTimeOrReading()
        {
            var root = new GameObject("Transactional RC");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1.0;
            var source = simulation.AddVoltageSource("V1", 1.0);
            var resistor = simulation.AddResistor("R1", 1.0);
            var capacitor = simulation.AddCapacitor("C1", 1.0);
            var groundSwitch = simulation.AddSwitch("S1", initiallyClosed: true);
            simulation.Connect(source.Negative, capacitor.Negative);
            simulation.Connect(source.Negative, groundSwitch.Positive);
            simulation.SetGround(groundSwitch.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, capacitor.Positive);
            var voltageProbe = simulation.AddVoltageProbe(
                "Capacitor voltage",
                capacitor.Positive,
                capacitor.Negative);
            var currentProbe = simulation.AddCurrentProbe("Resistor current", resistor);
            var probeEvents = 0;
            voltageProbe.ReadingChanged += _ => probeEvents++;
            currentProbe.ReadingChanged += _ => probeEvents++;

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(capacitor.Voltage, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(voltageProbe.Voltage, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(currentProbe.Current, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(probeEvents, Is.EqualTo(2));

            groundSwitch.Open();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Circuit simulation step failed"));
            Assert.That(simulation.Step(), Is.False);
            Assert.That(simulation.CurrentTime, Is.EqualTo(1.0).Within(1e-9));
            Assert.That(capacitor.Voltage, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(voltageProbe.Voltage, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(currentProbe.Current, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(probeEvents, Is.EqualTo(2));

            groundSwitch.Close();
            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.CurrentTime, Is.EqualTo(2.0).Within(1e-9));
            Assert.That(capacitor.Voltage, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(voltageProbe.Voltage, Is.EqualTo(0.75).Within(1e-9));
            Assert.That(currentProbe.Current, Is.EqualTo(0.25).Within(1e-9));
            Assert.That(probeEvents, Is.EqualTo(4));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumperInSeriesReportsBranchCurrent()
        {
            var root = new GameObject("Jumper Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 1e-3;
            var source = simulation.AddVoltageSource("V1", 5.0);
            var jumper = simulation.AddJumper("J1");
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, jumper.Positive);
            simulation.Connect(jumper.Negative, load.Positive);

            Assert.That(simulation.StartSimulation(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(load.Voltage, Is.EqualTo(5.0).Within(1e-9));
            Assert.That(load.Current, Is.EqualTo(0.005).Within(1e-12));
            Assert.That(jumper.Voltage, Is.Zero.Within(1e-9));
            Assert.That(jumper.Current, Is.EqualTo(0.005).Within(1e-12));
            Assert.That(jumper.Power, Is.Zero.Within(1e-12));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnalyzerReportsThresholdTransitionsAndContextMapping()
        {
            var root = new GameObject("Analyzed Circuit");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            simulation.TimeStep = 0.25;
            var analyzer = root.AddComponent<CircuitAnalyzer>();
            var source = simulation.AddSinusoidalVoltageSource("V1", 0.0, 5.0, 1.0);
            var load = simulation.AddResistor("Load", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, load.Positive);
            analyzer.AddThreshold(load, CircuitMetric.Voltage, CircuitThresholdDirection.Above, 4.0, 1.0);
            analyzer.AddThreshold(load, CircuitMetric.Voltage, CircuitThresholdDirection.Below, -4.0, -1.0);

            Assert.That(simulation.StartSimulation(), Is.True);
            analyzer.ClearHistory();
            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.Step(), Is.True);

            Assert.That(analyzer.RecentEvents.Count, Is.EqualTo(2));
            Assert.That(analyzer.RecentEvents[0].Kind, Is.EqualTo(CircuitAnalysisEventKind.ThresholdEntered));
            Assert.That(analyzer.RecentEvents[1].Kind, Is.EqualTo(CircuitAnalysisEventKind.ThresholdExited));
            Assert.That(simulation.Step(), Is.True);
            Assert.That(simulation.Step(), Is.True);
            Assert.That(analyzer.RecentEvents.Count, Is.EqualTo(4));
            Assert.That(analyzer.RecentEvents[2].Kind, Is.EqualTo(CircuitAnalysisEventKind.ThresholdEntered));
            Assert.That(analyzer.RecentEvents[3].Kind, Is.EqualTo(CircuitAnalysisEventKind.ThresholdExited));

            var context = new CircuitContextBuilder(simulation)
                .WithAnalyzer(analyzer)
                .IncludeRecentEvents(4)
                .Build();
            var loadId = context.Netlist.GetContextId(load);
            Assert.That(context.Events[0].ComponentId, Is.EqualTo(loadId));
            Assert.That(context.Netlist.GetGameObject(loadId), Is.SameAs(load.gameObject));
            Assert.That(context.ToPromptText(), Does.Contain("component=" + loadId));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnalyzerBoundsControlEventHistory()
        {
            var root = new GameObject("Control Events");
            var simulation = root.AddComponent<CircuitSimulation>();
            simulation.AutomaticStepping = false;
            var analyzer = root.AddComponent<CircuitAnalyzer>();
            analyzer.HistoryCapacity = 2;
            var source = simulation.AddVoltageSource("V1", 5.0);
            var circuitSwitch = simulation.AddSwitch("S1", false);
            var load = simulation.AddResistor("R1", 1000.0);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Negative, load.Negative);
            simulation.Connect(source.Positive, circuitSwitch.Positive);
            simulation.Connect(circuitSwitch.Negative, load.Positive);
            Assert.That(simulation.StartSimulation(), Is.True);
            analyzer.ClearHistory();

            circuitSwitch.Close();
            circuitSwitch.Open();
            circuitSwitch.Close();

            Assert.That(analyzer.RecentEvents.Count, Is.EqualTo(2));
            Assert.That(analyzer.RecentEvents[0].Kind, Is.EqualTo(CircuitAnalysisEventKind.ControlStateChanged));
            Assert.That(analyzer.RecentEvents[1].Kind, Is.EqualTo(CircuitAnalysisEventKind.ControlStateChanged));
            Assert.That(analyzer.RecentEvents[1].Message, Does.Contain("closed"));

            Object.Destroy(root);
            yield return null;
        }
    }
}
