using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Results;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Core.Validation;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Compiles registered scene elements and owns one fixed-step realtime simulation.</summary>
    [DisallowMultipleComponent]
    public sealed class CircuitSimulation : MonoBehaviour
    {
        private const string MissingTerminalCode = "UNITY_MISSING_TERMINAL";
        private const string InvalidTerminalOwnerCode = "UNITY_INVALID_TERMINAL_OWNER";
        private const string InvalidConnectionCode = "UNITY_INVALID_CONNECTION";
        private const string InvalidParameterCode = "UNITY_INVALID_PARAMETER";

        [SerializeField]
        [Min(float.Epsilon)]
        private double timeStep = 1e-3;

        [SerializeField]
        [Min(1)]
        private int maximumStepsPerFrame = 32;

        [SerializeField]
        private bool automaticStepping = true;

        [SerializeField]
        private bool startOnEnable = true;

        private readonly HashSet<CircuitElement> elements = new HashSet<CircuitElement>();
        private readonly Dictionary<CircuitComponent, ComponentId> componentIds =
            new Dictionary<CircuitComponent, ComponentId>();
        private RealtimeSimulationSession session;
        private IReadOnlyList<CircuitValidationIssue> validationIssues = Array.Empty<CircuitValidationIssue>();
        private CircuitSimulationFailure lastFailure;
        private CircuitSimulationState state = CircuitSimulationState.Uninitialized;
        private bool isDirty = true;
        private bool wantsToRun;
        private double accumulator;

        /// <summary>Raised whenever the externally visible lifecycle state changes.</summary>
        public event Action<CircuitSimulationState> StateChanged;

        /// <summary>Raised when a rebuild or numerical step fails.</summary>
        public event Action<CircuitSimulationFailure> SimulationFailed;

        /// <summary>Gets or sets the fixed backward-Euler integration step in seconds.</summary>
        public double TimeStep
        {
            get => timeStep;
            set
            {
                ValidatePositiveFinite(value, nameof(value));
                if (timeStep.Equals(value))
                {
                    return;
                }

                timeStep = value;
                RequestRebuild();
            }
        }

        /// <summary>Gets or sets the maximum number of fixed steps processed by one call to <see cref="Tick"/>.</summary>
        public int MaximumStepsPerFrame
        {
            get => maximumStepsPerFrame;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum steps must be at least one.");
                }

                maximumStepsPerFrame = value;
            }
        }

        /// <summary>Gets or sets whether Unity Update supplies scaled frame time automatically.</summary>
        public bool AutomaticStepping
        {
            get => automaticStepping;
            set => automaticStepping = value;
        }

        /// <summary>Gets the current lifecycle state.</summary>
        public CircuitSimulationState State => state;

        /// <summary>Gets whether scene changes require a new immutable Core circuit.</summary>
        public bool IsDirty => isDirty;

        /// <summary>Gets the latest caught failure, if any.</summary>
        public CircuitSimulationFailure LastFailure => lastFailure;

        /// <summary>Gets validation warnings or errors produced by the latest rebuild.</summary>
        public IReadOnlyList<CircuitValidationIssue> ValidationIssues => validationIssues;

        /// <summary>Gets the latest committed simulation time, or zero without a session.</summary>
        public double CurrentTime => session?.CurrentTime ?? 0.0;

        /// <summary>Registers an enabled descendant. Normal Unity lifecycle registration is automatic.</summary>
        public void Register(CircuitElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (!element.transform.IsChildOf(transform))
            {
                throw new ArgumentException("Circuit elements must be descendants of their simulation manager.", nameof(element));
            }

            if (elements.Add(element))
            {
                RequestRebuild();
            }
        }

        /// <summary>Unregisters an element and invalidates the compiled circuit.</summary>
        public void Unregister(CircuitElement element)
        {
            if (element != null && elements.Remove(element))
            {
                RequestRebuild();
            }
        }

        /// <summary>Marks topology or parameters dirty; rebuilding is deferred until the next step.</summary>
        public void RequestRebuild()
        {
            isDirty = true;
        }

        /// <summary>Starts automatic or explicit elapsed-time advancement.</summary>
        public bool StartSimulation()
        {
            wantsToRun = true;
            if (!EnsureSession())
            {
                return false;
            }

            SetState(CircuitSimulationState.Running);
            return true;
        }

        /// <summary>Pauses automatic and elapsed-time advancement while retaining the current session.</summary>
        public void PauseSimulation()
        {
            wantsToRun = false;
            if (state != CircuitSimulationState.Faulted)
            {
                SetState(CircuitSimulationState.Paused);
            }
        }

        /// <summary>Resumes a paused simulation.</summary>
        public bool ResumeSimulation()
        {
            return StartSimulation();
        }

        /// <summary>Rebuilds immediately and restarts from time zero.</summary>
        public bool RestartSimulation()
        {
            wantsToRun = true;
            RequestRebuild();
            return Rebuild();
        }

        /// <summary>Advances exactly one fixed step, including while paused.</summary>
        public bool Step()
        {
            if (!EnsureSession())
            {
                return false;
            }

            return AdvanceOneStep();
        }

        /// <summary>Adds elapsed seconds to the fixed-step accumulator and returns the number of accepted steps.</summary>
        public int Tick(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds, "Elapsed time must be finite and non-negative.");
            }

            if (!wantsToRun || (state == CircuitSimulationState.Faulted && !isDirty))
            {
                return 0;
            }

            if (!EnsureSession())
            {
                return 0;
            }

            accumulator += elapsedSeconds;
            var steps = 0;
            while (accumulator >= timeStep && steps < maximumStepsPerFrame)
            {
                if (!AdvanceOneStep())
                {
                    break;
                }

                accumulator -= timeStep;
                steps++;
            }

            return steps;
        }

        /// <summary>Compiles enabled descendants and creates a fresh realtime session.</summary>
        public bool Rebuild()
        {
            ClearReadings();
            accumulator = 0.0;
            session = null;
            componentIds.Clear();
            validationIssues = Array.Empty<CircuitValidationIssue>();
            lastFailure = null;

            try
            {
                CollectEnabledElements();
                var build = BuildCircuit();
                session = new RealtimeSimulationSession(
                    build.CompiledCircuit,
                    new RealtimeSimulationOptions(timeStep));
                foreach (var pair in build.ComponentIds)
                {
                    componentIds.Add(pair.Key, pair.Value);
                }

                validationIssues = build.CompiledCircuit.ValidationReport.Issues;
                isDirty = false;
                SetState(wantsToRun ? CircuitSimulationState.Running : CircuitSimulationState.Paused);
                return true;
            }
            catch (Exception exception)
            {
                var issues = exception is CircuitCompilationException compilationException
                    ? compilationException.ValidationReport.Issues
                    : Array.Empty<CircuitValidationIssue>();
                validationIssues = issues;
                lastFailure = new CircuitSimulationFailure(exception, issues);
                isDirty = false;
                SetState(CircuitSimulationState.Faulted);
                SimulationFailed?.Invoke(lastFailure);
                Debug.LogError("Circuit simulation rebuild failed: " + exception.Message, this);
                return false;
            }
        }

        /// <summary>Creates and registers a resistor GameObject with two child terminals.</summary>
        public Resistor AddResistor(string name, double resistanceOhms)
        {
            var component = CreateComponent<Resistor>(name);
            component.ResistanceOhms = resistanceOhms;
            return component;
        }

        /// <summary>Creates and registers a capacitor GameObject with two child terminals.</summary>
        public Capacitor AddCapacitor(string name, double capacitanceFarads)
        {
            var component = CreateComponent<Capacitor>(name);
            component.CapacitanceFarads = capacitanceFarads;
            return component;
        }

        /// <summary>Creates and registers an inductor GameObject with two child terminals.</summary>
        public Inductor AddInductor(string name, double inductanceHenries)
        {
            var component = CreateComponent<Inductor>(name);
            component.InductanceHenries = inductanceHenries;
            return component;
        }

        /// <summary>Creates and registers a default Shockley diode.</summary>
        public Diode AddDiode(string name)
        {
            return CreateComponent<Diode>(name);
        }

        /// <summary>Creates and registers a constant voltage source.</summary>
        public VoltageSource AddVoltageSource(string name, double voltage)
        {
            var source = CreateComponent<VoltageSource>(name);
            source.ConfigureConstant(voltage);
            return source;
        }

        /// <summary>Creates and registers a sinusoidal voltage source.</summary>
        public VoltageSource AddSinusoidalVoltageSource(
            string name,
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            var source = CreateComponent<VoltageSource>(name);
            source.ConfigureSinusoidal(offset, amplitude, frequencyHz, phaseRadians);
            return source;
        }

        /// <summary>Creates and registers a constant current source.</summary>
        public CurrentSource AddCurrentSource(string name, double current)
        {
            var source = CreateComponent<CurrentSource>(name);
            source.ConfigureConstant(current);
            return source;
        }

        /// <summary>Creates and registers a sinusoidal current source.</summary>
        public CurrentSource AddSinusoidalCurrentSource(
            string name,
            double offset,
            double amplitude,
            double frequencyHz,
            double phaseRadians = 0.0)
        {
            var source = CreateComponent<CurrentSource>(name);
            source.ConfigureSinusoidal(offset, amplitude, frequencyHz, phaseRadians);
            return source;
        }

        /// <summary>Creates an ideal wire between two terminals.</summary>
        public CircuitWire Connect(CircuitTerminal first, CircuitTerminal second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            var wireObject = new GameObject("Wire");
            wireObject.transform.SetParent(transform, false);
            var wire = wireObject.AddComponent<CircuitWire>();
            wire.Connect(first, second);
            return wire;
        }

        /// <summary>Sets or clears a ground marker on a terminal.</summary>
        public void SetGround(CircuitTerminal terminal, bool isGround = true)
        {
            if (terminal == null)
            {
                throw new ArgumentNullException(nameof(terminal));
            }

            terminal.IsGround = isGround;
        }

        /// <summary>Deletes an ideal wire and schedules a rebuild.</summary>
        public void DeleteWire(CircuitWire wire)
        {
            if (wire == null)
            {
                return;
            }

            UnregisterHierarchy(wire.gameObject);
            DestroyGameObject(wire.gameObject);
            RequestRebuild();
        }

        /// <summary>Deletes a device and all connections touching either of its terminals.</summary>
        public void DeleteComponent(TwoTerminalCircuitComponent component)
        {
            if (component == null)
            {
                return;
            }

            var connections = GetComponentsInChildren<CircuitConnection>(true);
            for (var index = connections.Length - 1; index >= 0; index--)
            {
                var connection = connections[index];
                if (connection == null)
                {
                    continue;
                }

                if (ReferenceEquals(connection.First, component.Positive) ||
                    ReferenceEquals(connection.First, component.Negative) ||
                    ReferenceEquals(connection.Second, component.Positive) ||
                    ReferenceEquals(connection.Second, component.Negative))
                {
                    UnregisterHierarchy(connection.gameObject);
                    DestroyGameObject(connection.gameObject);
                }
            }

            UnregisterHierarchy(component.gameObject);
            DestroyGameObject(component.gameObject);
            RequestRebuild();
        }

        private void OnEnable()
        {
            wantsToRun = startOnEnable;
            CollectEnabledElements();
            RequestRebuild();
            SetState(wantsToRun ? CircuitSimulationState.Running : CircuitSimulationState.Paused);
        }

        private void OnDisable()
        {
            wantsToRun = false;
            if (state != CircuitSimulationState.Faulted)
            {
                SetState(CircuitSimulationState.Paused);
            }
        }

        private void Update()
        {
            if (automaticStepping && wantsToRun)
            {
                Tick(Time.deltaTime);
            }
        }

        private bool EnsureSession()
        {
            return !isDirty && session != null ? true : Rebuild();
        }

        private bool AdvanceOneStep()
        {
            try
            {
                var sample = session.Advance();
                foreach (var pair in componentIds)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.Publish(sample, pair.Value);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                lastFailure = new CircuitSimulationFailure(exception, validationIssues);
                SetState(CircuitSimulationState.Faulted);
                SimulationFailed?.Invoke(lastFailure);
                Debug.LogError("Circuit simulation step failed: " + exception.Message, this);
                return false;
            }
        }

        private CircuitBuild BuildCircuit()
        {
            ValidatePositiveFinite(timeStep, nameof(TimeStep));
            if (maximumStepsPerFrame < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(MaximumStepsPerFrame), maximumStepsPerFrame, "Maximum steps must be at least one.");
            }

            var builder = new CircuitBuilder();
            var components = GetActiveElements<CircuitComponent>();
            var connections = GetActiveElements<CircuitConnection>();
            var terminals = new Dictionary<CircuitTerminal, TerminalId>();
            var ids = new Dictionary<CircuitComponent, ComponentId>();
            var authoringIssues = new List<CircuitValidationIssue>();

            for (var index = 0; index < components.Count; index++)
            {
                var component = components[index];
                var twoTerminal = component as TwoTerminalCircuitComponent;
                if (twoTerminal == null || twoTerminal.Positive == null || twoTerminal.Negative == null)
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        MissingTerminalCode,
                        component.DisplayName + " must reference positive and negative terminals."));
                    continue;
                }

                if (ReferenceEquals(twoTerminal.Positive, twoTerminal.Negative))
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        MissingTerminalCode,
                        component.DisplayName + " uses the same object for both terminals."));
                    continue;
                }

                if (!twoTerminal.Positive.isActiveAndEnabled ||
                    !twoTerminal.Negative.isActiveAndEnabled ||
                    !ReferenceEquals(twoTerminal.Positive.Simulation, this) ||
                    !ReferenceEquals(twoTerminal.Negative.Simulation, this) ||
                    !ReferenceEquals(twoTerminal.Positive.Owner, twoTerminal) ||
                    !ReferenceEquals(twoTerminal.Negative.Owner, twoTerminal))
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        InvalidTerminalOwnerCode,
                        component.DisplayName + " terminals must be children owned by that component."));
                    continue;
                }

                if (terminals.ContainsKey(twoTerminal.Positive) || terminals.ContainsKey(twoTerminal.Negative))
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        InvalidTerminalOwnerCode,
                        component.DisplayName + " reuses a terminal already owned by another component."));
                    continue;
                }

                TwoTerminalComponentHandle handle;
                try
                {
                    handle = component.AddTo(builder, component.GetType().Name + "_" + index);
                }
                catch (ArgumentException exception)
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        InvalidParameterCode,
                        component.DisplayName + ": " + exception.Message));
                    continue;
                }

                ids.Add(component, handle.ComponentId);
                terminals.Add(twoTerminal.Positive, handle.Positive);
                terminals.Add(twoTerminal.Negative, handle.Negative);
            }

            foreach (var pair in terminals)
            {
                if (pair.Key.IsGround)
                {
                    builder.MarkAsGround(pair.Value);
                }
            }

            for (var index = 0; index < connections.Count; index++)
            {
                var connection = connections[index];
                if (!connection.IsConducting)
                {
                    continue;
                }

                TerminalId firstId;
                TerminalId secondId;
                if (connection.First == null || connection.Second == null ||
                    !terminals.TryGetValue(connection.First, out firstId) ||
                    !terminals.TryGetValue(connection.Second, out secondId) ||
                    !ReferenceEquals(connection.First.Simulation, this) ||
                    !ReferenceEquals(connection.Second.Simulation, this))
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        InvalidConnectionCode,
                        connection.name + " has a missing, inactive, or cross-simulation terminal."));
                    continue;
                }

                if (firstId == secondId)
                {
                    authoringIssues.Add(CreateAuthoringIssue(
                        InvalidConnectionCode,
                        connection.name + " cannot connect a terminal to itself."));
                    continue;
                }

                builder.Connect(firstId, secondId);
            }

            if (authoringIssues.Count > 0)
            {
                var report = new CircuitValidationReport(authoringIssues);
                throw new CircuitCompilationException("Unity circuit authoring validation failed.", report);
            }

            var compiled = new CircuitCompiler().Compile(builder.Build());
            return new CircuitBuild(compiled, ids);
        }

        private List<T> GetActiveElements<T>() where T : CircuitElement
        {
            var result = new List<T>();
            foreach (var element in elements)
            {
                var typed = element as T;
                if (typed != null && typed.isActiveAndEnabled && typed.transform.IsChildOf(transform))
                {
                    result.Add(typed);
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(GetHierarchyKey(left), GetHierarchyKey(right)));
            return result;
        }

        private void CollectEnabledElements()
        {
            elements.RemoveWhere(element => element == null || !element.transform.IsChildOf(transform));
            var descendants = GetComponentsInChildren<CircuitElement>(true);
            for (var index = 0; index < descendants.Length; index++)
            {
                var element = descendants[index];
                if (element != null && element.isActiveAndEnabled)
                {
                    elements.Add(element);
                    element.AttachToNearestSimulation();
                }
            }
        }

        private T CreateComponent<T>(string componentName) where T : TwoTerminalCircuitComponent
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentException("Component name cannot be null or whitespace.", nameof(componentName));
            }

            var componentObject = new GameObject(componentName);
            componentObject.transform.SetParent(transform, false);
            var component = componentObject.AddComponent<T>();
            component.DisplayName = componentName;
            component.EnsureTerminals();
            return component;
        }

        private void ClearReadings()
        {
            var components = GetComponentsInChildren<CircuitComponent>(true);
            for (var index = 0; index < components.Length; index++)
            {
                components[index].ClearReading();
            }
        }

        private void UnregisterHierarchy(GameObject root)
        {
            var ownedElements = root.GetComponentsInChildren<CircuitElement>(true);
            for (var index = 0; index < ownedElements.Length; index++)
            {
                ownedElements[index].DetachFromSimulation();
            }
        }

        private static void DestroyGameObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static CircuitValidationIssue CreateAuthoringIssue(string code, string message)
        {
            return new CircuitValidationIssue(code, ValidationSeverity.Error, message);
        }

        private static string GetHierarchyKey(CircuitElement element)
        {
            var indexes = new Stack<int>();
            var current = element.transform;
            while (current != null)
            {
                indexes.Push(current.GetSiblingIndex());
                current = current.parent;
            }

            var key = new StringBuilder();
            while (indexes.Count > 0)
            {
                key.Append(indexes.Pop().ToString("D8", System.Globalization.CultureInfo.InvariantCulture));
                key.Append('/');
            }

            var siblings = element.GetComponents<CircuitElement>();
            for (var index = 0; index < siblings.Length; index++)
            {
                if (ReferenceEquals(siblings[index], element))
                {
                    key.Append(index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                }
            }

            key.Append('/');
            key.Append(element.GetType().FullName);
            return key.ToString();
        }

        private void SetState(CircuitSimulationState newState)
        {
            if (state == newState)
            {
                return;
            }

            state = newState;
            StateChanged?.Invoke(newState);
        }

        private static void ValidatePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
            }
        }

        private sealed class CircuitBuild
        {
            public CircuitBuild(CompiledCircuit compiledCircuit, Dictionary<CircuitComponent, ComponentId> componentIds)
            {
                CompiledCircuit = compiledCircuit;
                ComponentIds = componentIds;
            }

            public CompiledCircuit CompiledCircuit { get; }

            public Dictionary<CircuitComponent, ComponentId> ComponentIds { get; }
        }
    }
}
