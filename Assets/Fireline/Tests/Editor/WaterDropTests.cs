using System;
using System.Linq;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    /// <summary>Conservation, geometry, drag/impact and trusted fire-coverage boundaries for the isolated water prerequisite.</summary>
    public sealed class WaterDropTests
    {
        /// <summary>Release a loaded rotated/spinning body and preserve its origin, point velocity, fuel and emitted mass.</summary>
        [Test]
        public void ReleaseConservesTankAndRebasesDryBody()
        {
            var source=FlightDynamicsTests.Fixture(4000);
            source.Attitude=FlightRotation.FromRotationVector(new FlightVector(.1,.2,.3));
            source.AngularVelocityRadps=new FlightVector(.1,.2,.3); var copy=source;
            var release=WaterDrop.Create(source,1234.5);
            Assert.That(source,Is.EqualTo(copy)); Assert.That(release.AircraftAfter.FuelKg,Is.EqualTo(source.FuelKg));
            Assert.That(release.AircraftAfter.WaterKg+release.Parcels.Sum(p=>p.MassKg),Is.EqualTo(source.WaterKg).Within(1e-9));
            double oldCg=FlightDynamics.Mass(source.FuelKg,source.WaterKg).CgZ;
            double newCg=FlightDynamics.Mass(source.FuelKg,release.AircraftAfter.WaterKg).CgZ;
            var a=new FlightVector(3,-2,6-oldCg); var b=new FlightVector(3,-2,6-newCg);
            Assert.That((source.PositionM+source.Attitude.Rotate(a)-release.AircraftAfter.PositionM-release.AircraftAfter.Attitude.Rotate(b)).Length,Is.LessThan(1e-10));
            Assert.That((source.VelocityMps+source.Attitude.Rotate(FlightVector.Cross(source.AngularVelocityRadps,a))
                -release.AircraftAfter.VelocityMps-release.AircraftAfter.Attitude.Rotate(FlightVector.Cross(source.AngularVelocityRadps,b))).Length,Is.LessThan(1e-10));
            var lever=new FlightVector(-1.75,-1,.125-oldCg);
            Assert.That((release.Parcels[0].PositionM-source.PositionM-source.Attitude.Rotate(lever)).Length,Is.LessThan(1e-10));
            Assert.That((release.Parcels[0].VelocityMps-source.VelocityMps-source.Attitude.Rotate(FlightVector.Cross(source.AngularVelocityRadps,lever)+new FlightVector(0,-2,0))).Length,Is.LessThan(1e-10));
        }

        /// <summary>Out-of-tank, malformed-count, nonfinite and intersecting-aperture requests leave the caller unchanged.</summary>
        [Test]
        public void InvalidReleaseCannotDebitTank()
        {
            var source=FlightDynamicsTests.Fixture(100); var copy=source;
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaterDrop.Create(source,101));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaterDrop.Create(source,double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaterDrop.Create(source,1,columns:2,rows:2));
            source.PositionM.Y=1; Assert.Throws<ArgumentException>(()=>WaterDrop.Create(source,1));
            source.PositionM=copy.PositionM; Assert.That(source,Is.EqualTo(copy));
        }

        /// <summary>Vacuum acceleration follows semi-implicit gravity; impact uses the crossing segment rather than a vertical ray.</summary>
        [Test]
        public void VacuumStepAndSegmentImpactAreAnalytic()
        {
            var p=new WaterParcel {MassKg=1,PositionM=new FlightVector(0,.05,0),VelocityMps=new FlightVector(3,-10,4)};
            var landed=WaterDrop.Step(p,new FlightVector(),.01,densityKgM3:0);
            double fraction=.05/((10+9.80665*.01)*.01);
            Assert.That(landed.HitGround,Is.True); Assert.That(landed.PositionM.X,Is.EqualTo(.03*fraction).Within(1e-12));
            Assert.That(landed.PositionM.Z,Is.EqualTo(.04*fraction).Within(1e-12)); Assert.That(landed.PositionM.Y,Is.Zero);
            Assert.That(landed.FlightSeconds,Is.EqualTo(.01*fraction).Within(1e-12));
            Assert.That(WaterDrop.Step(landed,new FlightVector(8,0,0),.01),Is.EqualTo(landed));
        }

        /// <summary>Real wind accelerates parcels sideways while they keep mass and eventually intersect the plane.</summary>
        [Test]
        public void CrosswindDriftsBeyondEquivalentCalmImpact()
        {
            var source=FlightDynamicsTests.Fixture(2000); source.PositionM.Y=30;
            var calm=WaterDrop.Create(source,2000).Parcels[0]; var windy=calm;
            for(int i=0;i<2000 && (!calm.HitGround || !windy.HitGround);i++)
            { calm=WaterDrop.Step(calm,new FlightVector(),.01); windy=WaterDrop.Step(windy,new FlightVector(8,0,0),.01); }
            Assert.That(calm.HitGround && windy.HitGround,Is.True); Assert.That(windy.PositionM.X-calm.PositionM.X,Is.GreaterThan(5));
            Assert.That(windy.MassKg,Is.EqualTo(calm.MassKg));
        }

        /// <summary>Invalid trajectory parameters or inconsistent point states throw without mutating a supplied parcel.</summary>
        [Test]
        public void InvalidStepsAreRejected()
        {
            var p=new WaterParcel {MassKg=1,PositionM=new FlightVector(0,10,0)}; var copy=p;
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaterDrop.Step(p,new FlightVector(),.1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaterDrop.Step(p,new FlightVector(double.NaN,0,0),.01));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WaterDrop.Step(p,new FlightVector(),.01,dragAreaPerKg:-1));
            Assert.That(p,Is.EqualTo(copy)); p.PositionM.Y=0;
            Assert.Throws<ArgumentException>(()=>WaterDrop.Step(p,new FlightVector(),.01));
        }

        /// <summary>A hit at four cell-center boundaries distributes exactly one mass across four weights.</summary>
        [Test]
        public void CoverageIsBilinearAndMassConserving()
        {
            var p=new WaterParcel {MassKg=100,PositionM=new FlightVector(10,0,10),HitGround=true};
            var coverage=WaterDrop.Rasterize(new[]{p},1,16,10);
            Assert.That(coverage.MassKg,Is.EqualTo(100)); Assert.That(coverage.Hits.Length,Is.EqualTo(4));
            Assert.That(coverage.Hits.Sum(h=>h.Weight),Is.EqualTo(1).Within(1e-12));
            Assert.That(coverage.Hits.All(h=>h.Chunk.Equals(new ChunkId(1,0,0)) && h.Weight==.25),Is.True);
            Assert.That(coverage.Hits.Select(h=>h.Cell),Is.EqualTo(new[]{0,1,16,17}));
        }

        /// <summary>Crossing world zero reaches four neighboring signed chunks without dropping or duplicating mass.</summary>
        [Test]
        public void CoverageHandlesNegativeChunkSeams()
        {
            var p=new WaterParcel {MassKg=100,HitGround=true}; var coverage=WaterDrop.Rasterize(new[]{p},1,16,10);
            Assert.That(coverage.Hits.Select(h=>h.Chunk.ToString()),Is.EqualTo(new[]{"1:-1:-1","1:-1:0","1:0:-1","1:0:0"}));
            Assert.That(coverage.Hits.All(h=>h.Cell>=0 && h.Cell<256),Is.True);
            Assert.That(coverage.Hits.Sum(h=>h.Weight),Is.EqualTo(1));
        }

        /// <summary>In-flight masses cannot become coverage; unloaded destinations retain the existing atomic fire rejection.</summary>
        [Test]
        public void InFlightOrUnknownCoverageCannotMutateFire()
        {
            var p=new WaterParcel {MassKg=100,PositionM=new FlightVector(0,10,0)};
            Assert.Throws<ArgumentException>(()=>WaterDrop.Rasterize(new[]{p},1,16,10));
            p.PositionM.Y=0; p.HitGround=true; var coverage=WaterDrop.Rasterize(new[]{p},1,16,10);
            var world=new FireWorld(415); world.AddChunk(new ChunkId(1,0,0),true);
            Assert.Throws<ArgumentException>(()=>world.ApplyWater(coverage.MassKg,coverage.Hits));
            Assert.That(world.AppliedWaterKg,Is.Zero); Assert.That(world.GetCell(new ChunkId(1,0,0),0).WaterKgPerM2,Is.Zero);
        }

        /// <summary>Actual raster weights enter existing cooling/wetting once, preserving total kilograms and unaffected cells.</summary>
        [Test]
        public void LandedCoverageFeedsExistingGradedSuppression()
        {
            var id=new ChunkId(1,0,0); var world=new FireWorld(415); world.AddChunk(id,true);
            world.Ignite(id,0); world.Ignite(id,1);
            var p=new WaterParcel {MassKg=100,PositionM=new FlightVector(6,0,5),HitGround=true};
            var coverage=WaterDrop.Rasterize(new[]{p},1,16,10); world.ApplyWater(coverage.MassKg,coverage.Hits);
            Assert.That(world.GetCell(id,0).WaterKgPerM2,Is.EqualTo(.9).Within(1e-12));
            Assert.That(world.GetCell(id,1).WaterKgPerM2,Is.EqualTo(.1).Within(1e-12));
            Assert.That(world.GetCell(id,0).Intensity,Is.LessThan(world.GetCell(id,1).Intensity));
            Assert.That(world.GetCell(id,2).WaterKgPerM2,Is.Zero); Assert.That(world.AppliedWaterKg,Is.EqualTo(100));
        }
    }
}
