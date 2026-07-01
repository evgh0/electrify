# Circuit Simulator

This repository contains a .NET 8 electrical circuit simulation core built around terminal-to-node topology compilation and Modified Nodal Analysis (MNA).

## Projects

```text
CircuitSimulator.sln
src/CircuitSimulator.Core
src/CircuitSimulator.Unity
tests/CircuitSimulator.Core.Tests
```

`CircuitSimulator.Core` contains the physical model, topology compiler, validation, MNA assembly, Math.NET-backed linear solving, linear and nonlinear DC operating points, bounded backward-Euler transient simulation, and ongoing fixed-step realtime sessions.

`CircuitSimulator.Unity` is a Git-installable Unity Package Manager package for Unity 6. It provides typed passive devices, sources, ideal switches, momentary buttons, terminal/wire authoring, automatic hierarchy registration, frame-driven realtime simulation, mapped readings, runtime creation/deletion helpers, inspectors, gizmos, tests, and an RC sample. Install `https://github.com/evgh0/electrify.git?path=/src/CircuitSimulator.Unity` through Unity Package Manager. The package contains .NET Standard 2.1 builds of Core and Math.NET, while Core remains independent of Unity.

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
- Shockley diode;
- LED modeled as a Shockley diode fitted to a nominal forward voltage/current point.
- controllable ideal switch.

Reactive/passive parameters must be finite and greater than zero. Source values must be finite. Independent voltage and current sources can use constant or sinusoidal time-domain waveforms while retaining a separate DC operating-point value.

## Breadboard Routing

`Breadboard` is a Core routing helper built on top of `CircuitBuilder`. It does not add a solver component or create empty-hole MNA variables; instead, it maps a 30-column solderless breadboard layout to ordinary ideal wires between terminals that you insert.

The standard layout matches the common center-gap breadboard:

- rows A-E in the same column are connected;
- rows F-J in the same column are connected;
- each red or blue power rail is continuous across columns 1-30;
- top and bottom rails are separate unless connected with a jumper.

```csharp
var builder = new CircuitBuilder();
var board = new Breadboard(builder);

var source = builder.AddVoltageSource("V1", 5.0);
var resistor = builder.AddResistor("R1", 1_000.0);

var redRail = BreadboardHole.PowerRail(BreadboardPowerRail.TopPositive, 1);
var blueRail = BreadboardHole.PowerRail(BreadboardPowerRail.TopNegative, 1);
var stripPositive = BreadboardHole.TerminalStrip(BreadboardRow.C, 10);
var stripNegative = BreadboardHole.TerminalStrip(BreadboardRow.H, 10);

board.ConnectNets(redRail, stripPositive);
board.ConnectNets(blueRail, stripNegative);

board.Insert(redRail, source.Positive);
board.Insert(blueRail, source.Negative);
board.Insert(stripPositive, resistor.Positive);
board.Insert(stripNegative, resistor.Negative);
board.MarkNetAsGround(blueRail);

var result = new DcOperatingPointSolver().Solve(builder.Build());
double resistorCurrent = result.GetComponentCurrent(resistor.ComponentId);
```

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
2. voltage-source branch currents ordered by component ID;
3. inductor branch currents ordered by component ID;
4. ideal-switch branch currents ordered by component ID.

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

For nonlinear circuits, `DcOperatingPointSolver` applies Newton-Raphson to affine diode and LED tangent stamps. Convergence uses absolute and relative correction limits, with bounded Shockley-device voltage steps and a differentiable high-voltage continuation of the exponential to avoid overflow.

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

Ideal switches remain part of the compiled MNA system in both states, so changing a switch does not recompile the circuit or reset simulation state:

```csharp
var contact = builder.AddSwitch("S1", initiallyClosed: false);
var session = new RealtimeSimulationSession(
    builder.Build(),
    new RealtimeSimulationOptions(1e-3));

session.SetSwitchState(contact.ComponentId, true); // Applied by the next step.
var sample = session.Advance();
```

A closed switch is an exact zero-volt constraint; an open switch enforces exactly zero branch current. Switch state is sampled once at the start of each step and may be changed safely while `RunAsync` is active. If opening an ideal contact creates a singular floating circuit, the step fails transactionally: time and capacitor/inductor history remain committed at the previous sample, and closing the contact permits a retry on the same session.

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
