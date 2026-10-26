using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>Actual first-parcel accepted step and the M01 wind sample used for that step.</summary>
    [Serializable]
    public sealed class WaterObservation
    {
        public int step;
        public ulong windTick;
        public FlightVector windMps;
        public WaterParcel parcel;
    }

    /// <summary>Actual affected-cell state before and after one trusted coverage application; no fire-time evolution is hidden.</summary>
    [Serializable]
    public sealed class WaterCellObservation
    {
        public WaterHit hit;
        public FireCell before, after;
    }

    /// <summary>Reproducible isolated release inputs, every first-parcel step, all final impacts and actual existing fire response.</summary>
    [Serializable]
    public sealed class WaterDropCase
    {
        public string name;
        public int seed=415;
        public double dt, groundY, densityKgM3, dragAreaPerKg=DebugWaterProfile.DragAreaPerKg, appliedWaterKg;
        public WindState wind;
        public FlightState aircraft;
        public WaterRelease release;
        public WaterParcel[] impacts;
        public WaterObservation[] trajectory;
        public WaterCoverage coverage;
        public WaterCellObservation[] cells;
    }

    /// <summary>Dedicated-player numerical results, separate from real-time gameplay, client rendering and aircraft calibration.</summary>
    [Serializable]
    public sealed class WaterDropProof
    {
        public string profile=DebugWaterProfile.Identifier;
        public WaterDropCase[] cases;
        public double[] batchStepMs;
        public int timingWarmupCalls=100, timingCalls=1000, parcelsPerBatch=64, gc0Collections;
        public double checksumGuard;
    }

    /// <summary>Explicit server QA fixture around actual pure release/flight/coverage functions; ordinary sessions do not execute it.</summary>
    public static class WaterDropVerification
    {
        /// <summary>Fly 64 emitted masses to a flat surface, record real results and apply their raster once to a seeded burning grid.</summary>
        private static WaterDropCase Case(string name, double height, double eastWind=0, double dt=.01, bool tilted=false, double density=1.225)
        {
            var aircraft=FlightVerification.Fixture(4000); aircraft.PositionM=new FlightVector(80,height,40);
            if(tilted) { aircraft.Attitude=FlightRotation.FromRotationVector(new FlightVector(.05,.1,.08)); aircraft.AngularVelocityRadps=new FlightVector(.1,.2,.1); }
            var field=new WindField(415); field.Set(eastWind,0,0);
            var release=WaterDrop.Create(aircraft,2000); var parcels=(WaterParcel[])release.Parcels.Clone();
            var result=new WaterDropCase {name=name,dt=dt,densityKgM3=density,wind=field.State,aircraft=aircraft,release=release};
            var trace=new List<WaterObservation>(); bool allLanded=false;
            for(int step=0;step<20000 && !allLanded;step++)
            {
                allLanded=true; ulong tick=(ulong)Math.Floor((step*dt+1e-10)/.2);
                for(int i=0;i<parcels.Length;i++)
                {
                    if(parcels[i].HitGround) continue;
                    var p=parcels[i].PositionM; var sample=field.Sample(p.X,p.Z,p.Y,tick);
                    var wind=new FlightVector(sample.X,0,sample.Z);
                    parcels[i]=WaterDrop.Step(parcels[i],wind,dt,densityKgM3:density);
                    if(i==0) trace.Add(new WaterObservation {step=step+1,windTick=tick,windMps=wind,parcel=parcels[i]});
                    allLanded &= parcels[i].HitGround;
                }
            }
            if(!allLanded) throw new InvalidOperationException("Water proof impact timeout.");
            result.trajectory=trace.ToArray(); result.impacts=parcels;
            var coverage=WaterDrop.Rasterize(parcels,1,32,10); result.coverage=coverage;
            var world=new FireWorld(415,32,10); var id=new ChunkId(1,0,0); world.AddChunk(id,true);
            for(int i=0;i<32*32;i++) world.Ignite(id,i);
            var cells=new List<WaterCellObservation>();
            foreach(var hit in coverage.Hits) cells.Add(new WaterCellObservation {hit=hit,before=world.GetCell(hit.Chunk,hit.Cell)});
            world.ApplyWater(coverage.MassKg,coverage.Hits);
            foreach(var cell in cells) cell.after=world.GetCell(cell.hit.Chunk,cell.hit.Cell);
            result.cells=cells.ToArray(); result.appliedWaterKg=world.AppliedWaterKg;
            return result;
        }

        /// <summary>Compare two heights in calm/crosswind, a finer step, rotated/spinning release and an analytic vacuum reference.</summary>
        public static WaterDropProof Run()
        {
            var result=new WaterDropProof {cases=new[]{Case("low_calm",30),Case("high_calm",100),
                Case("low_crosswind",30,8),Case("high_crosswind",100,8),Case("fine_high_crosswind",100,8,.005),
                Case("tilted_spinning",60,8,tilted:true),Case("vacuum",30,density:0)}};
            var parcels=result.cases[0].release.Parcels; var watch=new Stopwatch();
            for(int n=0;n<result.timingWarmupCalls;n++) foreach(var p in parcels) WaterDrop.Step(p,new FlightVector(8,0,0),.01);
            int collections=GC.CollectionCount(0); result.batchStepMs=new double[result.timingCalls];
            for(int n=0;n<result.timingCalls;n++)
            {
                watch.Restart();
                foreach(var p in parcels) result.checksumGuard+=WaterDrop.Step(p,new FlightVector(8,0,0),.01).PositionM.Y;
                watch.Stop(); result.batchStepMs[n]=watch.Elapsed.TotalMilliseconds;
            }
            result.gc0Collections=GC.CollectionCount(0)-collections; return result;
        }
    }
}
