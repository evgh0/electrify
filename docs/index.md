---
_layout: landing
---

# Circuit Simulator

Circuit Simulator is a .NET 8 library and Avalonia demonstration for compiling physical circuits into electrical nodes and solving linear/nonlinear DC and backward-Euler transient analyses with Modified Nodal Analysis (MNA).

## Current capabilities

- Immutable circuit definitions built with <xref:CircuitSimulator.Core.Model.CircuitBuilder>.
- Deterministic terminal-to-node compilation with ground assigned to `NodeId(0)`.
- Resistors, capacitors, inductors, diodes, and independent sources.
- Dense MNA assembly behind `IMnaSystemBuilder`.
- Math.NET-backed linear solving behind `ILinearSystemSolver`.
- Newton-Raphson diode analysis and transactional transient state.
- Ongoing fixed-step realtime sessions independent of rendering frame rate.
- Node-voltage and component-current queries from DC and transient results.
- Dark Avalonia transient lab with RC, RL, and rectifier examples.
- Standalone Core-only realtime RC filter visualization.

Start with the [getting-started guide](getting-started.md), open the [transient lab guide](editor.md), run the [standalone realtime demo](realtime-demo.md), or browse the [API reference](api/toc.yml).

> [!NOTE]
> Complex small-signal AC analysis and a general-purpose schematic editor remain deferred roadmap work.
