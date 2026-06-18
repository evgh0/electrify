# Circuit Simulator

This repository contains a .NET 8 electrical circuit simulation core built around terminal-to-node topology compilation and Modified Nodal Analysis (MNA).

## Projects

```text
CircuitSimulator.sln
src/CircuitSimulator.Core
src/CircuitSimulator.Console
tests/CircuitSimulator.Core.Tests
```

`CircuitSimulator.Core` contains the physical model, topology compiler, validation, MNA assembly, Math.NET-backed linear solving, and linear DC operating-point results. The console project is a demonstration only.

## Physical Model

Circuits are built with `CircuitBuilder`. Components own ordered terminals, ideal wires connect terminals, and one or more terminals can be marked as ground.

For all supported two-terminal components:

```text
terminal 0 = positive/reference
terminal 1 = negative
Vcomponent = Vpositive - Vnegative
```

Positive component current flows from terminal 0 to terminal 1.

Supported linear DC components:

- resistor, in ohms;
- independent current source, in amperes;
- independent voltage source, in volts.

Resistances must be finite and greater than zero. Source values must be finite.

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

The implemented production path is linear DC MNA for resistors, independent current sources, and independent voltage sources. Transient state, capacitors, inductors, Newton-Raphson, diodes, controlled sources, sparse assembly, AC analysis, and netlist parsing remain deferred roadmap work in `PLANS.md`.
