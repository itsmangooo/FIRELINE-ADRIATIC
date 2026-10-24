using System;
using Fireline.Server;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Resource exhaustion, atomic failure, changing CoG frame and bounded server-clock fuel accounting.</summary>
    public sealed class FuelFlightTests
    {
        /// <summary>Burn follows requested throttle and physical duration, changes mass/inertia, and never touches water.</summary>
        [TestCase(0)] [TestCase(.5)] [TestCase(1)]
        public void BurnUpdatesMassOnlyForPoweredPhysicalTime(double throttle)
        {
            var initial = FlightVerification.Fixture(2000);
            var result = FuelFlight.Step(initial, new FlightInput { Throttle = throttle }, new FlightVector(), 1.225, .005, 0, RunwayPlane.Debug);
            double expected = .25 * throttle * .005;
            Assert.That(result.BurnedKg, Is.EqualTo(expected));
            Assert.That(result.State.FuelKg, Is.EqualTo(400 - expected));
            Assert.That(result.State.WaterKg, Is.EqualTo(2000));
            Assert.That(result.EffectiveThrottle, Is.EqualTo(throttle));
            Assert.That(FlightDynamics.Mass(result.State.FuelKg, 2000).Kg, Is.EqualTo(12400 - expected).Within(1e-10));
            Assert.That(initial.FuelKg, Is.EqualTo(400));
        }

        /// <summary>The last insufficient fuel packet provides proportional mean thrust, then the empty tank remains off.</summary>
        [Test]
        public void PartialLastStepCannotProduceFreeThrustOrNegativeFuel()
        {
            var state = FlightVerification.Fixture(); state.FuelKg = .000625;
            var first = FuelFlight.Step(state, new FlightInput { Throttle = 1 }, new FlightVector(), 1.225, .005, 0, RunwayPlane.Debug);
            Assert.That(first.BurnedKg, Is.EqualTo(.000625)); Assert.That(first.EffectiveThrottle, Is.EqualTo(.5));
            Assert.That(first.State.FuelKg, Is.Zero);
            var second = FuelFlight.Step(first.State, new FlightInput { Throttle = 1 }, new FlightVector(), 1.225, .005, 0, RunwayPlane.Debug);
            Assert.That(second.BurnedKg, Is.Zero); Assert.That(second.EffectiveThrottle, Is.Zero);
            Assert.That(FlightDynamics.Evaluate(second.State, new FlightInput(), new FlightVector(), 1.225).ThrustN, Is.Zero);
        }

        /// <summary>Changing CoG preserves every dry-body point and its rotational velocity at arbitrary attitude and angular rate.</summary>
        [Test]
        public void RebasePreservesRigidBodyGeometryAndPointVelocities()
        {
            var initial = FlightVerification.Fixture(2000);
            initial.Attitude = FlightRotation.FromRotationVector(new FlightVector(.2, -.4, .3));
            initial.AngularVelocityRadps = new FlightVector(.1, -.2, .3);
            var after = FuelFlight.RebaseFuel(initial, 0);
            double oldCg = FlightDynamics.Mass(initial.FuelKg, initial.WaterKg).CgZ;
            double newCg = FlightDynamics.Mass(after.FuelKg, after.WaterKg).CgZ;
            foreach (var point in new[] { new FlightVector(), DebugRunwayProfile.Wheel(0), DebugRunwayProfile.Wheel(1), DebugRunwayProfile.Wheel(2) })
            {
                var oldLever = point - new FlightVector(0, 0, oldCg);
                var newLever = point - new FlightVector(0, 0, newCg);
                Assert.That((initial.PositionM + initial.Attitude.Rotate(oldLever) - after.PositionM - after.Attitude.Rotate(newLever)).Length, Is.LessThan(1e-10));
                var oldVelocity = initial.VelocityMps + initial.Attitude.Rotate(FlightVector.Cross(initial.AngularVelocityRadps, oldLever));
                var newVelocity = after.VelocityMps + after.Attitude.Rotate(FlightVector.Cross(after.AngularVelocityRadps, newLever));
                Assert.That((oldVelocity - newVelocity).Length, Is.LessThan(1e-10));
            }
            Assert.That(after.AngularVelocityRadps.X, Is.EqualTo(initial.AngularVelocityRadps.X));
        }

        /// <summary>Invalid commands, duration and overloaded contact cannot mutate caller state or consume fuel.</summary>
        [Test]
        public void RejectedCandidatesConsumeNothing()
        {
            var state = FlightVerification.Fixture(); state.PositionM.Y = .8;
            Assert.Throws<InvalidOperationException>(() => FuelFlight.Step(state, new FlightInput { Gear = 1, Throttle = 1 }, new FlightVector(), 1.225, .005, 0, RunwayPlane.Debug));
            Assert.Throws<ArgumentOutOfRangeException>(() => FuelFlight.Step(state, new FlightInput { Throttle = double.NaN }, new FlightVector(), 1.225, .005, 0, RunwayPlane.Debug));
            Assert.Throws<ArgumentOutOfRangeException>(() => FuelFlight.Step(state, new FlightInput(), new FlightVector(), 1.225, .02, 0, RunwayPlane.Debug));
            Assert.That(state.FuelKg, Is.EqualTo(400)); Assert.That(state.PositionM.Y, Is.EqualTo(.8));
            Assert.Throws<ArgumentOutOfRangeException>(() => FuelFlight.RebaseFuel(state, 401));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FlightAuthority(double.NaN));
        }

        /// <summary>A large callback gap consumes only eight accepted steps; lease expiry and vacancy cannot burn fuel.</summary>
        [Test]
        public void ServerDiscardedWallTimeAndExpiredControlsCannotBurnFuel()
        {
            var host = new FlightAuthority(); var wind = new WindField(415); host.Claim(10);
            host.Submit(10, host.Epoch, 1, host.Tick, new FlightInput { Gear = 1, Throttle = 1 }, 0, 0);
            Assert.That(host.Advance(10, .1, wind, 0), Is.EqualTo(8));
            Assert.That(host.ConsumedFuelKg, Is.EqualTo(.01).Within(1e-10));
            double fuel = host.State.FuelKg;
            host.Advance(.04, .6, wind, 0);
            Assert.That(host.State.FuelKg, Is.EqualTo(fuel)); Assert.That(host.Expirations, Is.EqualTo(1));
            host.Release(10, host.Epoch); host.Advance(.04, .64, wind, 0);
            Assert.That(host.State.FuelKg, Is.EqualTo(fuel));
        }

        /// <summary>An empty server aircraft accepts valid controls but follows exactly a zero-thrust contact reference.</summary>
        [Test]
        public void EmptyTankCannotBeBypassedByPilotInput()
        {
            var host = new FlightAuthority(0); var wind = new WindField(415); host.Claim(10);
            host.Submit(10, host.Epoch, 1, host.Tick, new FlightInput { Gear = 1, Throttle = 1 }, 0, 0);
            var reference = RunwayContact.Step(host.State, new FlightInput { Gear = 1 }, new FlightVector(), 1.225, .005, 0, RunwayPlane.Debug);
            host.Advance(.005, .005, wind, 0);
            Assert.That(host.State.PositionM.Z, Is.EqualTo(reference.PositionM.Z));
            Assert.That(host.State.VelocityMps.Z, Is.EqualTo(reference.VelocityMps.Z)); Assert.That(host.ConsumedFuelKg, Is.Zero);
        }
    }
}
