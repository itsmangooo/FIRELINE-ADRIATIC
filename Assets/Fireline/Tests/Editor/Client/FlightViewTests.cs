using System;
using Fireline.Client;
using Fireline.Simulation;
using Fireline.Shared;
using NUnit.Framework;
using UnityEngine;

namespace Fireline.Tests
{
    /// <summary>Device-level input and cosmetic interpolation boundaries; no server or scene physics is bypassed by the renderer.</summary>
    public sealed class FlightViewTests
    {
        /// <summary>Construct a finite unit-attitude display state in metres for independent interpolation checks.</summary>
        private static FlightState Pose(double x)=>new FlightState { PositionM=new FlightVector(x,2,0),Attitude=FlightRotation.Identity };

        /// <summary>A 100 ms receipt delay interpolates halfway between two real samples, then holds the newest without extrapolation.</summary>
        [Test]
        public void PoseInterpolationUsesDelayAndClampsEndpoints()
        {
            var track=new FlightPoseTrack(); track.Push(Pose(0),1,1,1); track.Push(Pose(10),21,1,1.1);
            Assert.That(track.Sample(1.15).PositionM.X,Is.EqualTo(5).Within(1e-12));
            Assert.That(track.Sample(1).PositionM.X,Is.Zero); Assert.That(track.Sample(5).PositionM.X,Is.EqualTo(10));
            Assert.That(track.Sample(5).Attitude.NormSquared,Is.EqualTo(1).Within(1e-12));
        }

        /// <summary>Equivalent opposite-sign quaternions never collapse to a zero quaternion or rotate the long way.</summary>
        [Test]
        public void QuaternionSignAndResetBoundariesAreSafe()
        {
            var track=new FlightPoseTrack(); var next=Pose(10); next.Attitude=new FlightRotation(0,0,0,-1);
            track.Push(Pose(0),1,1,1); track.Push(next,21,1,1.1);
            Assert.That(Math.Abs(track.Sample(1.15).Attitude.W),Is.EqualTo(1).Within(1e-12));
            track.Push(Pose(20),41,2,1.2); Assert.That(track.Sample(1.21).PositionM.X,Is.EqualTo(20));
            track.Push(Pose(200),61,2,1.3); Assert.That(track.Sample(1.31).PositionM.X,Is.EqualTo(200));
        }

        /// <summary>Stale/duplicate snapshots and nonfinite receipt/sample time cannot silently become display state.</summary>
        [Test]
        public void InvalidPresentationHistoryFailsWithoutReplacingLastPose()
        {
            var track=new FlightPoseTrack(); Assert.Throws<InvalidOperationException>(()=>track.Sample(1));
            track.Push(Pose(1),2,1,1);
            Assert.Throws<ArgumentException>(()=>track.Push(Pose(20),2,1,2));
            Assert.Throws<ArgumentException>(()=>track.Push(Pose(20),3,1,double.NaN));
            Assert.Throws<ArgumentException>(()=>track.Push(Pose(20),3,0,2));
            Assert.That(track.Sample(2).PositionM.X,Is.EqualTo(1));
        }

        /// <summary>View startup is explicit and cannot share the previous scripted network input owner.</summary>
        [Test]
        public void ViewLaunchRequiresCompatibleExplicitOptions()
        {
            Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(new[]{"--fireline-flight-view","debug"}));
            Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(new[]{"--fireline-flight-view-proof","x.json"}));
            Assert.Throws<ArgumentException>(()=>LaunchOptions.Parse(new[]{"--fireline-flight-session","debug","--fireline-flight-view","debug","--fireline-flight-network-proof","x.json"}));
            Assert.That(LaunchOptions.Parse(new[]{"--fireline-flight-session","debug","--fireline-flight-view","debug"}).FlightView,Is.True);
        }
    }
}
