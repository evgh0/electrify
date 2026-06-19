using System;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Base class for scene objects owned by a <see cref="CircuitSimulation"/>.</summary>
    public abstract class CircuitElement : MonoBehaviour
    {
        private CircuitSimulation simulation;

        /// <summary>Gets the nearest registered simulation manager.</summary>
        public CircuitSimulation Simulation => simulation;

        /// <summary>Requests an immutable circuit rebuild after a runtime property change.</summary>
        protected void NotifyCircuitChanged()
        {
            simulation?.RequestRebuild();
        }

        /// <summary>Registers this element when it becomes active.</summary>
        protected virtual void OnEnable()
        {
            AttachToNearestSimulation();
        }

        /// <summary>Unregisters this element when it becomes inactive.</summary>
        protected virtual void OnDisable()
        {
            DetachFromSimulation();
        }

        /// <summary>Moves registration when an element is reparented.</summary>
        protected virtual void OnTransformParentChanged()
        {
            AttachToNearestSimulation();
        }

        /// <summary>Invalidates the current snapshot after Inspector edits.</summary>
        protected virtual void OnValidate()
        {
            NotifyCircuitChanged();
        }

        internal void AttachToNearestSimulation()
        {
            var nearest = GetComponentInParent<CircuitSimulation>();
            if (ReferenceEquals(nearest, simulation))
            {
                return;
            }

            DetachFromSimulation();
            simulation = nearest;
            simulation?.Register(this);
        }

        internal void DetachFromSimulation()
        {
            if (simulation == null)
            {
                return;
            }

            var previous = simulation;
            simulation = null;
            previous.Unregister(this);
        }
    }

    /// <summary>A physical endpoint belonging to one two-terminal component.</summary>
    [DisallowMultipleComponent]
    public sealed class CircuitTerminal : CircuitElement
    {
        [SerializeField]
        private bool isGround;

        /// <summary>Gets or sets whether this terminal is a ground marker.</summary>
        public bool IsGround
        {
            get => isGround;
            set
            {
                if (isGround == value)
                {
                    return;
                }

                isGround = value;
                NotifyCircuitChanged();
            }
        }

        /// <summary>Gets the nearest owning two-terminal component.</summary>
        public TwoTerminalCircuitComponent Owner => GetComponentInParent<TwoTerminalCircuitComponent>();
    }

    /// <summary>Base class for a topology connection, including future switches and buttons.</summary>
    public abstract class CircuitConnection : CircuitElement
    {
        [SerializeField]
        private CircuitTerminal first;

        [SerializeField]
        private CircuitTerminal second;

        /// <summary>Gets the first connected terminal.</summary>
        public CircuitTerminal First => first;

        /// <summary>Gets the second connected terminal.</summary>
        public CircuitTerminal Second => second;

        /// <summary>Gets whether this connection currently joins its terminals electrically.</summary>
        public abstract bool IsConducting { get; }

        /// <summary>Assigns the connected terminal pair and requests a rebuild.</summary>
        public void Connect(CircuitTerminal firstTerminal, CircuitTerminal secondTerminal)
        {
            first = firstTerminal ?? throw new ArgumentNullException(nameof(firstTerminal));
            second = secondTerminal ?? throw new ArgumentNullException(nameof(secondTerminal));
            if (ReferenceEquals(first, second))
            {
                throw new ArgumentException("A circuit connection requires two different terminals.");
            }

            NotifyCircuitChanged();
        }

        /// <summary>Allows a derived controllable connection to invalidate topology after changing state.</summary>
        protected void NotifyConductivityChanged()
        {
            NotifyCircuitChanged();
        }
    }

    /// <summary>An always-conducting ideal wire between two terminals.</summary>
    [DisallowMultipleComponent]
    public sealed class CircuitWire : CircuitConnection
    {
        /// <inheritdoc />
        public override bool IsConducting => true;
    }
}
