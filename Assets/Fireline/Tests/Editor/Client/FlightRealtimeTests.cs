using System;
using Fireline.Client;
using Fireline.Shared;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Guard explicit QA entry points and bounded delayed-replica controls; no production autopilot is implied.</summary>
    public sealed class FlightRealtimeTests
    {
        [Serializable]
        private sealed class RpcEnvelope { public FlightRequestRpc request; }
        /// <summary>Nested command evidence must retain the complete actual RPC rather than silently omitting a nonserializable struct.</summary>
        [Test]
        public void QueuedCommandEvidenceRetainsTheActualWireFields()
        {
            var expected=new FlightRequestRpc {Version=FlightWire.Version,Kind=3,Epoch=7,Sequence=42,Tick=99,
                Throttle=1,Pitch=.125f,Roll=-.25f,Yaw=.5f,Gear=1,Flaps=1,Brake=1};
            string json=UnityEngine.JsonUtility.ToJson(new RpcEnvelope {request=expected});
            Assert.That(json,Does.Contain("\"request\":"));
            Assert.That(UnityEngine.JsonUtility.FromJson<RpcEnvelope>(json).request,Is.EqualTo(expected));
        }
        /// <summary>Finite zero-speed data must roll safely, and airborne/approach/stop requests latch without integrating a pose.</summary>
        [Test]
        public void RouteRequestsAreBoundedAndStopDoesNotRestart()
        {
            var control=new FlightRouteControls(); var source=new FlightStateRpc {Version=FlightWire.Version,Qw=1,FuelKg=400,Y=2};
            Assert.That(control.Sample(source).Pitch,Is.Zero);
            source.Vz=50; Assert.That(control.Sample(source).Pitch,Is.EqualTo(1));
            source.Y=8; var request=control.Sample(source); Assert.That(control.Phase,Is.EqualTo(1));
            Assert.That(request.Pitch,Is.InRange(-1,1)); source.Z=1300; control.Sample(source);
            source.Y=2; Assert.That(control.Sample(source).Throttle,Is.Zero); Assert.That(control.Phase,Is.EqualTo(3));
            source.Y=10; Assert.That(control.Sample(source).Throttle,Is.Zero); Assert.That(source.FuelKg,Is.EqualTo(400));
        }
        /// <summary>The calm-air QA pilot refuses unqualified wind and malformed data rather than presenting a plausible request.</summary>
        [Test]
        public void RouteRefusesOtherWeatherAndInvalidReplica()
        {
            var control=new FlightRouteControls(); var source=new FlightStateRpc {Version=FlightWire.Version,Qw=1,WindX=8};
            Assert.Throws<InvalidOperationException>(()=>control.Sample(source));
            source.WindX=0; source.Qw=0; Assert.Throws<ArgumentOutOfRangeException>(()=>control.Sample(source));
        }
        /// <summary>Route owners require render/capture/metrics and cannot silently share another scripted input owner.</summary>
        [Test]
        public void RouteLaunchIsExplicitAndExclusive()
        {
            Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(new[]{"--fireline-flight-route-proof","x"}));
            Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(new[]{"--fireline-flight-route-role","owner"}));
            Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(new[]{"--fireline-flight-session","debug","--fireline-connect","127.0.0.1","--fireline-flight-route-proof","x"}));
            var valid=new[]{"--fireline-flight-session","debug","--fireline-connect","127.0.0.1","--fireline-flight-route-proof","x","--fireline-flight-route-role","observer"};
            Assert.That(LaunchOptions.Parse(valid).FlightRouteRole,Is.EqualTo("observer"));
            valid[7]="owner"; Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(valid));
        }
    }
}
