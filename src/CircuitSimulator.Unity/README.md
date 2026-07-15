# Circuit Simulator Unity Helper

`com.evgh.circuit-simulator` is a Unity 6 package that maps scene components to an embedded realtime circuit model and exposes voltage, current, and power readings without exposing MNA data. It also provides observation-only voltage and component-current probes. Use topology-only `CircuitWire` objects for ordinary nets and readable `Jumper` components for ideal shorts whose branch current should be observed.

## Install

Add the repository as a Git dependency with the package path:

```text
https://github.com/evgh0/electrify.git?path=/src/CircuitSimulator.Unity
```

The consuming project must use Unity 6000.0 or later and the .NET Standard 2.1 API compatibility level. The simulator source and dense linear solver are compiled directly with the package, with no managed plug-in dependency.

## Minimal runtime circuit

```csharp
var simulation = gameObject.AddComponent<CircuitSimulation>();
var source = simulation.AddVoltageSource("V1", 5.0);
var jumper = simulation.AddJumper("J1");
var resistor = simulation.AddResistor("R1", 1_000.0);
var circuitSwitch = simulation.AddSwitch("S1", initiallyClosed: true);

simulation.SetGround(source.Negative);
simulation.Connect(source.Negative, resistor.Negative);
simulation.Connect(source.Positive, jumper.Positive);
simulation.Connect(jumper.Negative, circuitSwitch.Positive);
simulation.Connect(circuitSwitch.Negative, resistor.Positive);

var voltageProbe = simulation.AddVoltageProbe("Load voltage", resistor.Positive, resistor.Negative);
var currentProbe = simulation.AddCurrentProbe("Load current", resistor);

resistor.ReadingChanged += reading => Debug.Log(reading.Power);
voltageProbe.ReadingChanged += reading => Debug.Log(reading.Voltage);
currentProbe.ReadingChanged += reading => Debug.Log(reading.Current);
simulation.StartSimulation();
```

Inspector-authored devices register with the nearest enabled `CircuitSimulation` in their parent hierarchy. Every two-terminal device uses terminal 0 as positive/reference and terminal 1 as negative. Positive current flows from positive to negative.

Runtime parameter, wiring, enable-state, hierarchy, and deletion changes mark the circuit dirty. The next numerical step rebuilds the embedded circuit model, resets simulation time and reactive history, and clears existing readings.

Electrical values can be changed at runtime with explicit setters such as `resistor.SetResistance(2000.0)`, `source.SetVoltage(5.0)`, and `circuitSwitch.SetClosed(true)`. Switch and button contact changes apply live on the next numerical step without resetting the current session.

Independent voltage and current sources support constant, sinusoidal, symmetric square, and symmetric triangle waveforms. Periodic modes share offset, peak amplitude, frequency, and phase-in-radians parameters. Square waves have a fixed 50% duty cycle and begin at `offset + amplitude` at zero phase. Triangle waves begin at the offset while rising. Runtime code can create them with factories such as `AddSquareVoltageSource` and `AddTriangleCurrentSource`, or change existing sources with the corresponding `Set...Voltage` and `Set...Current` methods.

`CircuitSimulation.AddJumper(name, first, second)` creates a readable ideal jumper and connects its positive endpoint to `first` and negative endpoint to `second`; positive current is reported in that direction. `CircuitSwitch.Open`, `Close`, and `Toggle`, plus `CircuitButton.Press` and `Release`, are live control operations. They apply on the next numerical step without rebuilding or clearing state. Buttons are normally open by default and can be configured as normally closed.

`CircuitSimulation.AddVoltageProbe(name, positive, negative)` measures `Vpositive - Vnegative` between two existing terminals. `CircuitSimulation.AddCurrentProbe(name, target)` mirrors the target component's established positive-to-negative current. Probe readings and events update after accepted steps, but probes never enter the compiled topology or LLM netlist. Adding, removing, disabling, or retargeting a probe does not dirty or restart the simulation. An unavailable or cross-circuit target clears only that probe's reading. Current at an arbitrary point inside an ideal-wire node is not uniquely defined; insert a readable `Jumper` when an explicit series branch must be measured.

