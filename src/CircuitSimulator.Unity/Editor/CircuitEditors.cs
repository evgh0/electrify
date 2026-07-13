using System;
using UnityEditor;
using UnityEngine;

namespace CircuitSimulator.Unity.Editor
{
    [CustomEditor(typeof(CircuitSimulation))]
    internal sealed class CircuitSimulationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var simulation = (CircuitSimulation)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State", simulation.State.ToString());
            EditorGUILayout.LabelField("Simulation Time", simulation.CurrentTime.ToString("G8") + " s");
            EditorGUILayout.LabelField("Circuit Dirty", simulation.IsDirty ? "Yes" : "No");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild"))
                {
                    simulation.Rebuild();
                }

                if (GUILayout.Button(simulation.State == CircuitSimulationState.Running ? "Pause" : "Start"))
                {
                    if (simulation.State == CircuitSimulationState.Running)
                    {
                        simulation.PauseSimulation();
                    }
                    else
                    {
                        simulation.StartSimulation();
                    }
                }

                if (GUILayout.Button("Step"))
                {
                    simulation.Step();
                }
            }

            if (simulation.LastFailure != null)
            {
                EditorGUILayout.HelpBox(simulation.LastFailure.Message, MessageType.Error);
            }

            foreach (var issue in simulation.ValidationIssues)
            {
                var messageType = issue.Severity == CircuitDiagnosticSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Code + ": " + issue.Message, messageType);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(simulation);
            }
        }
    }

    [CustomEditor(typeof(CircuitComponent), true)]
    internal sealed class CircuitComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var component = (CircuitComponent)target;
            var source = target as IndependentSource;
            var controlled = target as ControlledSwitchComponent;
            if (source != null)
            {
                DrawPropertiesExcluding(
                    serializedObject,
                    "m_Script",
                    "waveformMode",
                    "constantValue",
                    "offset",
                    "amplitude",
                    "frequencyHz",
                    "phaseRadians");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waveformMode"));
                var mode = (SourceWaveformMode)serializedObject.FindProperty("waveformMode").enumValueIndex;
                if (mode == SourceWaveformMode.Constant)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("constantValue"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("amplitude"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("frequencyHz"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("phaseRadians"));
                }

                serializedObject.ApplyModifiedProperties();
            }
            else if (controlled != null)
            {
                DrawPropertiesExcluding(serializedObject, "m_Script", "isClosed", "normallyClosed");
                serializedObject.ApplyModifiedProperties();
                DrawSwitchControls(controlled);
            }
            else
            {
                DrawDefaultInspector();
            }

            var twoTerminal = component as TwoTerminalCircuitComponent;
            if (twoTerminal != null && (twoTerminal.Positive == null || twoTerminal.Negative == null))
            {
                EditorGUILayout.HelpBox("This component requires positive and negative child terminals.", MessageType.Warning);
                if (GUILayout.Button("Create Missing Terminals"))
                {
                    CreateMissingTerminals(twoTerminal);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Latest Reading", EditorStyles.boldLabel);
            if (component.HasReading)
            {
                EditorGUILayout.LabelField("Voltage", component.Voltage.ToString("G8") + " V");
                EditorGUILayout.LabelField("Current", component.Current.ToString("G8") + " A");
                EditorGUILayout.LabelField("Power", component.Power.ToString("G8") + " W");
            }
            else
            {
                EditorGUILayout.LabelField("No accepted sample");
            }

            if (twoTerminal != null && twoTerminal.Simulation != null)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Delete Component and Connections"))
                {
                    DeleteComponentWithUndo(twoTerminal);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void DrawSwitchControls(ControlledSwitchComponent controlled)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ideal Contact", EditorStyles.boldLabel);

            var circuitSwitch = controlled as CircuitSwitch;
            if (circuitSwitch != null)
            {
                var isClosed = EditorGUILayout.Toggle("Closed", circuitSwitch.IsClosed);
                if (isClosed != circuitSwitch.IsClosed)
                {
                    Undo.RecordObject(circuitSwitch, "Change Circuit Switch State");
                    circuitSwitch.IsClosed = isClosed;
                    EditorUtility.SetDirty(circuitSwitch);
                }

                if (GUILayout.Button(circuitSwitch.IsClosed ? "Open" : "Close"))
                {
                    Undo.RecordObject(circuitSwitch, "Toggle Circuit Switch");
                    circuitSwitch.Toggle();
                    EditorUtility.SetDirty(circuitSwitch);
                }

                return;
            }

            var button = controlled as CircuitButton;
            if (button == null)
            {
                return;
            }

            var normallyClosed = EditorGUILayout.Toggle("Normally Closed", button.NormallyClosed);
            if (normallyClosed != button.NormallyClosed)
            {
                Undo.RecordObject(button, "Change Circuit Button Contact");
                button.NormallyClosed = normallyClosed;
                EditorUtility.SetDirty(button);
            }

            EditorGUILayout.LabelField("Pressed", button.IsPressed ? "Yes" : "No");
            if (GUILayout.Button(button.IsPressed ? "Release" : "Press"))
            {
                if (button.IsPressed)
                {
                    button.Release();
                }
                else
                {
                    button.Press();
                }
            }
        }

        private static void CreateMissingTerminals(TwoTerminalCircuitComponent component)
        {
            Undo.RecordObject(component, "Create Circuit Terminals");
            var positive = component.Positive ?? CreateTerminal(component.transform, "Positive (+)");
            var negative = component.Negative ?? CreateTerminal(component.transform, "Negative (-)");
            component.SetTerminals(positive, negative);
            EditorUtility.SetDirty(component);
        }

        private static CircuitTerminal CreateTerminal(Transform parent, string terminalName)
        {
            var terminalObject = new GameObject(terminalName);
            Undo.RegisterCreatedObjectUndo(terminalObject, "Create Circuit Terminal");
            terminalObject.transform.SetParent(parent, false);
            return Undo.AddComponent<CircuitTerminal>(terminalObject);
        }

        private static void DeleteComponentWithUndo(TwoTerminalCircuitComponent component)
        {
            var simulation = component.Simulation;
            var connections = simulation.GetComponentsInChildren<CircuitConnection>(true);
            for (var index = connections.Length - 1; index >= 0; index--)
            {
                var connection = connections[index];
                if (ReferenceEquals(connection.First, component.Positive) ||
                    ReferenceEquals(connection.First, component.Negative) ||
                    ReferenceEquals(connection.Second, component.Positive) ||
                    ReferenceEquals(connection.Second, component.Negative))
                {
                    Undo.DestroyObjectImmediate(connection.gameObject);
                }
            }

            Undo.DestroyObjectImmediate(component.gameObject);
            simulation.RequestRebuild();
        }
    }

    [CustomEditor(typeof(VoltageProbe))]
    internal sealed class VoltageProbeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var probe = (VoltageProbe)target;

            if (probe.Positive == null || probe.Negative == null)
            {
                EditorGUILayout.HelpBox("Assign positive and negative terminals to measure voltage.", MessageType.Warning);
            }
            else if (ReferenceEquals(probe.Positive, probe.Negative))
            {
                EditorGUILayout.HelpBox("Positive and negative references must be different terminals.", MessageType.Warning);
            }
            else if (probe.Simulation == null ||
                     !ReferenceEquals(probe.Positive.Simulation, probe.Simulation) ||
                     !ReferenceEquals(probe.Negative.Simulation, probe.Simulation))
            {
                EditorGUILayout.HelpBox("Both terminals must belong to this probe's circuit.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Latest Reading", EditorStyles.boldLabel);
            if (probe.HasReading)
            {
                EditorGUILayout.LabelField("Voltage", probe.Voltage.ToString("G8") + " V");
                EditorGUILayout.LabelField("Time", probe.LatestReading.Value.Time.ToString("G8") + " s");
            }
            else
            {
                EditorGUILayout.LabelField("No available sample");
            }
        }
    }

    [CustomEditor(typeof(CurrentProbe))]
    internal sealed class CurrentProbeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var probe = (CurrentProbe)target;

            if (probe.Target == null)
            {
                EditorGUILayout.HelpBox("Assign a component whose signed current should be observed.", MessageType.Warning);
            }
            else if (probe.Simulation == null || !ReferenceEquals(probe.Target.Simulation, probe.Simulation))
            {
                EditorGUILayout.HelpBox("The target component must belong to this probe's circuit.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Latest Reading", EditorStyles.boldLabel);
            if (probe.HasReading)
            {
                EditorGUILayout.LabelField("Current", probe.Current.ToString("G8") + " A");
                EditorGUILayout.LabelField("Time", probe.LatestReading.Value.Time.ToString("G8") + " s");
            }
            else
            {
                EditorGUILayout.LabelField("No available sample");
            }
        }
    }

    [CustomEditor(typeof(CircuitWire))]
    internal sealed class CircuitWireEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var wire = (CircuitWire)target;
            if (wire.First == null || wire.Second == null)
            {
                EditorGUILayout.HelpBox("Assign two terminals before rebuilding the circuit.", MessageType.Error);
            }

            if (GUILayout.Button("Delete Wire"))
            {
                var simulation = wire.Simulation;
                Undo.DestroyObjectImmediate(wire.gameObject);
                simulation?.RequestRebuild();
            }
        }
    }

    internal static class CircuitGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawTerminal(CircuitTerminal terminal, GizmoType gizmoType)
        {
            if (terminal == null)
            {
                return;
            }

            var owner = terminal.Owner;
            Gizmos.color = terminal.IsGround
                ? new Color(1.0f, 0.75f, 0.1f)
                : ReferenceEquals(owner?.Positive, terminal)
                    ? new Color(0.95f, 0.25f, 0.25f)
                    : new Color(0.25f, 0.55f, 1.0f);
            Gizmos.DrawSphere(terminal.transform.position, 0.06f);

            if (terminal.IsGround)
            {
                var position = terminal.transform.position;
                Gizmos.DrawLine(position, position + Vector3.down * 0.18f);
                Gizmos.DrawLine(position + Vector3.down * 0.18f + Vector3.left * 0.08f,
                    position + Vector3.down * 0.18f + Vector3.right * 0.08f);
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawWire(CircuitWire wire, GizmoType gizmoType)
        {
            if (wire == null || wire.First == null || wire.Second == null)
            {
                return;
            }

            Gizmos.color = wire.Simulation == null ? Color.red : new Color(0.75f, 0.8f, 0.9f);
            Gizmos.DrawLine(wire.First.transform.position, wire.Second.transform.position);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawControlledSwitch(ControlledSwitchComponent controlled, GizmoType gizmoType)
        {
            if (controlled == null || controlled.Positive == null || controlled.Negative == null)
            {
                return;
            }

            var positive = controlled.Positive.transform.position;
            var negative = controlled.Negative.transform.position;
            var delta = negative - positive;
            var firstContact = positive + delta * 0.25f;
            var secondContact = positive + delta * 0.75f;
            Gizmos.color = controlled.IsElectricallyClosed
                ? new Color(0.2f, 0.9f, 0.35f)
                : new Color(1.0f, 0.45f, 0.2f);
            Gizmos.DrawLine(positive, firstContact);
            Gizmos.DrawLine(secondContact, negative);
            Gizmos.DrawSphere(firstContact, 0.035f);
            Gizmos.DrawSphere(secondContact, 0.035f);
            Gizmos.DrawLine(
                firstContact,
                controlled.IsElectricallyClosed
                    ? secondContact
                    : Vector3.Lerp(firstContact, secondContact, 0.85f) + Vector3.up * 0.12f);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawJumper(Jumper jumper, GizmoType gizmoType)
        {
            if (jumper == null || jumper.Positive == null || jumper.Negative == null)
            {
                return;
            }

            Gizmos.color = jumper.Simulation == null ? Color.red : new Color(0.25f, 0.9f, 0.85f);
            Gizmos.DrawLine(jumper.Positive.transform.position, jumper.Negative.transform.position);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawVoltageProbe(VoltageProbe probe, GizmoType gizmoType)
        {
            if (probe == null || probe.Positive == null || probe.Negative == null)
            {
                return;
            }

            Gizmos.color = probe.Simulation == null
                ? Color.red
                : new Color(0.95f, 0.75f, 0.2f);
            Gizmos.DrawLine(probe.Positive.transform.position, probe.Negative.transform.position);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawCurrentProbe(CurrentProbe probe, GizmoType gizmoType)
        {
            if (probe == null || probe.Target == null)
            {
                return;
            }

            Gizmos.color = probe.Simulation == null
                ? Color.red
                : new Color(0.75f, 0.35f, 0.95f);
            Gizmos.DrawLine(probe.transform.position, probe.Target.transform.position);
        }
    }
}
