# Circuit Simulator

This repository contains a .NET 8 electrical circuit simulation core built around terminal-to-node topology compilation and Modified Nodal Analysis (MNA).

## Projects

```text
CircuitSimulator.sln
src/CircuitSimulator.Core
src/CircuitSimulator.Console
src/CircuitSimulator.Editor.Contracts
src/CircuitSimulator.Editor
src/CircuitSimulator.Editor.App
src/CircuitSimulator.RealtimeDemo
tests/CircuitSimulator.Core.Tests
tests/CircuitSimulator.Editor.Tests
tests/CircuitSimulator.RealtimeDemo.Tests
```

`CircuitSimulator.Core` contains the physical model, topology compiler, validation, MNA assembly, Math.NET-backed linear solving, linear and nonlinear DC operating points, bounded backward-Euler transient simulation, and ongoing fixed-step realtime sessions. The console project is a demonstration only.

The editor projects add an Avalonia transient showcase without introducing any Avalonia dependency into the simulation core. `Editor.Contracts` owns fixed schematic geometry, `Editor` owns demo definitions, orchestration, chart data, and custom rendering, and `Editor.App` is the desktop host.

## Avalonia transient lab

Run the Avalonia workbench:

```bash
dotnet run --project src/CircuitSimulator.Editor.App/CircuitSimulator.Editor.App.csproj
```

The dark VS Code/Obsidian-inspired workbench includes three deterministic examples:

- RC charging from a 5 V step;
- RL current response from a 5 V step;
- a nonlinear 50 Hz diode rectifier with smoothing capacitor and load.

Select a component from the list or circuit drawing to inspect synchronized voltage, current, and instantaneous-power traces. Hover the chart or use the arrow keys to move its shared sample cursor. The inspector accepts engineering values such as `4.7k`, `100u`, and `10m`; **Apply & Run** validates the fields, rebuilds the immutable circuit, and starts a cancellable background simulation.

The showcase uses fixed circuit topologies. General placement, wiring, undo/redo, persistence, and document tabs remain deferred full-editor work.

## Standalone realtime demo

Run the independent realtime RC filter demonstration:

```bash
dotnet run --project src/CircuitSimulator.RealtimeDemo/CircuitSimulator.RealtimeDemo.csproj
```

This project references `CircuitSimulator.Core` directly and does not depend on the Editor projects. It continuously simulates a predefined 1 Hz two-stage RC low-pass filter using a fixed 5 ms timestep. The left side shows a rolling ten-second voltage/current/power chart, while the right side shows the fixed schematic. Select a component from the dropdown or click its symbol; selection changes presentation only and does not restart the simulation.

The sample stream runs away from the Avalonia render callback. A bounded rolling buffer retains only the newest ten seconds, and the UI publishes immutable snapshots at approximately 30 frames per second.

## Physical Model

Circuits are built with `CircuitBuilder`. Components own ordered terminals, ideal wires connect terminals, and one or more terminals can be marked as ground.

For all supported two-terminal components:

```text
terminal 0 = positive/reference
terminal 1 = negative
Vcomponent = Vpositive - Vnegative
```

Positive component current flows from terminal 0 to terminal 1.

Supported components:

- resistor, in ohms;
- independent current source, in amperes;
- independent voltage source, in volts;
- capacitor, in farads;
- inductor, in henries;
- Shockley diode.

Reactive/passive parameters must be finite and greater than zero. Source values must be finite. Independent voltage and current sources can use constant or sinusoidal time-domain waveforms while retaining a separate DC operating-point value.

## Compilation

`CircuitCompiler` collapses ideal-wire-connected terminals into electrical nodes using union-find. Ground is an electrical node and is always assigned `NodeId(0)`. Ground has no MNA voltage variable.

Node numbering is deterministic:

1. ground first;
2. remaining node groups ordered by minimum terminal ID.

The compiled circuit includes:

- `Netlist` for terminal-to-node lookup;
- `NodeGraph` as a component/node multigraph;
- compiled component node references;
- structured validation warnings;
- deterministic `MnaVariableMap`.

Compilation validates missing ground, disconnected ground markers, invalid references, invalid terminal counts, floating subnetworks, and voltage-source self-loops.

## MNA Convention

All linear systems use:

```text
A * x = z
```

The unknown vector is ordered as:

```text
[non-ground node voltages, voltage-source branch currents]
```

Variable ordering is deterministic:

1. non-ground node voltages ordered by node ID;
2. voltage-source branch currents ordered by component ID.

Positive RHS current means current injected into a node. A current source from positive node `p` to negative node `n` stamps:

```text
z[p] += -I
z[n] += +I
```

A voltage source satisfying `Vp - Vn = Vs` stamps:

```text
A[p,k] += +1
A[n,k] += -1
A[k,p] += +1
A[k,n] += -1
z[k]   += Vs
```

The implementation uses one global matrix, equivalent to the standard MNA block form:

```text
[ G  B ] [ v ] = [ j ]
[ C  D ] [ i ]   [ e ]
```

Component stamps depend on `IMnaSystemBuilder`, not on Math.NET matrices.

