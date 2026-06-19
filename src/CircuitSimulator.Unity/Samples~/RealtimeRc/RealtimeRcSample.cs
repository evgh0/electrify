using UnityEngine;

namespace CircuitSimulator.Unity.Samples
{
    /// <summary>Self-contained runtime sample for a 5 V RC charging circuit.</summary>
    public sealed class RealtimeRcSample : MonoBehaviour
    {
        private CircuitSimulation simulation;
        private Resistor resistor;
        private Capacitor capacitor;
        private CircuitSwitch circuitSwitch;
        private CircuitButton dischargeButton;

        private void Start()
        {
            simulation = gameObject.AddComponent<CircuitSimulation>();
            simulation.TimeStep = 1e-3;

            var source = simulation.AddVoltageSource("V1", 5.0);
            resistor = simulation.AddResistor("R1", 1000.0);
            capacitor = simulation.AddCapacitor("C1", 100e-6);
            circuitSwitch = simulation.AddSwitch("S1", initiallyClosed: true);
            dischargeButton = simulation.AddButton("PB1");
            var dischargeResistor = simulation.AddResistor("R2", 1000.0);

            simulation.Connect(source.Negative, capacitor.Negative);
            simulation.Connect(source.Negative, dischargeResistor.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, circuitSwitch.Positive);
            simulation.Connect(circuitSwitch.Negative, capacitor.Positive);
            simulation.Connect(capacitor.Positive, dischargeButton.Positive);
            simulation.Connect(dischargeButton.Negative, dischargeResistor.Positive);

            capacitor.ReadingChanged += reading =>
                Debug.Log("Capacitor: " + reading, capacitor);
            simulation.StartSimulation();
        }

        private void OnGUI()
        {
            if (simulation == null)
            {
                return;
            }

            GUI.Label(
                new Rect(20, 20, 700, 25),
                "State: " + simulation.State + ", time: " + simulation.CurrentTime.ToString("F3") + " s");
            GUI.Label(
                new Rect(20, 45, 520, 25),
                "C1: " + capacitor.Voltage.ToString("F4") + " V, " + capacitor.Current.ToString("F6") + " A");

            if (circuitSwitch != null && GUI.Button(
                    new Rect(20, 80, 220, 32),
                    circuitSwitch.IsClosed ? "Open S1 (no reset)" : "Close S1 (no reset)"))
            {
                circuitSwitch.Toggle();
            }

            var buttonHeld = dischargeButton != null &&
                GUI.RepeatButton(new Rect(20, 120, 220, 32), "Hold PB1 to discharge");
            if (dischargeButton != null)
            {
                if (buttonHeld)
                {
                    dischargeButton.Press();
                }
                else
                {
                    dischargeButton.Release();
                }
            }

            if (resistor != null && GUI.Button(new Rect(20, 160, 220, 32), "Set R1 to 2 kOhm"))
            {
                resistor.ResistanceOhms = 2000.0;
            }

            if (resistor != null && GUI.Button(new Rect(20, 200, 220, 32), "Delete R1 and attached wires"))
            {
                simulation.DeleteComponent(resistor);
                resistor = null;
            }

            if (simulation.LastFailure != null)
            {
                GUI.Label(new Rect(20, 245, 700, 40), "Failure: " + simulation.LastFailure.Message);
            }
        }
    }
}
