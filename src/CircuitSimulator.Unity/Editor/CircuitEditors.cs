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
                var messageType = issue.Severity == CircuitSimulator.Core.Validation.ValidationSeverity.Error
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
            var source = target as IndependentSource;
            if (source == null)
            {
                DrawDefaultInspector();
            }
            else
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

            var component = (CircuitComponent)target;
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
    }
}
