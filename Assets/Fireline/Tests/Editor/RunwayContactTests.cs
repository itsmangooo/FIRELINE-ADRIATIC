using System;
using Fireline.Shared;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Independent force, equilibrium, dissipation, envelope and compatibility checks for the estimated gear model.</summary>
    public sealed class RunwayContactTests
    {
        /// <summary>Build a generic SI body independent of the graphical aircraft and without fuel-induced CoG shift.</summary>
        public static FlightState Fixture(double height = 2.05, double speed = 0) => new FlightState {
            PositionM = new FlightVector(0, height, 0), VelocityMps = new FlightVector(0, 0, speed), Attitude = FlightRotation.Identity };

        /// <summary>At analytically calculated static compression the three reactions equal weight and all moments cancel.</summary>
        [Test]
        public void StaticEquilibriumMatchesWeightAndSymmetricMoments()
        {
            double weight = DebugFlightProfile.DryMassKg * DebugFlightProfile.GravityMps2;
            var state = Fixture(2 - weight / (3 * DebugRunwayProfile.SpringNPerM));
            var contact = RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 1, RunwayPlane.Debug);
            Assert.That(contact.NormalN, Is.EqualTo(weight).Within(1e-8));
            Assert.That(contact.BodyMomentNm.Length, Is.LessThan(1e-8));
            var next = RunwayContact.Step(state, new FlightInput { Gear = 1 }, new FlightVector(), 0, .005, 1, RunwayPlane.Debug);
            Assert.That((next.PositionM - state.PositionM).Length, Is.LessThan(1e-12));
            Assert.That(next.VelocityMps.Length, Is.LessThan(1e-12));
        }

        /// <summary>No nearby surface force means exactly the existing aerodynamic/gravity trajectory, rather than a changed flight model.</summary>
        [Test]
        public void AirbornePathMatchesExistingSolverExactly()
        {
            var old = Fixture(1000, 60); var contact = old;
            var input = new FlightInput { Gear = 1, Throttle = .5, Pitch = .1 };
            for (int i = 0; i < 400; i++)
            {
                old = FlightDynamics.Step(old, input, new FlightVector(8, 0, 0), 1.225, .005);
                contact = RunwayContact.Step(contact, input, new FlightVector(8, 0, 0), 1.225, .005, 0, RunwayPlane.Debug);
                Assert.That(contact.PositionM.X, Is.EqualTo(old.PositionM.X)); Assert.That(contact.PositionM.Y, Is.EqualTo(old.PositionM.Y));
                Assert.That(contact.PositionM.Z, Is.EqualTo(old.PositionM.Z)); Assert.That(contact.Attitude.W, Is.EqualTo(old.Attitude.W));
            }
        }

        /// <summary>Loads never pull a departing tire toward the surface, while body angular velocity contributes to wheel velocity.</summary>
        [Test]
        public void ContactIsUnilateralAndUsesRotatingPointVelocity()
        {
            var state = Fixture(1.9); state.VelocityMps.Y = 10;
            var contact = RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 0, RunwayPlane.Debug);
            Assert.That(contact.NormalN, Is.Zero);
            state.VelocityMps = new FlightVector(); state.AngularVelocityRadps.X = .2;
            contact = RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 0, RunwayPlane.Debug);
            Assert.That(contact.Nose.VelocityMps.Y, Is.EqualTo(-1.2).Within(1e-12));
            Assert.That(contact.LeftMain.VelocityMps.Y, Is.EqualTo(.6).Within(1e-12));
        }

        /// <summary>Combined longitudinal/lateral tire demands obey the load ellipse and remove kinetic energy at every wheel.</summary>
        [Test]
        public void FrictionIsDissipativeAndCoulombLimited()
        {
            var state = Fixture(1.8, 25); state.VelocityMps.X = 12;
            var contact = RunwayContact.Evaluate(state, new FlightInput { Gear = 1, Yaw = .4 }, 1, RunwayPlane.Debug);
            int i = 0;
            foreach (var wheel in new[] { contact.LeftMain, contact.RightMain, contact.Nose })
            {
                double mu = i++ < 2 ? DebugRunwayProfile.BrakedFriction : DebugRunwayProfile.RollingFriction;
                double norm = Math.Pow(wheel.LongitudinalN / (mu * wheel.NormalN), 2)
                    + Math.Pow(wheel.LateralN / (DebugRunwayProfile.LateralFriction * wheel.NormalN), 2);
                Assert.That(norm, Is.LessThanOrEqualTo(1 + 1e-12));
                Assert.That(wheel.FrictionPowerW, Is.LessThanOrEqualTo(0));
            }
        }

        /// <summary>Steering a loaded nose wheel right produces a rightward yaw reaction without client-side state mutation.</summary>
        [Test]
        public void NoseSteeringProducesExpectedYawMoment()
        {
            var contact = RunwayContact.Evaluate(Fixture(1.85, 10), new FlightInput { Gear = 1, Yaw = .5 }, 0, RunwayPlane.Debug);
            Assert.That(contact.BodyMomentNm.Y, Is.GreaterThan(0));
            Assert.That(contact.FrictionPowerW, Is.LessThan(0));
        }

        /// <summary>The runway is finite and gear must be extended; neither creates an invisible infinite safety floor.</summary>
        [Test]
        public void SurfaceBoundsAndGearRetractionAreExplicit()
        {
            var state = Fixture(1.85); state.PositionM.X = 100;
            Assert.That(RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 1, RunwayPlane.Debug).NormalN, Is.Zero);
            state.PositionM.X = 0;
            Assert.That(RunwayContact.Evaluate(state, new FlightInput { Gear = .99 }, 1, RunwayPlane.Debug).NormalN, Is.Zero);
            state.PositionM.Z = 3000;
            Assert.That(RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 1, RunwayPlane.Debug).NormalN, Is.Zero);
        }

        /// <summary>Brake dissipates forward speed and shortens travel under matched mass, surface, atmosphere and time step.</summary>
        [Test]
        public void BrakesReduceSpeedAndTravelWithoutChangingMass()
        {
            var braked = Fixture(1.85, 25); var coast = braked; var input = new FlightInput { Gear = 1 };
            for (int i = 0; i < 2000; i++)
            {
                braked = RunwayContact.Step(braked, input, new FlightVector(), 0, .005, 1, RunwayPlane.Debug);
                coast = RunwayContact.Step(coast, input, new FlightVector(), 0, .005, 0, RunwayPlane.Debug);
            }
            Assert.That(Math.Abs(braked.VelocityMps.Z), Is.LessThan(.2));
            Assert.That(braked.PositionM.Z, Is.LessThan(coast.PositionM.Z));
            Assert.That(braked.FuelKg, Is.Zero); Assert.That(braked.WaterKg, Is.Zero);
        }

        /// <summary>Supported rest remains bounded at two substep sizes; payload increases required compression instead of passing through the ground.</summary>
        [TestCase(.005, 0)]
        [TestCase(.0025, 4000)]
        public void RestSettlesAndMassIsPreserved(double dt, double waterKg)
        {
            var state = Fixture(); state.WaterKg = waterKg;
            for (int i = 0; i < (int)(10 / dt); i++) state = RunwayContact.Step(state, new FlightInput { Gear = 1 }, new FlightVector(), 0, dt, 1, RunwayPlane.Debug);
            var contact = RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 1, RunwayPlane.Debug);
            Assert.That(state.PositionM.Y, Is.InRange(1.7, 2)); Assert.That(state.VelocityMps.Length, Is.LessThan(1e-5));
            Assert.That(contact.NormalN, Is.EqualTo(FlightDynamics.Mass(0, waterKg).Kg * DebugFlightProfile.GravityMps2).Within(.1));
            Assert.That(state.WaterKg, Is.EqualTo(waterKg));
        }

        /// <summary>Reject excessive step, invalid brake/geometry, belly impact and excessive compression before publishing a candidate.</summary>
        [Test]
        public void InvalidAndUnsupportedStatesFailWithoutMutatingInput()
        {
            var state = Fixture(1.3); var original = state;
            Assert.Throws<ArgumentOutOfRangeException>(() => RunwayContact.Step(state, new FlightInput { Gear = 1 }, new FlightVector(), 0, .02, 0, RunwayPlane.Debug));
            Assert.Throws<ArgumentOutOfRangeException>(() => RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, double.NaN, RunwayPlane.Debug));
            Assert.Throws<ArgumentOutOfRangeException>(() => RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 0, new RunwayPlane()));
            state.PositionM.Y = 1.2;
            Assert.Throws<InvalidOperationException>(() => RunwayContact.Evaluate(state, new FlightInput { Gear = 1 }, 0, RunwayPlane.Debug));
            state = original; state.VelocityMps.Y = -100;
            Assert.Throws<InvalidOperationException>(() => RunwayContact.Step(state, new FlightInput { Gear = 1 }, new FlightVector(), 0, .005, 0, RunwayPlane.Debug));
            Assert.That(state.PositionM.Y, Is.EqualTo(original.PositionM.Y));
        }

        /// <summary>The runway verifier has an explicit nonempty, isolated server-only startup contract.</summary>
        [Test]
        public void RunwayProofCannotShareOtherExitOrPersistenceModes()
        {
            Assert.That(LaunchOptions.Parse(new[] { "--fireline-runway-proof", "runway.json" }).RunwayProofPath, Is.EqualTo("runway.json"));
            foreach (string flag in new[] { "--fireline-flight-proof", "--fireline-sim-proof", "--fireline-replication-proof", "--fireline-world-load", "--fireline-world-save", "--fireline-smoke-seconds" })
                Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-runway-proof", "runway.json", flag, "1" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-runway-proof", " " }));
        }
    }
}
