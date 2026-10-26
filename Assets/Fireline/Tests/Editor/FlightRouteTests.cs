using System;
using Fireline.Server;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Guard the QA pilot's state machine and the actual bounded authority route without claiming multiplayer/rendered acceptance.</summary>
    public sealed class FlightRouteTests
    {
        /// <summary>A valid stationary body cannot receive undefined trim values or rotate before the speed threshold.</summary>
        [Test]
        public void PilotStartsWithFiniteGroundRollAndRejectsMalformedState()
        {
            var pilot = new FlightRoutePilot(); var state = new FlightAuthority().State;
            var command = pilot.Command(state); command.Validate();
            Assert.That(command.Pitch, Is.Zero); Assert.That(command.Throttle, Is.EqualTo(1));
            Assert.That(command.Flaps, Is.EqualTo(1)); Assert.That(command.Gear, Is.EqualTo(1)); Assert.That(pilot.Brake, Is.Zero);
            state.PositionM.Y = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => pilot.Command(state));
        }

        /// <summary>Approach stays latched and low-height throttle cut cannot restart even if the body bounces after contact.</summary>
        [Test]
        public void PilotCutsThrottleOnlyAfterAirborneApproachAndDoesNotRestart()
        {
            var pilot = new FlightRoutePilot(); var state = new FlightAuthority().State;
            state.VelocityMps.Z = 50; Assert.That(pilot.Command(state).Pitch, Is.EqualTo(1));
            state.PositionM.Y = 6; pilot.Command(state); Assert.That(pilot.Phase, Is.EqualTo(1));
            state.PositionM.Z = 1500; pilot.Command(state); Assert.That(pilot.Phase, Is.EqualTo(2));
            state.PositionM.Y = 2; Assert.That(pilot.Command(state).Throttle, Is.Zero); Assert.That(pilot.Brake, Is.EqualTo(1));
            state.PositionM.Y = 4; state.PositionM.Z = 1300;
            Assert.That(pilot.Command(state).Throttle, Is.Zero); Assert.That(pilot.Phase, Is.EqualTo(3));
        }

        /// <summary>Real submit/advance/fuel/contact accepts a complete calm-air route and five settled seconds without time discard.</summary>
        [Test]
        public void AuthorityRouteActuallyTakesOffLandsAndSettlesInsideTheFiniteRunway()
        {
            var route = FlightRouteVerification.Run();
            Assert.That(route.passed, Is.True); Assert.That(route.longestAirborneSeconds, Is.GreaterThanOrEqualTo(5));
            Assert.That(route.stoppedSeconds, Is.GreaterThanOrEqualTo(5)); Assert.That(route.maximumHeightM, Is.GreaterThan(5));
            var last = route.samples[route.samples.Length - 1];
            Assert.That(last.state.VelocityMps.Length, Is.LessThan(.2)); Assert.That(last.supportedWheels, Is.EqualTo(3));
            Assert.That(last.state.PositionM.Z, Is.LessThan(2490)); Assert.That(route.consumedFuelKg, Is.GreaterThan(1));
            Assert.That(last.tick, Is.EqualTo((ulong)route.samples.Length));
        }
    }
}
