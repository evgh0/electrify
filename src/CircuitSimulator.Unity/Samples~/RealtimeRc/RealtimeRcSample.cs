using UnityEngine;

namespace CircuitSimulator.Unity.Samples
{
    /// <summary>Self-contained runtime sample for a 5 V RC charging circuit.</summary>
    public sealed class RealtimeRcSample : MonoBehaviour
    {
        private CircuitSimulation simulation;
        private Resistor resistor;
        private Capacitor capacitor;

        private void Start()
        {
            simulation = gameObject.AddComponent<CircuitSimulation>();
            simulation.TimeStep = 1e-3;

            var source = simulation.AddVoltageSource("V1", 5.0);
            resistor = simulation.AddResistor("R1", 1000.0);
            capacitor = simulation.AddCapacitor("C1", 100e-6);

            simulation.Connect(source.Negative, capacitor.Negative);
            simulation.SetGround(source.Negative);
            simulation.Connect(source.Positive, resistor.Positive);
            simulation.Connect(resistor.Negative, capacitor.Positive);

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

            GUI.Label(new Rect(20, 20, 520, 25), "State: " + simulation.State);
            GUI.Label(
                new Rect(20, 45, 520, 25),
                "C1: " + capacitor.Voltage.ToString("F4") + " V, " + capacitor.Current.ToString("F6") + " A");

            if (resistor != null && GUI.Button(new Rect(20, 80, 220, 32), "Set R1 to 2 kOhm"))
            {
                resistor.ResistanceOhms = 2000.0;
            }

            if (resistor != null && GUI.Button(new Rect(20, 120, 220, 32), "Delete R1 and attached wires"))
            {
                simulation.DeleteComponent(resistor);
                resistor = null;
            }

            if (simulation.LastFailure != null)
            {
                GUI.Label(new Rect(20, 165, 700, 40), "Failure: " + simulation.LastFailure.Message);
            }
        }
    }
}
