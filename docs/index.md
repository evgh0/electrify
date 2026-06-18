---
_layout: landing
---

# Circuit Simulator

Circuit Simulator is a .NET 8 library for building electrical circuits, compiling physical terminals and ideal wires into electrical nodes, and solving linear DC operating points with Modified Nodal Analysis (MNA).

## Current capabilities

- Immutable circuit definitions built with <xref:CircuitSimulator.Core.Model.CircuitBuilder>.
- Deterministic terminal-to-node compilation with ground assigned to `NodeId(0)`.
- Resistors and independent current and voltage sources.
- Dense MNA assembly behind `IMnaSystemBuilder`.
- Math.NET-backed linear solving behind `ILinearSystemSolver`.
- Node-voltage and component-current queries from DC operating-point results.

Start with the [getting-started guide](getting-started.md), or browse the [API reference](api/toc.yml).

> [!NOTE]
> Transient analysis, reactive components, nonlinear analysis, and diodes are roadmap work and are not part of the current production path.
