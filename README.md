# Circuit Simulator Unity Package

This repository now ships a Unity-only realtime electrical circuit simulator package.

Install it through Unity Package Manager with the package path:

```text
https://github.com/evgh0/electrify.git?path=/src/CircuitSimulator.Unity
```

The package targets Unity 6000.0 or later with the .NET Standard 2.1 API compatibility level. The numerical engine is compiled as package source under `src/CircuitSimulator.Unity/Runtime/Core`, including an in-package dense linear solver.

## Supported Runtime Features

- typed two-terminal resistors, capacitors, inductors, voltage sources, current sources, Shockley diodes, LEDs, ideal switches, momentary buttons, and readable ideal jumpers;
- constant, sinusoidal, square, and triangle independent-source waveforms;
- terminal/wire authoring with one or more ground markers;
- frame-driven fixed-step backward-Euler realtime simulation;
- live switch and button state changes without recompiling or resetting reactive history;
- mapped voltage, current, and power readings on Unity components;
- observation-only voltage and component-current probes that do not alter or restart the circuit;
- deterministic LLM-oriented netlists and context snapshots that map component IDs back to GameObjects;
- bounded circuit analysis events for lifecycle, control, and electrical threshold transitions;
- structured rebuild and simulation diagnostics;
- editor inspectors, gizmos, Unity tests, and a realtime RC sample.

Every two-terminal device uses terminal 0 as positive/reference and terminal 1 as negative:

```text
Vcomponent = Vpositive - Vnegative
```

Positive component current flows from positive to negative. Power readings use the passive sign convention, so positive power is absorbed by the component.

## Minimal Example

```csharp
var simulation = gameObject.AddComponent<CircuitSimulation>();
simulation.TimeStep = 1e-3;

var source = simulation.AddVoltageSource("V1", 5.0);
var resistor = simulation.AddResistor("R1", 1_000.0);
var capacitor = simulation.AddCapacitor("C1", 100e-6);
var jumper = simulation.AddJumper("J1");

simulation.Connect(source.Negative, capacitor.Negative);
simulation.SetGround(source.Negative);
simulation.Connect(source.Positive, jumper.Positive);
simulation.Connect(jumper.Negative, resistor.Positive);
simulation.Connect(resistor.Negative, capacitor.Positive);

var voltageProbe = simulation.AddVoltageProbe("Capacitor voltage", capacitor.Positive, capacitor.Negative);
var currentProbe = simulation.AddCurrentProbe("Resistor current", resistor);

capacitor.ReadingChanged += reading => Debug.Log(reading.Voltage);
voltageProbe.ReadingChanged += reading => Debug.Log(reading.Voltage);
currentProbe.ReadingChanged += reading => Debug.Log(reading.Current);
simulation.StartSimulation();

var analyzer = gameObject.AddComponent<CircuitAnalyzer>();
analyzer.SetSimulation(simulation);
analyzer.AddThreshold(resistor, CircuitMetric.Power, CircuitThresholdDirection.Above, 0.1, 0.08);

var context = new CircuitContextBuilder(simulation)
    .WithAnalyzer(analyzer)
    .Build();
Debug.Log(context.ToPromptText());

// Resolve an ID mentioned by an assistant, then locate its world object.
GameObject mentionedObject = context.Netlist.GetGameObject("C1");
```

`CircuitSimulation.Tick(seconds)` advances a fixed-step accumulator and is called automatically from `Update` when automatic stepping is enabled. `Step()` accepts exactly one numerical step, including while paused. Use `CircuitWire` for topology-only ideal connections and `Jumper` when the ideal short itself needs voltage, current, power, or `ReadingChanged` results.

