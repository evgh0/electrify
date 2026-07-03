# Changelog

## Unreleased

- Added readable ideal jumper components with zero-voltage MNA stamping and positive-to-negative branch-current readings.
- Added runtime jumper factories, jumper gizmos, documentation, and Unity test coverage.

## 1.2.0

- Merged the realtime simulation engine into the Unity package source.
- Removed the packaged Core DLL and standalone offline solver distribution.
- Kept Math.NET Numerics as the package's only managed plug-in.
- Added Unity coverage for realtime RL stepping, sinusoidal sources, transactional failed steps, and voltage-source self-loop diagnostics.

## 1.1.0

- Added stateful ideal switches and normally-open/normally-closed momentary buttons.
- Added live switch control that preserves realtime time, reactive history, accumulated frame time, and readings.
- Added switch/button inspector controls, contact gizmos, tests, and sample interactions.

## 1.0.0

- Added Unity 6 realtime circuit manager and typed two-terminal device behaviours.
- Added terminal/wire authoring, mapped readings, runtime factories, rebuild control, and cascading deletion.
- Added custom inspectors, scene gizmos, Unity Test Framework coverage, and the realtime RC sample.
