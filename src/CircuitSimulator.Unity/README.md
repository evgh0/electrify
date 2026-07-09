# Circuit Simulator Unity Helper

`com.evgh.circuit-simulator` is a Unity 6 package that maps scene components to an embedded realtime circuit model and exposes voltage, current, and power readings without exposing MNA data. Use topology-only `CircuitWire` objects for ordinary nets and readable `Jumper` components for ideal shorts whose branch current should be observed.

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

resistor.ReadingChanged += reading => Debug.Log(reading.Power);
simulation.StartSimulation();
```

Inspector-authored devices register with the nearest enabled `CircuitSimulation` in their parent hierarchy. Every two-terminal device uses terminal 0 as positive/reference and terminal 1 as negative. Positive current flows from positive to negative.

Runtime parameter, wiring, enable-state, hierarchy, and deletion changes mark the circuit dirty. The next numerical step rebuilds the embedded circuit model, resets simulation time and reactive history, and clears existing readings.

Electrical values can be changed at runtime with explicit setters such as `resistor.SetResistance(2000.0)`, `source.SetVoltage(5.0)`, and `circuitSwitch.SetClosed(true)`. Switch and button contact changes apply live on the next numerical step without resetting the current session.

`CircuitSimulation.AddJumper(name, first, second)` creates a readable ideal jumper and connects its positive endpoint to `first` and negative endpoint to `second`; positive current is reported in that direction. `CircuitSwitch.Open`, `Close`, and `Toggle`, plus `CircuitButton.Press` and `Release`, are live control operations. They apply on the next numerical step without rebuilding or clearing state. Buttons are normally open by default and can be configured as normally closed.

See `Documentation~/getting-started.md` and the Realtime RC sample for the full lifecycle and failure behavior.