`VoltageProbe` observes the signed voltage between any two compiled terminals. `CurrentProbe` mirrors the signed positive-to-negative current already calculated for an existing component. Probes are observation-only: adding, removing, or retargeting one does not rebuild the circuit, reset simulation time, or change reactive history. A missing, disabled, destroyed, or cross-circuit target makes only that probe unavailable. Current through an arbitrary point in an ideal-wire node is not uniquely defined; use a readable `Jumper` when the circuit needs an explicit measurable series branch.

Runtime component values can be edited with explicit setters such as `resistor.SetResistance(2000.0)`, `source.SetVoltage(5.0)`, and `circuitSwitch.SetClosed(true)`.

Periodic voltage and current sources support sinusoidal, symmetric square, and symmetric triangle modes. They share offset, peak amplitude, frequency, and phase-in-radians parameters. Square waves use a fixed 50% duty cycle; at zero phase they begin at `offset + amplitude`. Triangle waves begin at the offset while rising.

## Building LLM context

The context API is designed for assistants that need to reason about a circuit and then refer back to physical objects in a Unity scene. A context has three layers:

1. `CircuitNetlistSnapshot` describes the compiled topology and component configuration.
2. `CircuitAnalyzer` records meaningful changes instead of forcing the LLM to compare every simulation sample.
3. `CircuitContextBuilder` combines the netlist, copied readings, and recent events into an immutable snapshot and deterministic prompt string.

### Getting and inspecting a netlist

Call `CircuitSimulation.GetNetlist()` after authoring the circuit. If the circuit is dirty, the call attempts a rebuild. It throws `InvalidOperationException` with the compilation failure as its inner exception if no valid netlist can be produced.

```csharp
CircuitNetlistSnapshot netlist = simulation.GetNetlist();

foreach (CircuitNetlistNode node in netlist.Nodes)
{
    Debug.Log($"{node.Id}, ground={node.IsGround}, components={string.Join(",", node.ComponentIds)}");
}

foreach (CircuitNetlistComponent entry in netlist.Components)
{
    Debug.Log($"{entry.Id}: {entry.DisplayName} ({entry.Kind}), " +
              $"positive={entry.PositiveNodeId}, negative={entry.NegativeNodeId}");
}

string canonicalNetlist = netlist.ToNetlistText();
```

Nodes use deterministic IDs `N0`, `N1`, and so on; `N0` is always ground. Components use snapshot-local IDs `C0`, `C1`, and so on. The component entries include display name, kind, positive and negative nodes, SI-valued parameters, and direct Unity mappings.

```csharp
string resistorId = netlist.GetContextId(resistor);
CircuitComponent mappedComponent = netlist.GetComponent(resistorId);
GameObject mappedObject = netlist.GetGameObject(resistorId);

if (netlist.TryGetComponent("C2", out CircuitComponent optionalComponent))
{
    Debug.Log(optionalComponent.DisplayName);
}
```

Use context IDs, rather than display names, in the LLM protocol. Display names can be duplicated or edited. Context IDs are deterministic for the same compiled hierarchy, but a topology or hierarchy rebuild can reassign them; do not store them as persistent save-game identifiers.

The netlist is structural. It describes topology and configured device values but does not perform an additional DC operating-point solve. Live electrical results come from the latest accepted realtime simulation sample.

### Recording circuit events

Add a `CircuitAnalyzer` to the simulation object or one of its children and associate it with the simulation. It records:

- successful circuit rebuilds;
- simulation state changes and failures;
- switch and button contact changes;
- configured voltage, current, and power threshold transitions.

```csharp
var analyzer = gameObject.AddComponent<CircuitAnalyzer>();
analyzer.SetSimulation(simulation);
analyzer.HistoryCapacity = 64;

CircuitThresholdRule highPower = analyzer.AddThreshold(
    resistor,
    CircuitMetric.Power,
    CircuitThresholdDirection.Above,
    enterValue: 0.020,
    exitValue: 0.015);

analyzer.EventRecorded += item =>
    Debug.Log($"[{item.Time:F3}s] {item.Kind}: {item.Message}");
```

