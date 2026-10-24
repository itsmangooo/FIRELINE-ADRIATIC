using System;
using Fireline.Shared;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    public sealed class FlightDynamicsTests
    {
        /// <summary>Create a generic untrimmed test body at 1000 m with 400 kg fuel and 60 m/s forward velocity.</summary>
        public static FlightState Fixture(double water = 0) => new FlightState { PositionM = new FlightVector(0, 1000, 0),
            VelocityMps = new FlightVector(0, 0, 60), Attitude = FlightRotation.Identity, FuelKg = 400, WaterKg = water };

        /// <summary>Independent analytic dynamic pressure/force checks catch incorrect units, velocity exponent and drag direction.</summary>
        [Test]
        public void ForcesMatchAnalyticLiftDragGravityAndWindFrame()
        {
            var state = Fixture(); var f = FlightDynamics.Evaluate(state, new FlightInput(), new FlightVector(), 1.225);
            Assert.That(f.DynamicPressurePa, Is.EqualTo(2205).Within(1e-10));
            Assert.That(f.LiftN, Is.EqualTo(44100).Within(1e-8));
            Assert.That(f.DragN, Is.EqualTo(8246.7).Within(1e-8));
            Assert.That(f.WorldN.Y, Is.EqualTo(44100 - 10400 * 9.80665).Within(1e-8));
            Assert.That(FlightVector.Dot(f.AerodynamicWorldN, state.VelocityMps), Is.LessThan(0));
            state.VelocityMps += new FlightVector(8, 0, 10);
            var shifted = FlightDynamics.Evaluate(state, new FlightInput(), new FlightVector(8, 0, 10), 1.225);
            Assert.That((shifted.WorldN - f.WorldN).Length, Is.LessThan(1e-8));
            Assert.That((shifted.BodyMomentNm - f.BodyMomentNm).Length, Is.LessThan(1e-8));
        }

        /// <summary>Payload changes gravity, CoG, exact centerline parallel-axis inertia and lift acceleration without inventing aerodynamic data.</summary>
        [Test]
        public void PayloadHasExactMassCenterAndInertia()
        {
            var dry = FlightDynamics.Mass(400, 0); var full = FlightDynamics.Mass(400, 4000);
            Assert.That(dry.Kg, Is.EqualTo(10400)); Assert.That(full.Kg, Is.EqualTo(14400));
            Assert.That(full.CgZ, Is.EqualTo(4480.0 / 14400).Within(1e-12));
            double shift = 10000 * full.CgZ * full.CgZ + 400 * Math.Pow(-0.8 - full.CgZ, 2) + 4000 * Math.Pow(1.2 - full.CgZ, 2);
            Assert.That(full.InertiaKgM2.X, Is.EqualTo(50000 + shift).Within(1e-8));
            Assert.That(full.InertiaKgM2.Y, Is.EqualTo(100000 + shift).Within(1e-8));
            Assert.That(full.InertiaKgM2.Z, Is.EqualTo(120000));
            var a = FlightDynamics.Evaluate(Fixture(), new FlightInput(), new FlightVector(), 1.225);
            var b = FlightDynamics.Evaluate(Fixture(4000), new FlightInput(), new FlightVector(), 1.225);
            Assert.That(b.LiftN, Is.EqualTo(a.LiftN)); Assert.That(b.WorldN.Y / full.Kg, Is.LessThan(a.WorldN.Y / dry.Kg));
        }

        /// <summary>Lift is continuous through the estimated stall break and declines afterward; flaps/gear affect their intended coefficients.</summary>
        [Test]
        public void StallFlapsAndGearBehaveAsDocumented()
        {
            double edge = DebugFlightProfile.StallRadians;
            Assert.That(FlightDynamics.LiftCurve(edge + 1e-8, 0), Is.EqualTo(FlightDynamics.LiftCurve(edge, 0)).Within(1e-7));
            Assert.That(FlightDynamics.LiftCurve(30 * Math.PI / 180, 0), Is.LessThan(FlightDynamics.LiftCurve(edge, 0)));
            var f = FlightDynamics.Evaluate(Fixture(), new FlightInput(), new FlightVector(), 1.225);
            var flap = FlightDynamics.Evaluate(Fixture(), new FlightInput { Flaps = 1 }, new FlightVector(), 1.225);
            var gear = FlightDynamics.Evaluate(Fixture(), new FlightInput { Gear = 1 }, new FlightVector(), 1.225);
            Assert.That(flap.LiftN, Is.GreaterThan(f.LiftN)); Assert.That(flap.DragN, Is.GreaterThan(f.DragN));
            Assert.That(gear.LiftN, Is.EqualTo(f.LiftN)); Assert.That(gear.DragN - f.DragN, Is.EqualTo(5512.5).Within(1e-8));
        }

        /// <summary>With no atmosphere/thrust, semi-implicit motion matches the analytic discrete formula and converges toward ballistic position.</summary>
        [Test]
        public void BallisticTimeStepConvergenceAndNoInputMutation()
        {
            var initial = Fixture(); double previous = double.PositiveInfinity;
            foreach (double dt in new[] { 0.02, 0.01, 0.005 })
            {
                var state = initial; int steps = (int)Math.Round(4 / dt);
                for (int i = 0; i < steps; i++) state = FlightDynamics.Step(state, new FlightInput(), new FlightVector(), 0, dt);
                Assert.That(state.VelocityMps.Y, Is.EqualTo(-9.80665 * 4).Within(1e-8));
                double exact = 1000 - 0.5 * 9.80665 * 16;
                double error = Math.Abs(state.PositionM.Y - exact);
                Assert.That(error, Is.EqualTo(0.5 * 9.80665 * 4 * dt).Within(1e-8)); Assert.That(error, Is.LessThan(previous)); previous = error;
                Assert.That(state.Attitude.NormSquared, Is.EqualTo(1).Within(1e-12));
            }
            Assert.That(initial.PositionM.Y, Is.EqualTo(1000)); Assert.That(initial.VelocityMps.Y, Is.Zero);
        }

        /// <summary>Quaternion rotation/inverse and sustained control inputs exercise all three angular axes and preserve unit norm.</summary>
        [Test]
        public void AttitudeAndThreeAxisControlSigns()
        {
            var rotation = FlightRotation.FromRotationVector(new FlightVector(0, Math.PI / 2, 0));
            var forward = rotation.Rotate(new FlightVector(0, 0, 1));
            Assert.That(forward.X, Is.EqualTo(1).Within(1e-12));
            Assert.That((rotation.InverseRotate(forward) - new FlightVector(0, 0, 1)).Length, Is.LessThan(1e-12));
            var state = Fixture(); state.FuelKg = 0;
            var pitch = FlightDynamics.Step(state, new FlightInput { Pitch = 1 }, new FlightVector(), 1.225, 0.01);
            var roll = FlightDynamics.Step(state, new FlightInput { Roll = 1 }, new FlightVector(), 1.225, 0.01);
            var yaw = FlightDynamics.Step(state, new FlightInput { Yaw = 1 }, new FlightVector(), 1.225, 0.01);
            Assert.That(pitch.AngularVelocityRadps.X, Is.LessThan(0)); Assert.That(roll.AngularVelocityRadps.Z, Is.LessThan(0));
            Assert.That(yaw.AngularVelocityRadps.Y, Is.GreaterThan(0));
            for (int i = 0; i < 400; i++) state = FlightDynamics.Step(state, new FlightInput { Pitch = .2, Roll = .1, Yaw = .1, Throttle = .5 }, new FlightVector(8, 0, 0), 1.225, 0.005);
            Assert.That(state.Attitude.NormSquared, Is.EqualTo(1).Within(1e-12));
        }

        /// <summary>Nonzero coupled rotation must use omega cross I omega, not three independent torque/inertia divisions.</summary>
        [Test]
        public void EulerGyroscopicTermIsPresent()
        {
            var state = Fixture(); state.AngularVelocityRadps = new FlightVector(.1, .2, .3);
            var inertia = FlightDynamics.Mass(state.FuelKg, 0).InertiaKgM2;
            var cross = FlightVector.Cross(state.AngularVelocityRadps, new FlightVector(.1 * inertia.X, .2 * inertia.Y, .3 * inertia.Z));
            var next = FlightDynamics.Step(state, new FlightInput(), new FlightVector(), 0, .005);
            Assert.That(next.AngularVelocityRadps.X, Is.EqualTo(.1 - cross.X / inertia.X * .005).Within(1e-12));
            Assert.That(next.AngularVelocityRadps.Y, Is.EqualTo(.2 - cross.Y / inertia.Y * .005).Within(1e-12));
            Assert.That(next.AngularVelocityRadps.Z, Is.EqualTo(.3 - cross.Z / inertia.Z * .005).Within(1e-12));
        }

        /// <summary>Zero airspeed, vertical flow and reverse flow remain finite, with no unsupported reverse-flow lift.</summary>
        [Test]
        public void DegenerateFlowIsFinite()
        {
            foreach (var v in new[] { new FlightVector(), new FlightVector(0, 60, 0), new FlightVector(0, 0, -60) })
            {
                var state = Fixture(); state.VelocityMps = v;
                var f = FlightDynamics.Evaluate(state, new FlightInput(), new FlightVector(), 1.225);
                Assert.That(double.IsNaN(f.WorldN.Length), Is.False); Assert.That(double.IsInfinity(f.BodyMomentNm.Length), Is.False);
                Assert.That(f.LiftN, Is.Zero);
            }
        }

        /// <summary>Invalid external values and too-large steps throw before returning any candidate; the original value-type state is unchanged.</summary>
        [Test]
        public void InvalidStateCommandsMassAndStepAreRejected()
        {
            var state = Fixture();
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Mass(0, 4001));
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Mass(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Step(state, new FlightInput(), new FlightVector(), 1.225, .021));
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Evaluate(state, new FlightInput { Throttle = double.NaN }, new FlightVector(), 1.225));
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Evaluate(state, new FlightInput { Pitch = 2 }, new FlightVector(), 1.225));
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Evaluate(state, new FlightInput(), new FlightVector(double.PositiveInfinity, 0, 0), 1.225));
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Evaluate(state, new FlightInput(), new FlightVector(), -1));
            var bad = state; bad.Attitude = new FlightRotation();
            Assert.Throws<ArgumentOutOfRangeException>(() => FlightDynamics.Evaluate(bad, new FlightInput(), new FlightVector(), 1.225));
            Assert.That(state.PositionM.Y, Is.EqualTo(1000));
        }

        /// <summary>Flight verification is opt-in and cannot accidentally share the live checkpoint/fire/replication scenario.</summary>
        [Test]
        public void FlightProofOptionIsExclusive()
        {
            Assert.That(LaunchOptions.Parse(new[] { "--fireline-flight-proof", "result.json" }).FlightProofPath, Is.EqualTo("result.json"));
            foreach (string other in new[] { "--fireline-sim-proof", "--fireline-replication-proof", "--fireline-world-load", "--fireline-world-save", "--fireline-smoke-seconds" })
                Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-flight-proof", "result.json", other, "1" }));
        }
    }
}
