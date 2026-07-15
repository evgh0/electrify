using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Model;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Public component classifications used by circuit context snapshots.</summary>
    public enum CircuitComponentKind
    {
        /// <summary>A resistor.</summary>
        Resistor,
        /// <summary>An independent current source.</summary>
        CurrentSource,
        /// <summary>An independent voltage source.</summary>
        VoltageSource,
        /// <summary>A capacitor.</summary>
        Capacitor,
        /// <summary>An inductor.</summary>
        Inductor,
        /// <summary>A Shockley diode.</summary>
        Diode,
        /// <summary>An LED.</summary>
        Led,
        /// <summary>A switch or button.</summary>
        Switch,
        /// <summary>A readable ideal jumper.</summary>
        Jumper
    }

    /// <summary>One deterministic electrical node in a circuit netlist.</summary>
    public sealed class CircuitNetlistNode
    {
        internal CircuitNetlistNode(string id, bool isGround, IEnumerable<string> componentIds)
        {
            Id = id;
            IsGround = isGround;
            ComponentIds = new ReadOnlyCollection<string>(componentIds.ToArray());
        }

        /// <summary>Gets the context node ID, such as N0.</summary>
        public string Id { get; }
        /// <summary>Gets whether this is the ground node.</summary>
        public bool IsGround { get; }
        /// <summary>Gets IDs of components incident on this node.</summary>
        public IReadOnlyList<string> ComponentIds { get; }
    }

    /// <summary>One component entry in a deterministic circuit netlist.</summary>
    public sealed class CircuitNetlistComponent
    {
        private readonly IReadOnlyDictionary<string, string> parameters;

        internal CircuitNetlistComponent(
            string id,
            string displayName,
            CircuitComponentKind kind,
            string positiveNodeId,
            string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters,
            CircuitComponent component)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            PositiveNodeId = positiveNodeId;
            NegativeNodeId = negativeNodeId;
            this.parameters = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(parameters, StringComparer.Ordinal));
            Component = component;
        }

        /// <summary>Gets the snapshot-local context ID, such as C0.</summary>
        public string Id { get; }
        /// <summary>Gets the user-facing component name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the component classification.</summary>
        public CircuitComponentKind Kind { get; }
        /// <summary>Gets the node connected to terminal 0.</summary>
        public string PositiveNodeId { get; }
        /// <summary>Gets the node connected to terminal 1.</summary>
        public string NegativeNodeId { get; }
        /// <summary>Gets deterministic invariant-culture parameter values.</summary>
        public IReadOnlyDictionary<string, string> Parameters => parameters;
        /// <summary>Gets the mapped Unity circuit component.</summary>
        public CircuitComponent Component { get; }
        /// <summary>Gets the mapped Unity GameObject.</summary>
        public GameObject GameObject => Component.gameObject;
    }

    /// <summary>An immutable, Unity-facing structural circuit snapshot.</summary>
    public sealed class CircuitNetlistSnapshot
    {
        private readonly IReadOnlyDictionary<string, CircuitNetlistComponent> byId;
        private readonly IReadOnlyDictionary<CircuitComponent, string> byComponent;

        internal CircuitNetlistSnapshot(
            IEnumerable<CircuitNetlistNode> nodes,
            IEnumerable<CircuitNetlistComponent> components)
        {
            Nodes = new ReadOnlyCollection<CircuitNetlistNode>(nodes.ToArray());
            Components = new ReadOnlyCollection<CircuitNetlistComponent>(components.ToArray());
            byId = new ReadOnlyDictionary<string, CircuitNetlistComponent>(
                Components.ToDictionary(item => item.Id, StringComparer.Ordinal));
            byComponent = new ReadOnlyDictionary<CircuitComponent, string>(
                Components.ToDictionary(item => item.Component, item => item.Id));
        }

        /// <summary>Gets nodes in deterministic node-ID order.</summary>
        public IReadOnlyList<CircuitNetlistNode> Nodes { get; }
        /// <summary>Gets components in deterministic context-ID order.</summary>
        public IReadOnlyList<CircuitNetlistComponent> Components { get; }

        /// <summary>Resolves a context ID to its Unity component.</summary>
        public CircuitComponent GetComponent(string contextId) => GetEntry(contextId).Component;
        /// <summary>Resolves a context ID to its Unity GameObject.</summary>
        public GameObject GetGameObject(string contextId) => GetEntry(contextId).GameObject;

        /// <summary>Gets the snapshot-local ID assigned to a Unity component.</summary>
        public string GetContextId(CircuitComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (!byComponent.TryGetValue(component, out var id))
            {
                throw new KeyNotFoundException("The component is not part of this netlist snapshot.");
            }

            return id;
        }

        /// <summary>Attempts to resolve a context ID to its Unity component.</summary>
        public bool TryGetComponent(string contextId, out CircuitComponent component)
        {
            component = null;
            if (contextId == null || !byId.TryGetValue(contextId, out var entry))
            {
                return false;
            }

            component = entry.Component;
            return true;
        }

        /// <summary>Attempts to resolve a context ID to its live Unity scene object.</summary>
        /// <param name="contextId">The snapshot-local context ID, such as C0.</param>
        /// <param name="sceneObject">
        /// Receives the mapped scene object, or <see langword="null"/> when the ID is unknown
        /// or the mapped Unity object has been destroyed.
        /// </param>
        /// <returns><see langword="true"/> when a live mapped scene object was found.</returns>
        public bool TryGetSceneObject(string contextId, out GameObject sceneObject)
        {
            sceneObject = null;
            if (!TryGetComponent(contextId, out var component) || component == null)
            {
                return false;
            }

            sceneObject = component.gameObject;
            return sceneObject != null;
        }

        /// <summary>Formats the canonical human- and LLM-readable netlist.</summary>
        public string ToNetlistText()
        {
            var text = new StringBuilder();
            text.AppendLine("NETLIST");
            foreach (var node in Nodes)
            {
                text.Append("NODE ").Append(node.Id);
                if (node.IsGround)
                {
                    text.Append(" GROUND");
                }
                text.Append(" components=[").Append(string.Join(",", node.ComponentIds)).AppendLine("]");
            }

            foreach (var component in Components)
            {
                text.Append("COMPONENT ").Append(component.Id)
                    .Append(" name=\"").Append(Escape(component.DisplayName)).Append('"')
                    .Append(" kind=").Append(component.Kind)
                    .Append(" positive=").Append(component.PositiveNodeId)
                    .Append(" negative=").Append(component.NegativeNodeId);
                foreach (var parameter in component.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    text.Append(' ').Append(parameter.Key).Append('=').Append(parameter.Value);
                }
                text.AppendLine();
            }

            return text.ToString().TrimEnd();
        }

        public override string ToString() => ToNetlistText();

        internal static CircuitNetlistSnapshot Create(
            CompiledCircuit circuit,
            IReadOnlyDictionary<CircuitComponent, ComponentId> componentIds)
        {
            var ordered = componentIds.OrderBy(pair => pair.Value.Value).ToArray();
            var entries = new List<CircuitNetlistComponent>(ordered.Length);
            var nodeComponents = circuit.Netlist.Nodes.ToDictionary(
                node => node.Id.Value,
                _ => new List<string>());

            for (var index = 0; index < ordered.Length; index++)
            {
                var pair = ordered[index];
                var compiled = circuit.GetComponent(pair.Value);
                var id = "C" + index.ToString(CultureInfo.InvariantCulture);
                var positive = compiled.Nodes[0].Value;
                var negative = compiled.Nodes[1].Value;
                entries.Add(new CircuitNetlistComponent(
                    id,
                    pair.Key.DisplayName,
                    GetKind(pair.Key),
                    NodeId(positive),
                    NodeId(negative),
                    GetParameters(pair.Key),
                    pair.Key));
                nodeComponents[positive].Add(id);
                if (negative != positive)
                {
                    nodeComponents[negative].Add(id);
                }
            }

            var nodes = circuit.Netlist.Nodes
                .OrderBy(node => node.Id.Value)
                .Select(node => new CircuitNetlistNode(
                    NodeId(node.Id.Value),
                    node.Id == circuit.Netlist.GroundNodeId,
                    nodeComponents[node.Id.Value]));
            return new CircuitNetlistSnapshot(nodes, entries);
        }

        private CircuitNetlistComponent GetEntry(string contextId)
        {
            if (contextId == null)
            {
                throw new ArgumentNullException(nameof(contextId));
            }

            if (!byId.TryGetValue(contextId, out var entry))
            {
                throw new KeyNotFoundException("Unknown circuit context component ID '" + contextId + "'.");
            }

            return entry;
        }

        private static string NodeId(int value) => "N" + value.ToString(CultureInfo.InvariantCulture);

        private static CircuitComponentKind GetKind(CircuitComponent component)
        {
            if (component is Resistor) return CircuitComponentKind.Resistor;
            if (component is CurrentSource) return CircuitComponentKind.CurrentSource;
            if (component is VoltageSource) return CircuitComponentKind.VoltageSource;
            if (component is Capacitor) return CircuitComponentKind.Capacitor;
            if (component is Inductor) return CircuitComponentKind.Inductor;
            if (component is Diode) return CircuitComponentKind.Diode;
            if (component is Led) return CircuitComponentKind.Led;
            if (component is ControlledSwitchComponent) return CircuitComponentKind.Switch;
            if (component is Jumper) return CircuitComponentKind.Jumper;
            throw new ArgumentException("Unsupported circuit component type " + component.GetType().FullName + ".", nameof(component));
        }

        private static IReadOnlyDictionary<string, string> GetParameters(CircuitComponent component)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (component is Resistor resistor) result.Add("resistance_ohms", Number(resistor.ResistanceOhms));
            else if (component is Capacitor capacitor) result.Add("capacitance_farads", Number(capacitor.CapacitanceFarads));
            else if (component is Inductor inductor) result.Add("inductance_henries", Number(inductor.InductanceHenries));
            else if (component is Diode diode)
            {
                result.Add("saturation_current_amperes", Number(diode.SaturationCurrent));
                result.Add("ideality_factor", Number(diode.IdealityFactor));
                result.Add("thermal_voltage_volts", Number(diode.ThermalVoltage));
            }
            else if (component is Led led)
            {
                result.Add("nominal_forward_voltage_volts", Number(led.NominalForwardVoltage));
                result.Add("reference_current_amperes", Number(led.ReferenceCurrent));
                result.Add("ideality_factor", Number(led.IdealityFactor));
                result.Add("thermal_voltage_volts", Number(led.ThermalVoltage));
            }
            else if (component is IndependentSource source)
            {
                var unit = component is VoltageSource ? "volts" : "amperes";
                result.Add("waveform", source.WaveformMode.ToString());
                if (source.WaveformMode == SourceWaveformMode.Constant)
                {
                    result.Add("value_" + unit, Number(source.ConstantValue));
                }
                else
                {
                    result.Add("offset_" + unit, Number(source.Offset));
                    result.Add("amplitude_" + unit, Number(source.Amplitude));
                    result.Add("frequency_hz", Number(source.FrequencyHz));
                    result.Add("phase_radians", Number(source.PhaseRadians));
                }
            }
            else if (component is ControlledSwitchComponent controlled)
            {
                result.Add("closed", controlled.IsElectricallyClosed ? "true" : "false");
            }
            return result;
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