## Solving

`DcOperatingPointSolver` assembles the compiled circuit through `MnaAssembler` and solves it with `ILinearSystemSolver`. `MathNetLinearSystemSolver` is the default adapter and keeps Math.NET types behind the numerical layer.

The solver never inverts a matrix. It detects singular or near-singular pivots, preserves input `MnaLinearSystem` snapshots, validates finite solution values, and handles zero-dimensional systems.

For nonlinear circuits, `DcOperatingPointSolver` applies Newton-Raphson to affine diode tangent stamps. Convergence uses absolute and relative correction limits, with bounded diode-voltage steps and a differentiable high-voltage continuation of the exponential to avoid overflow.

## Transient simulation

`TransientSimulationSolver` uses fixed-step backward Euler. Capacitors use an equivalent conductance and voltage-history current source; inductors use a branch-current equation. Each run owns independent history, and capacitor/inductor state is committed only after the complete time step has solved successfully.

```csharp
var builder = new CircuitBuilder();
var source = builder.AddSinusoidalVoltageSource("V1", 0, 1, 1_000);
var resistor = builder.AddResistor("R1", 1_000);
var capacitor = builder.AddCapacitor("C1", 1e-6);

builder.Connect(source.Negative, capacitor.Negative);
builder.MarkAsGround(source.Negative);
builder.Connect(source.Positive, resistor.Positive);
builder.Connect(resistor.Negative, capacitor.Positive);

var options = new TransientSimulationOptions(0, 5e-3, 10e-6);
var transient = new TransientSimulationSolver().Solve(builder.Build(), options);
double finalVoltage = transient.Samples[^1].GetComponentVoltage(capacitor.ComponentId);
```

Unspecified capacitor voltage and inductor current default explicitly to zero. The first returned sample is one time step after `StartTime`; the final step is shortened when necessary to land exactly on `StopTime`.

### Ongoing realtime simulation

`RealtimeSimulationSession` advances the same backward-Euler model indefinitely without a stop time or an unbounded result collection. It can stream at best-effort 1x wall-clock pace:

```csharp
var session = new RealtimeSimulationSession(
    builder.Build(),
    new RealtimeSimulationOptions(timeStep: 10e-6));

using var cancellation = new CancellationTokenSource();
await foreach (var sample in session.RunAsync(cancellation.Token))
{
    PublishLatestSample(sample); // Keep rendering on a separate UI/frame loop.
}
```

The integration step is independent of rendering frequency. A faster renderer can display `LatestSample` more than once, while a slower renderer can display only the newest sample; every numerical step is still solved and committed. Consumers that need complete waveform history should collect the streamed immutable samples in their own bounded storage.

A variable-frame-rate application can instead maintain a fixed-step accumulator and use immediate manual advancement:

```csharp
accumulator += frameElapsedSeconds;
while (accumulator >= session.Options.TimeStep)
{
    session.Advance();
    accumulator -= session.Options.TimeStep;
}

Render(session.LatestSample);
```

Ending or cancelling `RunAsync` pauses at the last committed sample. Starting it again on the same session resumes reactive history with a fresh wall-clock anchor. Realtime pacing is best-effort rather than a hard real-time scheduling guarantee.

## Example

```csharp
var builder = new CircuitBuilder();
var v1 = builder.AddVoltageSource("V1", 10.0);
var r1 = builder.AddResistor("R1", 1_000.0);
var r2 = builder.AddResistor("R2", 2_000.0);

builder.Connect(v1.Negative, r2.Negative);
builder.MarkAsGround(v1.Negative);
builder.Connect(v1.Positive, r1.Positive);
builder.Connect(r1.Negative, r2.Positive);

var result = new DcOperatingPointSolver().Solve(builder.Build());

double node1 = result.GetTerminalVoltage(v1.Positive);
double node2 = result.GetTerminalVoltage(r2.Positive);
double sourceCurrent = result.GetBranchCurrent(v1.ComponentId);
```

Expected values:

```text
V(node 1) = 10 V
V(node 2) = 6.6666666667 V
I(V1)     = -0.003333333333 A
I(R1)     =  0.003333333333 A
I(R2)     =  0.003333333333 A
```

Run the demonstration:

```bash
dotnet run --project src/CircuitSimulator.Console/CircuitSimulator.Console.csproj
```

## Documentation

The documentation site combines conceptual guides with API reference pages generated from the core library's XML documentation by DocFX.

Restore the repository-local tool and build the site:

```bash
dotnet tool restore
dotnet docfx docs/docfx.json --warningsAsErrors
```

The generated site is written to `docs/_site`. To preview it locally:

```bash
dotnet docfx docs/docfx.json --serve
```

## Limitations

The implemented production path supports linear/nonlinear DC, bounded fixed-step backward-Euler transient analysis, and ongoing fixed-step realtime sessions. Complex small-signal AC analysis, adaptive time stepping, controlled sources, sparse assembly, and netlist parsing remain deferred roadmap work in `PLANS.md`. A sinusoidal source is a time-domain waveform and is not a frequency-domain phasor source.
