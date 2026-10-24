using System;
using Fireline.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fireline.Tests
{
    /// <summary>Real Input System state-event/edge tests using the installed package fixture in its supported PlayMode.</summary>
    public sealed class FlightDevicePlayTests : InputTestFixture
    {
        /// <summary>Read actual queued keyboard states with correct control signs and neutralize held throttle on focus loss.</summary>
        [Test]
        public void KeyboardMappingAndFocusNeutralizationUseActualInputEvents()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>(); var gamepad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var controls=new FlightTestControls(keyboard,gamepad);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift,Key.S,Key.D,Key.E,Key.Space)); InputSystem.Update();
                var c=controls.Sample(.1,true,true);
                Assert.That(c.Throttle,Is.EqualTo(.03f).Within(1e-6)); Assert.That(c.Pitch,Is.EqualTo(1)); Assert.That(c.Roll,Is.EqualTo(1)); Assert.That(c.Yaw,Is.EqualTo(1)); Assert.That(c.Brake,Is.EqualTo(1));
                c=controls.Sample(.1,false,true); Assert.That(c.Throttle,Is.Zero); Assert.That(c.Pitch,Is.Zero); Assert.That(c.Brake,Is.EqualTo(1));
                Assert.That(controls.Claim,Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); }
        }

        /// <summary>Use queued gamepad axes/triggers/buttons, including observer seat/camera actions and owned gear/flaps.</summary>
        [Test]
        public void GamepadMappingAndSeatControlsUseActualInputEvents()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>(); var gamepad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var controls=new FlightTestControls(keyboard,gamepad);
                // Establish a neutral input update before checking first-frame button edges on a newly added device.
                InputSystem.QueueStateEvent(gamepad,new GamepadState()); InputSystem.Update(); controls.Sample(.01,true,false);
                var state=new GamepadState { rightTrigger=1,leftStick=new Vector2(1,1),rightStick=new Vector2(1,0) }
                    .WithButton(GamepadButton.Start).WithButton(GamepadButton.RightShoulder).WithButton(GamepadButton.North).WithButton(GamepadButton.DpadUp);
                InputSystem.QueueStateEvent(gamepad,state); InputSystem.Update(); var c=controls.Sample(.1,true,true);
                Assert.That(controls.Claim,Is.True); Assert.That(controls.Camera,Is.True); Assert.That(c.Throttle,Is.GreaterThan(0));
                Assert.That(c.Pitch,Is.LessThan(0)); Assert.That(c.Roll,Is.GreaterThan(0)); Assert.That(c.Yaw,Is.GreaterThan(0));
                Assert.That(c.Gear,Is.Zero); Assert.That(c.Flaps,Is.EqualTo(.5f));
                c=controls.Sample(.1,true,false); Assert.That(c.Throttle,Is.Zero); Assert.That(c.Brake,Is.EqualTo(1)); Assert.That(c.Gear,Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); }
        }

        /// <summary>A sleep-sized delta cannot jump throttle to full, and opposing summed axes remain normalized.</summary>
        [Test]
        public void StalledFrameInputIsBoundedAndInvalidTimeRejected()
        {
            var k=InputSystem.AddDevice<Keyboard>(); var g=InputSystem.AddDevice<Gamepad>();
            try
            {
                var controls=new FlightTestControls(k,g);
                InputSystem.QueueStateEvent(k,new KeyboardState(Key.LeftShift,Key.D));
                InputSystem.QueueStateEvent(g,new GamepadState { rightTrigger=1,leftStick=Vector2.right }); InputSystem.Update();
                var c=controls.Sample(10,true,true); Assert.That(c.Throttle,Is.EqualTo(.06f).Within(1e-6)); Assert.That(c.Roll,Is.EqualTo(1));
                Assert.Throws<ArgumentOutOfRangeException>(()=>controls.Sample(double.NaN,true,true));
            }
            finally { InputSystem.RemoveDevice(k); InputSystem.RemoveDevice(g); }
        }

    }
}
