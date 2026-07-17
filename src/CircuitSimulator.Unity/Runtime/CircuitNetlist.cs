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
    public abstract class CircuitNetlistComponent
    {
        private readonly IReadOnlyDictionary<string, string> parameters;

        internal CircuitNetlistComponent(
            string id,
            string displayName,
            CircuitComponentKind kind,
            string positiveNodeId,
            string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters,
            CircuitComponent component,
            GameObject sceneObject)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            PositiveNodeId = positiveNodeId;
            NegativeNodeId = negativeNodeId;
            this.parameters = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(parameters, StringComparer.Ordinal));
            Component = component;
            GameObject = sceneObject;
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
        /// <summary>
        /// Gets the mapped physical or visual Unity GameObject captured by this snapshot.
        /// </summary>
        public GameObject GameObject { get; }
    }

    /// <summary>An immutable resistor entry in a circuit netlist snapshot.</summary>
    public sealed class ResistorNetlistComponent : CircuitNetlistComponent
    {
        internal ResistorNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CircuitComponent component, GameObject sceneObject,
            double resistanceOhms)
            : base(id, displayName, CircuitComponentKind.Resistor, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            ResistanceOhms = resistanceOhms;
        }

        /// <summary>Gets the captured resistance in ohms.</summary>
        public double ResistanceOhms { get; }
    }

    /// <summary>An immutable capacitor entry in a circuit netlist snapshot.</summary>
    public sealed class CapacitorNetlistComponent : CircuitNetlistComponent
    {
        internal CapacitorNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CircuitComponent component, GameObject sceneObject,
            double capacitanceFarads)
            : base(id, displayName, CircuitComponentKind.Capacitor, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            CapacitanceFarads = capacitanceFarads;
        }

        /// <summary>Gets the captured capacitance in farads.</summary>
        public double CapacitanceFarads { get; }
    }

    /// <summary>An immutable inductor entry in a circuit netlist snapshot.</summary>
    public sealed class InductorNetlistComponent : CircuitNetlistComponent
    {
        internal InductorNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CircuitComponent component, GameObject sceneObject,
            double inductanceHenries)
            : base(id, displayName, CircuitComponentKind.Inductor, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            InductanceHenries = inductanceHenries;
        }

        /// <summary>Gets the captured inductance in henries.</summary>
        public double InductanceHenries { get; }
    }

    /// <summary>An immutable Shockley-diode entry in a circuit netlist snapshot.</summary>
    public sealed class DiodeNetlistComponent : CircuitNetlistComponent
    {
        internal DiodeNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CircuitComponent component, GameObject sceneObject,
            double saturationCurrent, double idealityFactor, double thermalVoltage)
            : base(id, displayName, CircuitComponentKind.Diode, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            SaturationCurrent = saturationCurrent;
            IdealityFactor = idealityFactor;
            ThermalVoltage = thermalVoltage;
        }

        /// <summary>Gets the captured reverse saturation current in amperes.</summary>
        public double SaturationCurrent { get; }
        /// <summary>Gets the captured emission ideality factor.</summary>
        public double IdealityFactor { get; }
        /// <summary>Gets the captured thermal voltage in volts.</summary>
        public double ThermalVoltage { get; }
    }

    /// <summary>An immutable LED entry in a circuit netlist snapshot.</summary>
    public sealed class LedNetlistComponent : CircuitNetlistComponent
    {
        internal LedNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CircuitComponent component, GameObject sceneObject,
            double nominalForwardVoltage, double referenceCurrent, double idealityFactor, double thermalVoltage)
            : base(id, displayName, CircuitComponentKind.Led, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            NominalForwardVoltage = nominalForwardVoltage;
            ReferenceCurrent = referenceCurrent;
            IdealityFactor = idealityFactor;
            ThermalVoltage = thermalVoltage;
        }

        /// <summary>Gets the captured nominal forward voltage in volts.</summary>
        public double NominalForwardVoltage { get; }
        /// <summary>Gets the captured reference forward current in amperes.</summary>
        public double ReferenceCurrent { get; }
        /// <summary>Gets the captured emission ideality factor.</summary>
        public double IdealityFactor { get; }
        /// <summary>Gets the captured thermal voltage in volts.</summary>
        public double ThermalVoltage { get; }
    }

    /// <summary>Common immutable configuration captured for an independent source.</summary>
    public abstract class IndependentSourceNetlistComponent : CircuitNetlistComponent
    {
        internal IndependentSourceNetlistComponent(
            string id, string displayName, CircuitComponentKind kind, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CircuitComponent component, GameObject sceneObject,
            SourceWaveformMode waveformMode, double constantValue, double offset, double amplitude,
            double frequencyHz, double phaseRadians)
            : base(id, displayName, kind, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            WaveformMode = waveformMode;
            ConstantValue = constantValue;
            Offset = offset;
            Amplitude = amplitude;
            FrequencyHz = frequencyHz;
            PhaseRadians = phaseRadians;
        }

        /// <summary>Gets the captured source waveform mode.</summary>
        public SourceWaveformMode WaveformMode { get; }
        /// <summary>Gets the captured constant source value.</summary>
        public double ConstantValue { get; }
        /// <summary>Gets the captured periodic waveform offset.</summary>
        public double Offset { get; }
        /// <summary>Gets the captured periodic waveform peak amplitude.</summary>
        public double Amplitude { get; }
        /// <summary>Gets the captured periodic waveform frequency in hertz.</summary>
        public double FrequencyHz { get; }
        /// <summary>Gets the captured periodic waveform phase in radians.</summary>
        public double PhaseRadians { get; }
    }

    /// <summary>An immutable voltage-source entry in a circuit netlist snapshot.</summary>
    public sealed class VoltageSourceNetlistComponent : IndependentSourceNetlistComponent
    {
        internal VoltageSourceNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, VoltageSource component, GameObject sceneObject)
            : base(id, displayName, CircuitComponentKind.VoltageSource, positiveNodeId, negativeNodeId, parameters, component,
                sceneObject, component.WaveformMode, component.ConstantValue, component.Offset, component.Amplitude,
                component.FrequencyHz, component.PhaseRadians)
        {
        }
    }

    /// <summary>An immutable current-source entry in a circuit netlist snapshot.</summary>
    public sealed class CurrentSourceNetlistComponent : IndependentSourceNetlistComponent
    {
        internal CurrentSourceNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, CurrentSource component, GameObject sceneObject)
            : base(id, displayName, CircuitComponentKind.CurrentSource, positiveNodeId, negativeNodeId, parameters, component,
                sceneObject, component.WaveformMode, component.ConstantValue, component.Offset, component.Amplitude,
                component.FrequencyHz, component.PhaseRadians)
        {
        }
    }

    /// <summary>An immutable ideal-switch or button entry in a circuit netlist snapshot.</summary>
    public sealed class SwitchNetlistComponent : CircuitNetlistComponent
    {
        internal SwitchNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, ControlledSwitchComponent component, GameObject sceneObject)
            : base(id, displayName, CircuitComponentKind.Switch, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
            IsClosed = component.IsElectricallyClosed;
        }

        /// <summary>Gets whether the captured ideal contact is electrically closed.</summary>
        public bool IsClosed { get; }
    }

    /// <summary>An immutable ideal-jumper entry in a circuit netlist snapshot.</summary>
    public sealed class JumperNetlistComponent : CircuitNetlistComponent
    {
        internal JumperNetlistComponent(
            string id, string displayName, string positiveNodeId, string negativeNodeId,
            IReadOnlyDictionary<string, string> parameters, Jumper component, GameObject sceneObject)
            : base(id, displayName, CircuitComponentKind.Jumper, positiveNodeId, negativeNodeId, parameters, component, sceneObject)
        {
        }
    }

    /// <summary>An immutable, Unity-facing structural circuit snapshot.</summary>
    public sealed class CircuitNetlistSnapshot
    {
        private readonly IReadOnlyDictionary<string, CircuitNetlistComponent> byId;
        private readonly IReadOnlyDictionary<CircuitComponent, string> byComponent;
        private readonly IReadOnlyDictionary<string, CircuitNetlistNode> nodesById;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<CircuitNetlistComponent>> componentsByNodeId;
        private readonly IReadOnlyDictionary<CircuitComponent, IReadOnlyList<CircuitNetlistNode>> nodesByComponent;

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
            nodesById = new ReadOnlyDictionary<string, CircuitNetlistNode>(
                Nodes.ToDictionary(item => item.Id, StringComparer.Ordinal));

            var incident = Nodes.ToDictionary(
                item => item.Id,
                _ => new List<CircuitNetlistComponent>(),
                StringComparer.Ordinal);
            var componentNodes = new Dictionary<CircuitComponent, IReadOnlyList<CircuitNetlistNode>>();
            foreach (var component in Components)
            {
                incident[component.PositiveNodeId].Add(component);
                if (!string.Equals(component.PositiveNodeId, component.NegativeNodeId, StringComparison.Ordinal))
                {
                    incident[component.NegativeNodeId].Add(component);
                }

                var connectedNodes = new List<CircuitNetlistNode>
                {
                    nodesById[component.PositiveNodeId]
                };
                if (!string.Equals(component.PositiveNodeId, component.NegativeNodeId, StringComparison.Ordinal))
                {
                    connectedNodes.Add(nodesById[component.NegativeNodeId]);
                }

                componentNodes.Add(
                    component.Component,
                    new ReadOnlyCollection<CircuitNetlistNode>(connectedNodes));
            }

            componentsByNodeId = new ReadOnlyDictionary<string, IReadOnlyList<CircuitNetlistComponent>>(
                incident.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<CircuitNetlistComponent>)new ReadOnlyCollection<CircuitNetlistComponent>(pair.Value),
                    StringComparer.Ordinal));
            nodesByComponent = new ReadOnlyDictionary<CircuitComponent, IReadOnlyList<CircuitNetlistNode>>(componentNodes);
        }

        /// <summary>Gets nodes in deterministic node-ID order.</summary>
        public IReadOnlyList<CircuitNetlistNode> Nodes { get; }
        /// <summary>Gets components in deterministic context-ID order.</summary>
        public IReadOnlyList<CircuitNetlistComponent> Components { get; }

        /// <summary>Returns whether the snapshot contains an entry of the specified type.</summary>
        public bool Contains<TEntry>() where TEntry : CircuitNetlistComponent =>
            Components.OfType<TEntry>().Any();

        /// <summary>Returns whether the snapshot contains an entry matching a typed predicate.</summary>
        public bool Contains<TEntry>(Func<TEntry, bool> predicate) where TEntry : CircuitNetlistComponent =>
            FindAll<TEntry>(predicate).Count != 0;

        /// <summary>Gets all entries of the specified type in deterministic context-ID order.</summary>
        public IReadOnlyList<TEntry> FindAll<TEntry>() where TEntry : CircuitNetlistComponent =>
            new ReadOnlyCollection<TEntry>(Components.OfType<TEntry>().ToArray());

        /// <summary>Gets all typed entries matching a predicate in deterministic context-ID order.</summary>
        public IReadOnlyList<TEntry> FindAll<TEntry>(Func<TEntry, bool> predicate) where TEntry : CircuitNetlistComponent
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return new ReadOnlyCollection<TEntry>(Components.OfType<TEntry>().Where(predicate).ToArray());
        }

        /// <summary>Counts entries of the specified type.</summary>
        public int Count<TEntry>() where TEntry : CircuitNetlistComponent =>
            Components.OfType<TEntry>().Count();

        /// <summary>Counts typed entries matching a predicate.</summary>
        public int Count<TEntry>(Func<TEntry, bool> predicate) where TEntry : CircuitNetlistComponent =>
            FindAll<TEntry>(predicate).Count;

        /// <summary>Gets a node by its snapshot-local ID.</summary>
        public CircuitNetlistNode GetNode(string nodeId)
        {
            if (nodeId == null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            if (!nodesById.TryGetValue(nodeId, out var node))
            {
                throw new KeyNotFoundException("Unknown circuit context node ID '" + nodeId + "'.");
            }

            return node;
        }

        /// <summary>Attempts to get a node by its snapshot-local ID.</summary>
        public bool TryGetNode(string nodeId, out CircuitNetlistNode node)
        {
            node = null;
            return nodeId != null && nodesById.TryGetValue(nodeId, out node);
        }

        /// <summary>Gets the distinct electrical nodes incident to a mapped component.</summary>
        public IReadOnlyList<CircuitNetlistNode> GetNodes(CircuitComponent component) =>
            nodesByComponent[GetEntry(component).Component];

        /// <summary>Gets components incident to a node in deterministic context-ID order.</summary>
        public IReadOnlyList<CircuitNetlistComponent> GetComponentsOnNode(string nodeId) =>
            componentsByNodeId[GetNode(nodeId).Id];

        /// <summary>Gets distinct components that share an electrical node with the supplied component.</summary>
        public IReadOnlyList<CircuitNetlistComponent> GetDirectNeighbors(CircuitComponent component)
        {
            var entry = GetEntry(component);
            var neighbors = new HashSet<CircuitNetlistComponent>();
            foreach (var node in GetNodes(entry.Component))
            {
                foreach (var incident in componentsByNodeId[node.Id])
                {
                    if (!ReferenceEquals(incident, entry))
                    {
                        neighbors.Add(incident);
                    }
                }
            }

            return new ReadOnlyCollection<CircuitNetlistComponent>(
                Components.Where(neighbors.Contains).ToArray());
        }

        /// <summary>Returns whether two distinct mapped components share at least one electrical node.</summary>
        public bool AreDirectlyConnected(CircuitComponent first, CircuitComponent second)
        {
            var firstEntry = GetEntry(first);
            var secondEntry = GetEntry(second);
            if (ReferenceEquals(firstEntry, secondEntry))
            {
                return false;
            }

            return string.Equals(firstEntry.PositiveNodeId, secondEntry.PositiveNodeId, StringComparison.Ordinal) ||
                   string.Equals(firstEntry.PositiveNodeId, secondEntry.NegativeNodeId, StringComparison.Ordinal) ||
                   string.Equals(firstEntry.NegativeNodeId, secondEntry.PositiveNodeId, StringComparison.Ordinal) ||
                   string.Equals(firstEntry.NegativeNodeId, secondEntry.NegativeNodeId, StringComparison.Ordinal);
        }

        /// <summary>Returns whether either terminal of a mapped component is directly connected to ground.</summary>
        public bool IsDirectlyConnectedToGround(CircuitComponent component) =>
            GetNodes(component).Any(node => node.IsGround);

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
            if (contextId == null || !byId.TryGetValue(contextId, out var entry) || entry.GameObject == null)
            {
                return false;
            }

            sceneObject = entry.GameObject;
            return true;
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
                entries.Add(CreateEntry(
                    id,
                    pair.Key,
                    NodeId(positive),
                    NodeId(negative)));
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

        private CircuitNetlistComponent GetEntry(CircuitComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (!byComponent.TryGetValue(component, out var contextId))
            {
                throw new KeyNotFoundException("The component is not part of this netlist snapshot.");
            }

            return byId[contextId];
        }

        private static string NodeId(int value) => "N" + value.ToString(CultureInfo.InvariantCulture);

        private static CircuitNetlistComponent CreateEntry(
            string id,
            CircuitComponent component,
            string positiveNodeId,
            string negativeNodeId)
        {
            var parameters = GetParameters(component);
            var displayName = component.DisplayName;
            var sceneObject = component.SceneObject;

            if (component is Resistor resistor)
            {
                return new ResistorNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, resistor, sceneObject,
                    resistor.ResistanceOhms);
            }

            if (component is Capacitor capacitor)
            {
                return new CapacitorNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, capacitor, sceneObject,
                    capacitor.CapacitanceFarads);
            }

            if (component is Inductor inductor)
            {
                return new InductorNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, inductor, sceneObject,
                    inductor.InductanceHenries);
            }

            if (component is Diode diode)
            {
                return new DiodeNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, diode, sceneObject,
                    diode.SaturationCurrent, diode.IdealityFactor, diode.ThermalVoltage);
            }

            if (component is Led led)
            {
                return new LedNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, led, sceneObject,
                    led.NominalForwardVoltage, led.ReferenceCurrent, led.IdealityFactor, led.ThermalVoltage);
            }

            if (component is VoltageSource voltageSource)
            {
                return new VoltageSourceNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, voltageSource, sceneObject);
            }

            if (component is CurrentSource currentSource)
            {
                return new CurrentSourceNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, currentSource, sceneObject);
            }

            if (component is ControlledSwitchComponent controlledSwitch)
            {
                return new SwitchNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, controlledSwitch, sceneObject);
            }

            if (component is Jumper jumper)
            {
                return new JumperNetlistComponent(
                    id, displayName, positiveNodeId, negativeNodeId, parameters, jumper, sceneObject);
            }

            throw new ArgumentException(
                "Unsupported circuit component type " + component.GetType().FullName + ".",
                nameof(component));
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