Thresholds use signed readings. Voltage is `Vpositive - Vnegative`, current is positive from the positive terminal to the negative terminal, and power follows the passive sign convention. `Above` enters when the value reaches `enterValue` and exits when it falls to `exitValue`; therefore its exit value must not exceed its enter value. `Below` uses the inverse relationship. Separate values provide hysteresis and prevent noisy samples near a boundary from producing repeated events.

History is retained oldest-to-newest and automatically discards the oldest item when `HistoryCapacity` is exceeded. Use `RemoveThreshold(rule)` to stop observing a rule and `ClearHistory()` to remove recorded events without removing rules. A successful rebuild clears active threshold states so the next accepted sample is evaluated against the new circuit.

### Creating the prompt snapshot

`CircuitContextBuilder` creates a defensive snapshot. Readings and events are copied, so advancing the simulation does not alter context that has already been submitted to an LLM.

```csharp
CircuitContextSnapshot context = new CircuitContextBuilder(simulation)
    .WithAnalyzer(analyzer)
    .IncludeReadings(true)
    .IncludeRecentEvents(16)
    .Build();

string promptContext = context.ToPromptText();
Debug.Log(promptContext);
```

Readings are included by default. Pass `false` to `IncludeReadings` for a topology-only prompt. `IncludeRecentEvents(0)` excludes event history. Without `WithAnalyzer`, the context still contains the netlist and optional readings.

`ToPromptText()` uses invariant culture and produces stable sections:

```text
CIRCUIT CONTEXT
CONVENTIONS ...

NETLIST
NODE N0 GROUND components=[C0,C1]
COMPONENT C1 name="Load" kind=Resistor positive=N1 negative=N0 resistance_ohms=1000

READINGS
C1 time_s=0.001 voltage_v=5 current_a=0.005 power_w=0.025

RECENT EVENTS oldest-to-newest
time_s=0.001 kind=ThresholdEntered component=C1 message="Load entered Power threshold ..."
```

Place this text in the user or tool context supplied to the LLM. Instruct the model to include the exact `C<n>` identifier whenever it mentions a component that the avatar should locate.

### Complete assistant/avatar example

The following example builds a circuit, records an overload-like threshold chosen by the application, creates prompt context, and resolves an ID returned by an assistant. The actual network request is intentionally left to the consuming application's LLM SDK.

```csharp
using CircuitSimulator.Unity;
using UnityEngine;

public sealed class CircuitAssistantExample : MonoBehaviour
{
    [SerializeField] private Transform assistantAvatar;

    private CircuitSimulation simulation;
    private CircuitAnalyzer analyzer;
    private CircuitContextSnapshot submittedContext;

    private void Start()
    {
        simulation = gameObject.AddComponent<CircuitSimulation>();
        simulation.AutomaticStepping = false;
        simulation.TimeStep = 1e-3;

        analyzer = gameObject.AddComponent<CircuitAnalyzer>();
        analyzer.SetSimulation(simulation);
        analyzer.HistoryCapacity = 64;

        VoltageSource source = simulation.AddVoltageSource("Supply", 5.0);
        CircuitSwitch powerSwitch = simulation.AddSwitch("Power switch", initiallyClosed: true);
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

        string llmContext = submittedContext.ToPromptText();
        Debug.Log(llmContext);

        // Send llmContext through the application's LLM integration.
        // Ask for a structured response containing a componentId such as "C2".
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

Keep the `CircuitContextSnapshot` that was actually submitted and resolve the response against that same snapshot. This avoids accidentally interpreting an old model response using IDs from a newer rebuilt circuit. Unity object references remain runtime mappings; if an object was destroyed after submission, treat the resolution as unavailable and build fresh context before asking again.

## Development

The standalone .NET Core project, offline DC solver, bounded transient solver, DocFX site, and xUnit test project have been removed. Verification is done through Unity EditMode and PlayMode tests for the package.
