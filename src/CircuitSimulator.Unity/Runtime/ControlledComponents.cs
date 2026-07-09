using CircuitSimulator.Core.Model;
using UnityEngine;

namespace CircuitSimulator.Unity
{
    /// <summary>Shared electrical behavior for controllable ideal-contact components.</summary>
    public abstract class ControlledSwitchComponent : TwoTerminalCircuitComponent
    {
        private bool hasValidatedState;
        private bool validatedState;

        /// <summary>Gets whether the ideal contact is electrically closed.</summary>
        public abstract bool IsElectricallyClosed { get; }

        internal override TwoTerminalComponentHandle AddTo(CircuitBuilder builder, string coreName)
        {
            return builder.AddSwitch(coreName, IsElectricallyClosed);
        }

        /// <summary>Applies a contact-state change without invalidating compiled topology.</summary>
        protected void NotifySwitchStateChanged()
        {
            validatedState = IsElectricallyClosed;
            hasValidatedState = true;
            if (Simulation == null)
            {
                return;
            }

            Simulation.SetSwitchState(this, IsElectricallyClosed);
        }

        /// <summary>Routes serialized contact changes live while retaining rebuilds for other Inspector edits.</summary>
        protected override void OnValidate()
        {
            var currentState = IsElectricallyClosed;
            if (hasValidatedState && currentState != validatedState)
            {
                NotifySwitchStateChanged();
                return;
            }

            validatedState = currentState;
            hasValidatedState = true;
            base.OnValidate();
        }
    }

    /// <summary>A latched ideal switch that can be opened, closed, or toggled at runtime.</summary>
    [DisallowMultipleComponent]
    public sealed class CircuitSwitch : ControlledSwitchComponent
    {
        [SerializeField]
        private bool isClosed;

        /// <summary>Gets or sets whether the ideal switch is closed.</summary>
        public bool IsClosed
        {
            get => isClosed;
            set => SetClosed(value);
        }

        /// <inheritdoc />
        public override bool IsElectricallyClosed => isClosed;

        /// <summary>Sets whether the ideal switch is closed.</summary>
        public void SetClosed(bool isClosed)
        {
            if (this.isClosed == isClosed)
            {
                return;
            }

            this.isClosed = isClosed;
            NotifySwitchStateChanged();
        }

        /// <summary>Closes the ideal contact.</summary>
        public void Close()
        {
            SetClosed(true);
        }

        /// <summary>Opens the ideal contact.</summary>
        public void Open()
        {
            SetClosed(false);
        }

        /// <summary>Toggles between open and closed.</summary>
        public void Toggle()
        {
            IsClosed = !IsClosed;
        }
    }

    /// <summary>A momentary ideal contact with normally-open or normally-closed behavior.</summary>
    [DisallowMultipleComponent]
    public sealed class CircuitButton : ControlledSwitchComponent
    {
        [SerializeField]
        private bool normallyClosed;

        private bool isPressed;

        /// <summary>Gets or sets whether the unpressed button is electrically closed.</summary>
        public bool NormallyClosed
        {
            get => normallyClosed;
            set => SetNormallyClosed(value);
        }

        /// <summary>Gets whether the button is currently pressed.</summary>
        public bool IsPressed => isPressed;

        /// <inheritdoc />
        public override bool IsElectricallyClosed => normallyClosed != isPressed;

        /// <summary>Sets whether the unpressed button is electrically closed.</summary>
        public void SetNormallyClosed(bool normallyClosed)
        {
            if (this.normallyClosed == normallyClosed)
            {
                return;
            }

            this.normallyClosed = normallyClosed;
            NotifySwitchStateChanged();
        }

        /// <summary>Presses the momentary button.</summary>
        public void Press()
        {
            SetPressed(true);
        }

        /// <summary>Releases the momentary button.</summary>
        public void Release()
        {
            SetPressed(false);
        }

        private void SetPressed(bool value)
        {
            if (isPressed == value)
            {
                return;
            }

            isPressed = value;
            NotifySwitchStateChanged();
        }
    }
}