## LLM circuit context and world mapping

The context API lets an assistant reason about circuit topology and live state while retaining a safe mapping back into the Unity world. It consists of:

- `CircuitNetlistSnapshot` for deterministic topology and configuration;
- `CircuitAnalyzer` for meaningful lifecycle, control, and electrical events;
- `CircuitContextBuilder` for immutable LLM-ready snapshots and canonical prompt text.

### Netlist snapshots

`CircuitSimulation.GetNetlist()` returns a deterministic immutable snapshot. If the circuit is dirty, it first attempts a rebuild and throws `InvalidOperationException` with the circuit failure as its inner exception when compilation is unsuccessful.

```csharp
CircuitNetlistSnapshot netlist = simulation.GetNetlist();

foreach (CircuitNetlistNode node in netlist.Nodes)
{
    Debug.Log($"{node.Id}, ground={node.IsGround}");
}

foreach (CircuitNetlistComponent entry in netlist.Components)
{
    Debug.Log($"{entry.Id}: {entry.DisplayName} ({entry.Kind}), " +
              $"positive={entry.PositiveNodeId}, negative={entry.NegativeNodeId}");
}

Debug.Log(netlist.ToNetlistText());
```

Nodes are named `N0`, `N1`, and so on, with `N0` always representing ground. Components receive snapshot-local `C0`, `C1`, and similar IDs. Each component entry contains its display name, kind, positive and negative nodes, invariant-culture SI parameters, and direct references to its `CircuitComponent` and `GameObject`.

```csharp
string id = netlist.GetContextId(resistor);
CircuitComponent component = netlist.GetComponent(id);
GameObject worldObject = netlist.GetGameObject(id);

if (netlist.TryGetComponent("C2", out CircuitComponent optionalComponent))
{
    Debug.Log(optionalComponent.DisplayName);
}
```

### Translating context components to scene objects

Keep the context snapshot submitted to the assistant and resolve returned `C<n>` IDs against that same snapshot:

```csharp
if (submittedContext.Netlist.TryGetSceneObject(componentId, out GameObject sceneObject))
{
    assistantAvatar.position = sceneObject.transform.position;
    assistantAvatar.LookAt(sceneObject.transform);
}
```

The method returns `false` for a null or unknown ID and when the mapped object has been destroyed. Translate in the opposite direction with `submittedContext.Netlist.GetContextId(sceneObject.GetComponent<CircuitComponent>())`. Context IDs are snapshot-local and can be reassigned by a rebuild; electrical node IDs such as `N0` do not identify individual scene objects.

Use `C<n>` IDs in the LLM exchange rather than display names, because names may be duplicated or changed. IDs are deterministic for the same compiled hierarchy but are not persistent identifiers: rebuilding after hierarchy or topology changes may assign different IDs. The netlist describes structure and configured parameters; it does not run an additional DC operating-point solve.

### Circuit analysis events

Associate a `CircuitAnalyzer` with the simulation and configure only the thresholds that are meaningful for the application:

```csharp
var analyzer = gameObject.AddComponent<CircuitAnalyzer>();
analyzer.SetSimulation(simulation);
analyzer.HistoryCapacity = 64;

CircuitThresholdRule rule = analyzer.AddThreshold(
    resistor,
    CircuitMetric.Power,
    CircuitThresholdDirection.Above,
    enterValue: 0.1,
    exitValue: 0.08);

analyzer.EventRecorded += item =>
    Debug.Log($"[{item.Time:F3}s] {item.Kind}: {item.Message}");
```

The analyser records successful rebuilds, simulation state changes and failures, switch/button contact changes, and configured voltage/current/power transitions. Values are signed using package conventions. For an `Above` rule, the condition enters at or above `enterValue` and exits at or below `exitValue`; the exit value must not exceed the enter value. A `Below` rule uses the inverse relationship. This explicit hysteresis prevents repeated events when a value fluctuates around a limit.

History is ordered oldest-to-newest and capped by `HistoryCapacity`. `RemoveThreshold(rule)` stops observing one rule, while `ClearHistory()` clears events without removing rules. A successful circuit rebuild resets active threshold state.

