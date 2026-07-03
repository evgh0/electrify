# Circuit Simulator Unity Package

This repository now ships a Unity-only realtime electrical circuit simulator package.

Install it through Unity Package Manager with the package path:

```text
https://github.com/evgh0/electrify.git?path=/src/CircuitSimulator.Unity
```

The package targets Unity 6000.0 or later with the .NET Standard 2.1 API compatibility level. The numerical engine is compiled as package source under `src/CircuitSimulator.Unity/Runtime/Core`, including an in-package dense linear solver.

## Supported Runtime Features

- typed two-terminal resistors, capacitors, inductors, voltage sources, current sources, Shockley diodes, LEDs, ideal switches, momentary buttons, and readable ideal jumpers;
- constant and sinusoidal independent-source waveforms;
- terminal/wire authoring with one or more ground markers;
- frame-driven fixed-step backward-Euler realtime simulation;
- live switch and button state changes without recompiling or resetting reactive history;
- mapped voltage, current, and power readings on Unity components;
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

capacitor.ReadingChanged += reading => Debug.Log(reading.Voltage);
simulation.StartSimulation();
```

`CircuitSimulation.Tick(seconds)` advances a fixed-step accumulator and is called automatically from `Update` when automatic stepping is enabled. `Step()` accepts exactly one numerical step, including while paused. Use `CircuitWire` for topology-only ideal connections and `Jumper` when the ideal short itself needs voltage, current, power, or `ReadingChanged` results.

## Development

The standalone .NET Core project, offline DC solver, bounded transient solver, DocFX site, and xUnit test project have been removed. Verification is done through Unity EditMode and PlayMode tests for the package.
