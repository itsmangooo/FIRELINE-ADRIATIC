using System;
using Fireline.Server;
using Fireline.Shared;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Authority boundary, command lifetime, bounded clock and unchanged solver checks independent of Unity callbacks.</summary>
    public sealed class FlightAuthorityTests
    {
        /// <summary>Evidence must contain actual numeric snapshots rather than silently serializing an unsupported struct to an empty object.</summary>
        [Test]
        public void SnapshotEvidenceRetainsPhysicalAndOwnershipFields()
        {
            var frame = new FlightStateRpc { Version = 1, Owner = 10, Epoch = 7, Tick = 1234, FireTick = 30,
                Z = 12.34567890123456, Qw = 1, FuelKg = 400, WaterKg = 2500 };
            string json = UnityEngine.JsonUtility.ToJson(frame);
            var read = UnityEngine.JsonUtility.FromJson<FlightStateRpc>(json);
            Assert.That(json, Does.Contain("\"Qw\"")); Assert.That(read.Tick, Is.EqualTo(frame.Tick));
            Assert.That(read.Owner, Is.EqualTo(frame.Owner)); Assert.That(read.Z, Is.EqualTo(frame.Z));
            Assert.That(read.WaterKg, Is.EqualTo(2500));
        }
        /// <summary>Issue a finite full-throttle command using the currently observed server state.</summary>
        private static FlightResult Command(FlightAuthority host, int identity = 10, uint serial = 1, double now = 1)
            => host.Submit(identity, host.Epoch, serial, host.Tick, new FlightInput { Gear = 1, Throttle = 1 }, 0, now);

        /// <summary>A second identity cannot occupy, release or command the first pilot's seat.</summary>
        [Test]
        public void OnlyTheActualOwnerMayCommandAndRelease()
        {
            var host = new FlightAuthority(); Assert.That(host.Claim(0), Is.EqualTo(FlightResult.Invalid));
            Assert.That(host.Claim(10), Is.EqualTo(FlightResult.Accepted)); uint epoch = host.Epoch;
            Assert.That(host.Claim(10), Is.EqualTo(FlightResult.Accepted)); Assert.That(host.Epoch, Is.EqualTo(epoch));
            Assert.That(host.Claim(20), Is.EqualTo(FlightResult.NotOwner));
            Assert.That(Command(host, 20), Is.EqualTo(FlightResult.NotOwner));
            Assert.That(host.Release(20, epoch), Is.EqualTo(FlightResult.NotOwner));
            Assert.That(host.Sequence, Is.Zero); Assert.That(host.Owner, Is.EqualTo(10));
        }

        /// <summary>Reusing a seat after disconnect invalidates queued old-generation commands even for the same identity.</summary>
        [Test]
        public void SeatGenerationRejectsPreviousOccupantPackets()
        {
            var host = new FlightAuthority(); host.Claim(10); uint previous = host.Epoch;
            Assert.That(Command(host), Is.EqualTo(FlightResult.Accepted));
            host.Release(10, previous); host.Claim(10);
            Assert.That(host.Epoch, Is.GreaterThan(previous));
            Assert.That(host.Submit(10, previous, 2, host.Tick, new FlightInput { Gear = 1 }, 1, 2), Is.EqualTo(FlightResult.NotOwner));
            Assert.That(Command(host, serial: 1, now: 2), Is.EqualTo(FlightResult.Accepted));
        }

        /// <summary>Commands with zero/repeated/decreasing serials never replace the last accepted command or refresh its lease.</summary>
        [TestCase(0u)] [TestCase(1u)] [TestCase(2u)]
        public void DuplicateAndReorderedSequencesAreRejected(uint serial)
        {
            var host = new FlightAuthority(); host.Claim(10); Command(host, serial: 2);
            Assert.That(Command(host, serial: serial, now: 2), Is.EqualTo(FlightResult.Sequence));
            Assert.That(host.Sequence, Is.EqualTo(2));
        }

        /// <summary>Reject malformed controls without consuming the sequence so a later corrected command can use it.</summary>
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-.1)] [TestCase(1.1)]
        public void NonfiniteAndOutOfRangeControlsFailWithoutMutation(double throttle)
        {
            var host = new FlightAuthority(); host.Claim(10);
            Assert.That(host.Submit(10, host.Epoch, 1, 0, new FlightInput { Gear = 1, Throttle = throttle }, 0, 1), Is.EqualTo(FlightResult.Invalid));
            Assert.That(host.Sequence, Is.Zero); Assert.That(Command(host), Is.EqualTo(FlightResult.Accepted));
        }

        /// <summary>Future ticks and observations older than one physical second are rejected before controls are installed.</summary>
        [Test]
        public void StaleAndFutureObservationsAreRejected()
        {
            var host = new FlightAuthority(); host.Claim(10); var wind = new WindField(415);
            Assert.That(host.Submit(10, host.Epoch, 1, 1, new FlightInput { Gear = 1 }, 0, 0), Is.EqualTo(FlightResult.Tick));
            for (int i = 0; i < 30; i++) host.Advance(.04, i * .04, wind, 0);
            Assert.That(host.Tick, Is.EqualTo(240));
            Assert.That(host.Submit(10, host.Epoch, 1, 0, new FlightInput { Gear = 1 }, 0, 2), Is.EqualTo(FlightResult.Tick));
            Assert.That(Command(host, now: 2), Is.EqualTo(FlightResult.Accepted));
        }

        /// <summary>Bound accepted input rate and reject backward/nonfinite receipt time without advancing the serial.</summary>
        [Test]
        public void InputRateAndReceiptClockAreChecked()
        {
            var host = new FlightAuthority(); host.Claim(10); Command(host);
            Assert.That(Command(host, serial: 2, now: 1.001), Is.EqualTo(FlightResult.Rate));
            Assert.That(Command(host, serial: 2, now: .9), Is.EqualTo(FlightResult.Invalid));
            Assert.That(Command(host, serial: 2, now: double.NaN), Is.EqualTo(FlightResult.Invalid));
            Assert.That(Command(host, serial: 2, now: 1.02), Is.EqualTo(FlightResult.Accepted));
        }

        /// <summary>A ten-second sleep costs eight steps, records 9.96 discarded seconds and never replays stale throttle.</summary>
        [Test]
        public void SleepRecoveryHasBoundedWorkAndExpiresInput()
        {
            var host = new FlightAuthority(); host.Claim(10); Command(host, now: 0);
            var neutral = host.State;
            for (int i = 0; i < 8; i++) neutral = RunwayContact.Step(neutral, new FlightInput { Gear = 1 }, new FlightVector(), 1.225, .005, 1, RunwayPlane.Debug);
            Assert.That(host.Advance(10, 10, new WindField(415), 0), Is.EqualTo(8));
            Assert.That(host.Tick, Is.EqualTo(8)); Assert.That(host.DiscardedSeconds, Is.EqualTo(9.96).Within(1e-12));
            Assert.That(host.Overruns, Is.EqualTo(1)); Assert.That(host.Expirations, Is.EqualTo(1));
            Assert.That(host.State.VelocityMps.Z, Is.EqualTo(neutral.VelocityMps.Z));
            Assert.That(host.State.PositionM.Z, Is.EqualTo(neutral.PositionM.Z));
        }

        /// <summary>Partial intervals accumulate exactly, while invalid elapsed time fails before mutating state.</summary>
        [Test]
        public void ClockKeepsFractionalRemainderAndRejectsInvalidTime()
        {
            var host = new FlightAuthority(); var wind = new WindField(415);
            Assert.That(host.Advance(.002, .002, wind, 0), Is.Zero);
            Assert.That(host.Advance(.004, .006, wind, 0), Is.EqualTo(1));
            Assert.That(host.Advance(.004, .010, wind, 0), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => host.Advance(double.NaN, .01, wind, 0));
            Assert.That(host.Tick, Is.EqualTo(2));
        }

        /// <summary>The live adapter uses exactly the existing contact solver and authoritative spatial wind under equivalent inputs.</summary>
        [Test]
        public void AdapterMatchesDirectSolverWithServerWind()
        {
            var host = new FlightAuthority(); host.Claim(10); Command(host, now: 0);
            var direct = host.State; var wind = new WindField(415); wind.Set(8, 0, 2);
            var command = new FlightInput { Gear = 1, Throttle = 1 };
            for (int i = 0; i < 40; i++)
            {
                var p = direct.PositionM; var sampled = wind.Sample(p.X, p.Z, p.Y, 13);
                direct = RunwayContact.Step(direct, command, new FlightVector(sampled.X, 0, sampled.Z), 1.225, .005, 0, RunwayPlane.Debug);
                host.Advance(.005, (i + 1) * .005, wind, 13);
                Assert.That(host.State.PositionM.X, Is.EqualTo(direct.PositionM.X));
                Assert.That(host.State.PositionM.Z, Is.EqualTo(direct.PositionM.Z));
                Assert.That(host.State.Attitude.W, Is.EqualTo(direct.Attitude.W));
            }
            Assert.That(host.State.VelocityMps.Z, Is.GreaterThan(0)); Assert.That(host.Faulted, Is.False);
        }

        /// <summary>After lease expiry the same wind/solver follows exactly a neutral braked reference; rejected input cannot prolong thrust.</summary>
        [Test]
        public void RejectedInputDoesNotRefreshLease()
        {
            var host = new FlightAuthority(); var wind = new WindField(415); host.Claim(10); Command(host, now: 0);
            for (int i = 0; i < 80; i++) host.Advance(.005, i * .005, wind, 0);
            Assert.That(Command(host, 20, 2, .49), Is.EqualTo(FlightResult.NotOwner));
            var reference = host.State;
            reference = RunwayContact.Step(reference, new FlightInput { Gear = 1 }, new FlightVector(), 1.225, .005, 1, RunwayPlane.Debug);
            host.Advance(.005, .5, wind, 0);
            Assert.That(host.Expirations, Is.EqualTo(1)); Assert.That(host.State.VelocityMps.Z, Is.EqualTo(reference.VelocityMps.Z));
        }

        /// <summary>Unsupported gear-up ground impact freezes the last valid state, releases the pilot and prevents further physical steps.</summary>
        [Test]
        public void PhysicsEnvelopeFailureFreezesAndReleasesSeat()
        {
            var host = new FlightAuthority(); var wind = new WindField(415); host.Claim(10);
            for (uint i = 1; i < 200 && !host.Faulted; i++)
            {
                double now = i * .005;
                if (i % 5 == 1) Assert.That(host.Submit(10, host.Epoch, i, host.Tick, new FlightInput(), 0, now), Is.EqualTo(FlightResult.Accepted));
                host.Advance(.005, now, wind, 0);
            }
            Assert.That(host.Faulted, Is.True); Assert.That(host.Owner, Is.Zero);
            Assert.That(host.State.PositionM.Y, Is.GreaterThanOrEqualTo(.5));
            var state = host.State; ulong tick = host.Tick;
            Assert.That(host.Advance(.04, 2, wind, 0), Is.Zero);
            Assert.That(host.Tick, Is.EqualTo(tick)); Assert.That(host.State.PositionM.Y, Is.EqualTo(state.PositionM.Y));
            Assert.That(host.Claim(10), Is.EqualTo(FlightResult.Invalid));
        }

        /// <summary>Explicit live flight modes cannot collide with numerical proofs, persistence or other exit timers.</summary>
        [Test]
        public void FlightLaunchModesRequireExplicitCompatibleOptions()
        {
            Assert.That(LaunchOptions.Parse(Array.Empty<string>()).FlightSession, Is.False);
            Assert.That(LaunchOptions.Parse(new[] { "--fireline-flight-session", "debug" }).FlightSession, Is.True);
            foreach (string key in new[] { "--fireline-flight-proof", "--fireline-runway-proof", "--fireline-replication-proof", "--fireline-world-save" })
                Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-flight-session", "debug", key, "x.json" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-flight-probe-role", "owner" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-flight-network-proof", "x.json" }));
        }
    }
}