### Building immutable prompt context

Combine structure, latest accepted readings, and recent events with `CircuitContextBuilder`:

```csharp
var context = new CircuitContextBuilder(simulation)
    .WithAnalyzer(analyzer)
    .IncludeReadings(true)
    .IncludeRecentEvents(16)
    .Build();

string promptContext = context.ToPromptText();
```

Readings are included by default. Pass `false` to `IncludeReadings` for topology-only context or `0` to `IncludeRecentEvents` to omit history. The builder works without an analyser when only the netlist and readings are required.

The resulting `CircuitContextSnapshot` defensively copies readings and events. Continuing to simulate does not modify already-submitted context. `ToPromptText()` uses invariant culture and emits deterministic conventions, netlist, readings, and chronological event sections. Tell the LLM to return the exact `C<n>` ID with any component it expects a world avatar to locate.

### Complete assistant/avatar example

This component creates an example circuit and prompt. The consuming application supplies the actual LLM request and passes the returned `componentId` to `LocateComponentMentionedByAssistant`.

```csharp
using CircuitSimulator.Unity;
using UnityEngine;

public sealed class CircuitAssistantExample : MonoBehaviour
{
    [SerializeField] private Transform assistantAvatar;

    private CircuitContextSnapshot submittedContext;

    private void Start()
    {
        var simulation = gameObject.AddComponent<CircuitSimulation>();
        simulation.AutomaticStepping = false;
        simulation.TimeStep = 1e-3;

        var analyzer = gameObject.AddComponent<CircuitAnalyzer>();
        analyzer.SetSimulation(simulation);
        analyzer.HistoryCapacity = 64;

        VoltageSource source = simulation.AddVoltageSource("Supply", 5.0);
        CircuitSwitch powerSwitch = simulation.AddSwitch("Power switch", true);
        Resistor load = simulation.AddResistor("Load resistor", 1_000.0);

        simulation.SetGround(source.Negative);
        simulation.Connect(source.Negative, load.Negative);
        simulation.Connect(source.Positive, powerSwitch.Positive);
        simulation.Connect(powerSwitch.Negative, load.Positive);

        analyzer.AddThreshold(
            load,
            CircuitMetric.Power,
            CircuitThresholdDirection.Above,
            enterValue: 0.020,
            exitValue: 0.015);

        simulation.StartSimulation();
        simulation.Step();

        submittedContext = new CircuitContextBuilder(simulation)
            .WithAnalyzer(analyzer)
            .IncludeReadings()
            .IncludeRecentEvents(16)
            .Build();

        string promptContext = submittedContext.ToPromptText();
        Debug.Log(promptContext);

        // Send promptContext through your LLM SDK and request a structured
        // componentId field whenever the response refers to a world component.
    }

    public bool LocateComponentMentionedByAssistant(string componentId)
    {
        if (submittedContext == null ||
            !submittedContext.Netlist.TryGetComponent(componentId, out CircuitComponent component) ||
            component == null)
        {
            return false;
        }

        assistantAvatar.position = component.transform.position;
        assistantAvatar.LookAt(component.transform);
        return true;
    }
}
```

Resolve a response against the same `CircuitContextSnapshot` that was submitted. If the circuit was rebuilt while the LLM request was in flight, its IDs may no longer describe the new topology. If a mapped Unity object has since been destroyed, build and submit fresh context.

An abbreviated canonical prompt looks like this:

```text
CIRCUIT CONTEXT
CONVENTIONS ...

NETLIST
NODE N0 GROUND components=[C0,C2]
COMPONENT C2 name="Load resistor" kind=Resistor positive=N2 negative=N0 resistance_ohms=1000

READINGS
C2 time_s=0.001 voltage_v=5 current_a=0.005 power_w=0.025

RECENT EVENTS oldest-to-newest
time_s=0.001 kind=ThresholdEntered component=C2 message="Load resistor entered Power threshold ..."
```

See `Documentation~/getting-started.md` and the Realtime RC sample for the full lifecycle and failure behavior.
