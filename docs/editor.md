# Avalonia transient lab

The Avalonia editor is split into projects that preserve the core dependency boundary:

```text
CircuitSimulator.Core
        ↑
CircuitSimulator.Editor.Contracts
        ↑
CircuitSimulator.Editor
        ↑
CircuitSimulator.Editor.App
```

Run it with:

```bash
dotnet run --project src/CircuitSimulator.Editor.App/CircuitSimulator.Editor.App.csproj
```

The focused workbench demonstrates the Core transient solver through three fixed examples: RC charging, RL response, and a nonlinear half-wave rectifier. Select an example from the left sidebar; the current simulation is cancelled and the new preset runs asynchronously.

Select a component from the accessible list or custom-drawn circuit. The waveform panel shows synchronized voltage, positive-to-negative current, and instantaneous power with independent ranges. Hover the chart or use the left and right arrow keys to move the shared cursor and inspect exact values.

The inspector accepts engineering values such as `4.7k`, `100u`, `10m`, and `1Meg`. Invalid input remains visible with inline validation and does not replace the last valid circuit. **Apply & Run** rebuilds the immutable preset, while **Cancel** preserves the last successful chart.

The application intentionally uses fixed topology and placement. General component placement, wiring, undo/redo, persistence, and multi-document editing remain deferred full-editor work.
