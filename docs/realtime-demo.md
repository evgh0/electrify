# Standalone realtime RC filter demo

`CircuitSimulator.RealtimeDemo` demonstrates <xref:CircuitSimulator.Core.Simulation.RealtimeSimulationSession>
without depending on the existing Editor projects.

Run it from the repository root:

```bash
dotnet run --project src/CircuitSimulator.RealtimeDemo/CircuitSimulator.RealtimeDemo.csproj
```

The predefined circuit is a 1 Hz, 5 V peak sinusoidal source driving two RC low-pass stages:

```text
V1 ── R1 ──┬── R2 ──┬──
            C1       C2
             │        │
            GND──────GND
```

`R1` and `R2` are 1 kΩ, `C1` is 100 µF, and `C2` is 220 µF. The simulation uses fixed-step backward Euler
with a 5 ms timestep and zero initial capacitor voltage.

The window is split vertically. The left side plots rolling voltage, positive-to-negative current, and
instantaneous power for the selected component. The right side contains the fixed circuit and component
selector. Selecting a component changes only the chart projection; the realtime session and reactive
history continue uninterrupted.

The Core stream is consumed on a background task. Its immutable samples enter a thread-safe circular
buffer that retains at most ten seconds. An Avalonia dispatcher timer publishes immutable chart snapshots
at approximately 30 frames per second, keeping rendering cadence independent from numerical integration.

The application intentionally provides no component editing, parameter editing, run controls, or
persistence. Closing the window cancels and awaits the realtime sample consumer.
