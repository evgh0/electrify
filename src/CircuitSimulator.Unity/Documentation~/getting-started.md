# Getting started

## Scene authoring

Add one `CircuitSimulation` to a root GameObject. Add typed device behaviours below it and assign their positive and negative child `CircuitTerminal` objects. The component inspector can create missing terminal children. Add `CircuitWire` objects below the same manager and assign two terminal endpoints. Use `Jumper` components instead when an ideal short also needs voltage, current, power, or `ReadingChanged` results. Mark at least one connected terminal as ground.

Enabled descendants register automatically. References across two simulation-manager hierarchies, terminals owned by the wrong component, dangling wires, invalid parameters, floating subnetworks, and missing ground markers produce a fault with structured validation issues.

## Runtime setup

The manager factory methods create a device GameObject and both terminals. `Connect` creates a topology-only ideal wire. `AddJumper` creates a readable ideal short; `AddJumper(name, first, second)` also wires the new jumper's positive terminal to `first` and negative terminal to `second`. All electrical values use SI units.

```csharp
var source = simulation.AddSinusoidalVoltageSource("V1", 0, 5, 50);
var jumper = simulation.AddJumper("J1");
var resistor = simulation.AddResistor("R1", 1000);
var capacitor = simulation.AddCapacitor("C1", 100e-6);
var circuitSwitch = simulation.AddSwitch("S1", initiallyClosed: true);

simulation.Connect(source.Negative, capacitor.Negative);
simulation.SetGround(source.Negative);
simulation.Connect(source.Positive, jumper.Positive);
simulation.Connect(jumper.Negative, circuitSwitch.Positive);
simulation.Connect(circuitSwitch.Negative, resistor.Positive);
simulation.Connect(resistor.Negative, capacitor.Positive);
```

`StartSimulation`, `PauseSimulation`, `ResumeSimulation`, and `RestartSimulation` control lifecycle. `Step` accepts exactly one fixed integration step. `Tick(seconds)` uses a fixed-step accumulator and returns the accepted-step count. When automatic stepping is enabled, Unity `Update` calls `Tick(Time.deltaTime)`.

The default timestep is 1 ms and the default frame limit is 32 numerical steps. Excess elapsed time remains in the accumulator; steps are not skipped or enlarged. Choose a timestep small enough for the fastest circuit dynamics.

## Readings

Every device, including `Jumper`, exposes `LatestReading`, `HasReading`, `Voltage`, `Current`, `Power`, and `ReadingChanged`. Before the first accepted sample, convenience values are zero and `HasReading` is false. Power uses the passive sign convention, so positive power is absorbed and negative power is delivered. A jumper is an ideal zero-volt constraint, and its current is positive from its `Positive` terminal to its `Negative` terminal.

## Switches and buttons

`CircuitSwitch` is a latched ideal contact with `Open`, `Close`, and `Toggle`. `CircuitButton` is momentary and exposes `Press` and `Release`; its unpressed state is normally open unless `NormallyClosed` is enabled.

Contact changes are sampled once at the start of the next numerical step. They do not rebuild the circuit, reset time or source phase, discard capacitor/inductor history, clear readings, or remove accumulated frame time. A closed contact has exactly zero voltage and an open contact has exactly zero current.

Opening an ideal contact can leave a floating circuit and produce a singular solve. The failed step is transactional: the latest reading, time, and reactive history remain unchanged. Correcting the contact state permits the same session to continue.

## Editing and deletion

Changing device properties, terminals, wires, ground flags, hierarchy, or enabled state schedules a rebuild before the next step. A rebuild resets time, waveform phase, capacitor/inductor history, accumulated frame time, and readings.

`DeleteComponent` destroys the device GameObject, including jumpers, and every `CircuitConnection` touching its terminals. `DeleteWire` removes one topology-only ideal wire. Editor commands use Unity Undo; runtime deletion uses `Object.Destroy`.

`CircuitConnection` represents static topology such as ideal wires. `Jumper`, controllable switches, and buttons are simulated components so they can expose readings or stateful behavior.
