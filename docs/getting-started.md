# Getting started

## Prerequisites

- .NET 8 SDK

Reference `CircuitSimulator.Core` from your application. Within this repository, the console demonstration already has the required project reference.

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

## Run the demonstration

From the repository root:

```bash
dotnet run --project src/CircuitSimulator.Console/CircuitSimulator.Console.csproj
```

The demonstration prints the compiled nodes, terminal mapping, graph edges, MNA variables, assembled system, voltages, and component currents.
