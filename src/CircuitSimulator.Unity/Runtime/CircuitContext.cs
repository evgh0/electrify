using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CircuitSimulator.Unity
{
    /// <summary>An analysis event copied into a circuit context snapshot.</summary>
    public sealed class CircuitContextEvent
    {
        internal CircuitContextEvent(CircuitAnalysisEvent source, string componentId)
        {
            Time = source.Time;
            Kind = source.Kind;
            Message = source.Message;
            ComponentId = componentId;
            Component = source.Component;
            Metric = source.Metric;
            Value = source.Value;
        }

        /// <summary>Gets event simulation time.</summary>
        public double Time { get; }
        /// <summary>Gets the event classification.</summary>
        public CircuitAnalysisEventKind Kind { get; }
        /// <summary>Gets the event description.</summary>
        public string Message { get; }
        /// <summary>Gets the current snapshot ID of the component, when still present.</summary>
        public string ComponentId { get; }
        /// <summary>Gets the original Unity component reference, when applicable.</summary>
        public CircuitComponent Component { get; }
        /// <summary>Gets the observed metric, when applicable.</summary>
        public CircuitMetric? Metric { get; }
        /// <summary>Gets the signed observed value, when applicable.</summary>
        public double? Value { get; }
    }

    /// <summary>An immutable LLM-oriented snapshot of circuit structure, readings, and events.</summary>
    public sealed class CircuitContextSnapshot
    {
        private readonly IReadOnlyDictionary<string, CircuitReading> readings;

        internal CircuitContextSnapshot(
            CircuitNetlistSnapshot netlist,
            IReadOnlyDictionary<string, CircuitReading> readings,
            IEnumerable<CircuitContextEvent> events)
        {
            Netlist = netlist ?? throw new ArgumentNullException(nameof(netlist));
            this.readings = new ReadOnlyDictionary<string, CircuitReading>(
                new Dictionary<string, CircuitReading>(readings, StringComparer.Ordinal));
            Events = new ReadOnlyCollection<CircuitContextEvent>(events.ToArray());
        }

        /// <summary>Gets the structural netlist and Unity object mappings.</summary>
        public CircuitNetlistSnapshot Netlist { get; }
        /// <summary>Gets readings copied when this snapshot was built.</summary>
        public IReadOnlyDictionary<string, CircuitReading> Readings => readings;
        /// <summary>Gets copied recent analysis events, oldest first.</summary>
        public IReadOnlyList<CircuitContextEvent> Events { get; }

        /// <summary>Formats deterministic context text suitable for an LLM prompt.</summary>
        public string ToPromptText()
        {
            var text = new StringBuilder();
            text.AppendLine("CIRCUIT CONTEXT");
            text.AppendLine("CONVENTIONS terminal 0 is positive/reference; voltage=Vpositive-Vnegative; positive current flows positive-to-negative; N0 is ground.");
            text.AppendLine();
            text.AppendLine(Netlist.ToNetlistText());

            if (readings.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("READINGS");
                foreach (var component in Netlist.Components)
                {
                    if (readings.TryGetValue(component.Id, out var reading))
                    {
                        text.Append(component.Id)
                            .Append(" time_s=").Append(Number(reading.Time))
                            .Append(" voltage_v=").Append(Number(reading.Voltage))
                            .Append(" current_a=").Append(Number(reading.Current))
                            .Append(" power_w=").Append(Number(reading.Power))
                            .AppendLine();
                    }
                }
            }

            if (Events.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("RECENT EVENTS oldest-to-newest");
                foreach (var item in Events)
                {
                    text.Append("time_s=").Append(Number(item.Time))
                        .Append(" kind=").Append(item.Kind);
                    if (item.ComponentId != null)
                    {
                        text.Append(" component=").Append(item.ComponentId);
                    }
                    text.Append(" message=\"").Append(Escape(item.Message)).AppendLine("\"");
                }
            }

            return text.ToString().TrimEnd();
        }

        public override string ToString() => ToPromptText();

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>Fluent builder for end-user circuit context snapshots.</summary>
    public sealed class CircuitContextBuilder
    {
        private readonly CircuitSimulation simulation;
        private CircuitAnalyzer analyzer;
        private bool includeReadings = true;
        private int recentEventCount = 32;

        /// <summary>Creates a context builder for a circuit simulation.</summary>
        public CircuitContextBuilder(CircuitSimulation simulation)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }

        /// <summary>Includes recent history from an analyser.</summary>
        public CircuitContextBuilder WithAnalyzer(CircuitAnalyzer analyzer)
        {
            if (analyzer != null && analyzer.Simulation != null && !ReferenceEquals(analyzer.Simulation, simulation))
            {
                throw new ArgumentException("The analyser belongs to a different circuit simulation.", nameof(analyzer));
            }
            this.analyzer = analyzer;
            return this;
        }

        /// <summary>Controls whether available component readings are copied.</summary>
        public CircuitContextBuilder IncludeReadings(bool include = true)
        {
            includeReadings = include;
            return this;
        }

        /// <summary>Sets the maximum number of newest analysis events to copy.</summary>
        public CircuitContextBuilder IncludeRecentEvents(int maximumCount)
        {
            if (maximumCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Event count cannot be negative.");
            }
            recentEventCount = maximumCount;
            return this;
        }

        /// <summary>Builds a new immutable context snapshot.</summary>
        public CircuitContextSnapshot Build()
        {
            var netlist = simulation.GetNetlist();
            var readings = new Dictionary<string, CircuitReading>(StringComparer.Ordinal);
            if (includeReadings)
            {
                foreach (var entry in netlist.Components)
                {
                    if (entry.Component.LatestReading.HasValue)
                    {
                        readings.Add(entry.Id, entry.Component.LatestReading.Value);
                    }
                }
            }

            var contextEvents = new List<CircuitContextEvent>();
            if (analyzer != null && recentEventCount > 0)
            {
                var source = analyzer.RecentEvents;
                var start = Math.Max(0, source.Count - recentEventCount);
                for (var index = start; index < source.Count; index++)
                {
                    var item = source[index];
                    string id = null;
                    if (item.Component != null)
                    {
                        try
                        {
                            id = netlist.GetContextId(item.Component);
                        }
                        catch (KeyNotFoundException)
                        {
                            // Preserve historical events for components no longer present.
                        }
                    }
                    contextEvents.Add(new CircuitContextEvent(item, id));
                }
            }

            return new CircuitContextSnapshot(netlist, readings, contextEvents);
        }
    }
}
