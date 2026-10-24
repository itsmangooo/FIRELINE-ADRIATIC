using System;
using Fireline.Shared;
using UnityEngine.InputSystem;

namespace Fireline.Client
{
    /// <summary>Engineering keyboard/gamepad mapping over installed Input System 1.20; no local simulation or production M05A input contexts.</summary>
    public sealed class FlightTestControls
    {
        private readonly Keyboard keyboard;
        private readonly Gamepad gamepad;
        private float throttle, flaps, gear = 1;
        /// <summary>One-frame request to claim a seat, read even while observing.</summary>
        public bool Claim { get; private set; }
        /// <summary>One-frame seat-release request; server remains responsible for authority.</summary>
        public bool Release { get; private set; }
        /// <summary>One-frame external-camera toggle, independent of pilot permission.</summary>
        public bool Camera { get; private set; }

        /// <summary>Use current physical devices normally; explicit synthetic devices make repeatable device-level proof possible.</summary>
        public FlightTestControls(Keyboard keyboard = null, Gamepad gamepad = null) { this.keyboard = keyboard; this.gamepad = gamepad; }

        /// <summary>Read normalized controls, cap throttle slew after stalls, and clear held commands on focus/seat loss.</summary>
        public FlightRequestRpc Sample(double seconds, bool focused, bool ownsSeat)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            var k = keyboard ?? Keyboard.current; var g = gamepad ?? Gamepad.current;
            Claim = focused && ((k != null && k.enterKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame));
            Release = focused && ((k != null && k.backspaceKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame));
            Camera = focused && ((k != null && k.cKey.wasPressedThisFrame) || (g != null && g.rightShoulder.wasPressedThisFrame));
            if (!focused || !ownsSeat) { throttle = flaps = 0; gear = 1; return new FlightRequestRpc { Gear = 1, Brake = 1 }; }
            float up = (k != null && k.leftShiftKey.isPressed ? 1 : 0) + (g?.rightTrigger.ReadValue() ?? 0);
            float down = (k != null && k.leftCtrlKey.isPressed ? 1 : 0) + (g?.leftTrigger.ReadValue() ?? 0);
            throttle = Clamp(throttle + (up - down) * (float)Math.Min(.1, seconds) * .3f, 0, 1);
            if ((k != null && k.fKey.wasPressedThisFrame) || (g != null && g.dpad.up.wasPressedThisFrame)) flaps = flaps >= 1 ? 0 : flaps + .5f;
            if ((k != null && k.gKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame)) gear = 1 - gear;
            return new FlightRequestRpc { Throttle = throttle, Flaps = flaps, Gear = gear,
                Pitch = Clamp((k != null && k.sKey.isPressed ? 1 : 0) - (k != null && k.wKey.isPressed ? 1 : 0) - (g?.leftStick.y.ReadValue() ?? 0), -1, 1),
                Roll = Clamp((k != null && k.dKey.isPressed ? 1 : 0) - (k != null && k.aKey.isPressed ? 1 : 0) + (g?.leftStick.x.ReadValue() ?? 0), -1, 1),
                Yaw = Clamp((k != null && k.eKey.isPressed ? 1 : 0) - (k != null && k.qKey.isPressed ? 1 : 0) + (g?.rightStick.x.ReadValue() ?? 0), -1, 1),
                Brake = (k != null && k.spaceKey.isPressed) || (g != null && g.buttonSouth.isPressed) ? 1 : 0 };
        }

        /// <summary>Clamp summed keyboard/controller axes without changing their documented positive directions.</summary>
        private static float Clamp(float value, float low, float high) => Math.Max(low, Math.Min(high, value));
    }
}
