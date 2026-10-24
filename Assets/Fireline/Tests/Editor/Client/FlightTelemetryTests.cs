using System;
using System.Globalization;
using Fireline.Client;
using Fireline.Shared;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Independent instrument conventions, wind-relative speed and rejected metadata without any client integration.</summary>
    public sealed class FlightTelemetryTests
    {
        /// <summary>Return finite version-two source data with deliberate vertical speed and horizontal wind.</summary>
        private static FlightStateRpc Source() => new FlightStateRpc { Version = FlightWire.Version, Qw = 1,
            Vx = 3, Vy = 4, Vz = 12, WindX = 3, FuelKg = 400, WaterKg = 2000, Y = 123.4, Tick = 99, FireTick = 2, WindRevision = 7 };

        /// <summary>Airspeed subtracts the actual wind, ground speed excludes vertical motion, and mass/units retain the server values.</summary>
        [Test]
        public void WindAndVerticalVelocityHaveDistinctInstrumentMeanings()
        {
            var data = FlightTelemetry.Read(Source());
            Assert.That(data.AirspeedMps, Is.EqualTo(Math.Sqrt(160)).Within(1e-12));
            Assert.That(data.GroundSpeedMps, Is.EqualTo(Math.Sqrt(153)).Within(1e-12));
            Assert.That(data.VerticalMps, Is.EqualTo(4)); Assert.That(data.CogAltitudeM, Is.EqualTo(123.4));
            Assert.That(data.MassKg, Is.EqualTo(12400)); Assert.That(data.FuelKg, Is.EqualTo(400));
            Assert.That(data.CogZM, Is.EqualTo(2080.0 / 12400).Within(1e-12)); Assert.That(data.WindRevision, Is.EqualTo(7));
        }

        /// <summary>Quaternion orientation produces north/east compass headings and documented nose-up/right-wing-down signs.</summary>
        [Test]
        public void CombinedAttitudeUsesDocumentedAviationSigns()
        {
            double radians = Math.PI / 180;
            var q = FlightRotation.FromRotationVector(new FlightVector(0, 90*radians, 0))
                * FlightRotation.FromRotationVector(new FlightVector(-10*radians, 0, 0))
                * FlightRotation.FromRotationVector(new FlightVector(0, 0, -15*radians));
            var source = Source(); source.Qx=q.X; source.Qy=q.Y; source.Qz=q.Z; source.Qw=q.W;
            var data = FlightTelemetry.Read(source);
            Assert.That(data.HeadingDegrees, Is.EqualTo(90).Within(1e-10));
            Assert.That(data.PitchDegrees, Is.EqualTo(10).Within(1e-10)); Assert.That(data.BankDegrees, Is.EqualTo(15).Within(1e-10));
        }

        /// <summary>West headings normalize into the compass range; zero speed/wind and vertical attitude remain finite.</summary>
        [Test]
        public void CompassWrapAndVerticalLimitDoNotProduceNan()
        {
            var source = Source(); var q = FlightRotation.FromRotationVector(new FlightVector(0,-Math.PI/2,0));
            source.Qy=q.Y; source.Qw=q.W;
            Assert.That(FlightTelemetry.Read(source).HeadingDegrees, Is.EqualTo(270).Within(1e-10));
            q = FlightRotation.FromRotationVector(new FlightVector(-Math.PI/2,0,0));
            source.Qx=q.X; source.Qy=0; source.Qw=q.W; source.Vx=source.Vy=source.Vz=source.WindX=0;
            var data = FlightTelemetry.Read(source);
            Assert.That(data.PitchDegrees, Is.EqualTo(90).Within(1e-6)); Assert.That(data.AirspeedMps, Is.Zero);
            Assert.That(double.IsNaN(data.BankDegrees) || double.IsInfinity(data.HeadingDegrees), Is.False);
        }

        /// <summary>Invalid wind cannot enter the replica or display as plausible airspeed.</summary>
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(101)]
        public void InvalidWindIsRejected(double east)
        {
            var source = Source(); source.WindX=east;
            Assert.Throws<ArgumentOutOfRangeException>(()=>FlightTelemetry.Read(source));
        }

        /// <summary>Protocol-one snapshots cannot silently omit wind, and invalid attitude/fuel still fail through the shared physical validator.</summary>
        [Test]
        public void OldVersionAndMalformedStateFailBeforeDisplay()
        {
            var source = Source(); source.Version=1;
            Assert.Throws<ArgumentException>(()=>FlightTelemetry.Read(source));
            source=Source(); source.Qw=0;
            Assert.Throws<ArgumentOutOfRangeException>(()=>FlightTelemetry.Read(source));
            source=Source(); source.FuelKg=-1;
            Assert.Throws<ArgumentOutOfRangeException>(()=>FlightTelemetry.Read(source));
        }

        /// <summary>Displayed text keeps explicit SI labels and decimal dots even under a comma-decimal OS culture.</summary>
        [Test]
        public void TextFormattingDoesNotDependOnOsDecimalSeparator()
        {
            var previous=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("hr-HR");
                string text=FlightTelemetry.Format(FlightTelemetry.Read(Source()));
                Assert.That(text, Does.Contain("Fuel 400.00 / Water 2000.00 kg"));
                Assert.That(text, Does.Contain("Ground (horizontal) 12.37 m/s"));
                Assert.That(text, Does.Contain("CoG 123.40 m")); Assert.That(text.Split('\n').Length, Is.EqualTo(9));
            }
            finally { CultureInfo.CurrentCulture=previous; }
        }
    }
}
