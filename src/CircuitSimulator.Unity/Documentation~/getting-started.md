# Getting started

## Scene authoring

Add one `CircuitSimulation` to a root GameObject. Add typed device behaviours below it and assign their positive and negative child `CircuitTerminal` objects. The component inspector can create missing terminal children. Add `CircuitWire` objects below the same manager and assign two terminal endpoints. Mark at least one connected terminal as ground.

Enabled descendants register automatically. References across two simulation-manager hierarchies, terminals owned by the wrong component, dangling wires, invalid parameters, floating subnetworks, and missing ground markers produce a fault with structured validation issues.

## Runtime setup

The manager factory methods create a device GameObject and both terminals. `Connect` creates an ideal wire. All electrical values use SI units.

```csharp
var source = simulation.AddSinusoidalVoltageSource("V1", 0, 5, 50);
var resistor = simulation.AddResistor("R1", 1000);
var capacitor = simulation.AddCapacitor("C1", 100e-6);

simulation.Connect(source.Negative, capacitor.Negative);
simulation.SetGround(source.Negative);
simulation.Connect(source.Positive, resistor.Positive);
simulation.Connect(resistor.Negative, capacitor.Positive);
```

`StartSimulation`, `PauseSimulation`, `ResumeSimulation`, and `RestartSimulation` control lifecycle. `Step` accepts exactly one fixed integration step. `Tick(seconds)` uses a fixed-step accumulator and returns the accepted-step count. When automatic stepping is enabled, Unity `Update` calls `Tick(Time.deltaTime)`.

The default timestep is 1 ms and the default frame limit is 32 numerical steps. Excess elapsed time remains in the accumulator; steps are not skipped or enlarged. Choose a timestep small enough for the fastest circuit dynamics.

## Readings

Every device exposes `LatestReading`, `HasReading`, `Voltage`, `Current`, `Power`, and `ReadingChanged`. Before the first accepted sample, convenience values are zero and `HasReading` is false. Power uses the passive sign convention, so positive power is absorbed and negative power is delivered.

## Editing and deletion

Changing device properties, terminals, wires, ground flags, hierarchy, or enabled state schedules a rebuild before the next step. A rebuild resets time, waveform phase, capacitor/inductor history, accumulated frame time, and readings.

`DeleteComponent` destroys the device GameObject and every `CircuitConnection` touching its terminals. `DeleteWire` removes one ideal wire. Editor commands use Unity Undo; runtime deletion uses `Object.Destroy`.

`CircuitConnection.IsConducting` is the extension point for future switch and button behaviours. A derived connection calls `NotifyConductivityChanged` whenever its conducting state changes.
