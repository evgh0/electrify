# Getting started

## Prerequisites

- .NET 8 SDK

Reference `CircuitSimulator.Core` from your application.

## Build and solve a voltage divider

All two-terminal components use terminal 0 as positive/reference and terminal 1 as negative. Positive component current flows from positive to negative.

```csharp
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;

var builder = new CircuitBuilder();
var source = builder.AddVoltageSource("V1", 10.0);
var r1 = builder.AddResistor("R1", 1_000.0);
var r2 = builder.AddResistor("R2", 2_000.0);

builder.Connect(source.Negative, r2.Negative);
builder.MarkAsGround(source.Negative);
builder.Connect(source.Positive, r1.Positive);
builder.Connect(r1.Negative, r2.Positive);

var result = new DcOperatingPointSolver().Solve(builder.Build());

double inputVoltage = result.GetTerminalVoltage(source.Positive); // 10 V
double outputVoltage = result.GetTerminalVoltage(r2.Positive);    // 20/3 V
double sourceCurrent = result.GetComponentCurrent(source.ComponentId);
```

<xref:CircuitSimulator.Core.Simulation.DcOperatingPointSolver.Solve(CircuitSimulator.Core.Model.Circuit)> compiles the physical circuit, assembles the MNA system, solves it, and returns an immutable <xref:CircuitSimulator.Core.Results.DcOperatingPointResult>.

## Ground and polarity

Every solvable circuit needs a ground marker. Connected ground markers compile into the same electrical node, which is always `NodeId(0)` and has no MNA voltage variable.

For every two-terminal component:

```text
Vcomponent = Vpositive - Vnegative
```

A voltage source branch current can be negative. This means that the physical current flows opposite the positive-to-negative reference direction.

## Drive an ongoing simulation from a variable frame rate

<xref:CircuitSimulator.Core.Simulation.RealtimeSimulationSession> keeps its fixed numerical timestep
independent of rendering cadence. A frame loop can accumulate elapsed wall time and advance zero, one,
or multiple steps per frame:

```csharp
var realtime = new RealtimeSimulationSession(
    builder.Build(),
    new RealtimeSimulationOptions(timeStep: 1e-4));

double accumulator = 0.0;

void RenderFrame(double elapsedSeconds)
{
    accumulator += elapsedSeconds;
    while (accumulator >= realtime.Options.TimeStep)
    {
        realtime.Advance();
        accumulator -= realtime.Options.TimeStep;
    }

    Render(realtime.LatestSample);
}
```

Alternatively, consume <xref:CircuitSimulator.Core.Simulation.RealtimeSimulationSession.RunAsync*>
away from the render callback. It uses monotonic absolute deadlines for best-effort 1x pacing and catches
up without skipping fixed integration steps. Rendering can read the latest immutable sample at any frame
rate, while a separate collector can retain samples needed for charts.
