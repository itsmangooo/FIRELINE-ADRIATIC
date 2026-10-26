using System;
using System.Collections.Generic;

namespace Fireline.Simulation
{
    /// <summary>Estimated parcel/aperture settings for the generic debug aircraft; no CL-415 calibration is claimed.</summary>
    public static class DebugWaterProfile
    {
        public const string Identifier = "DEBUG_WATER_PARCELS_V1";
        public const double DragAreaPerKg = .04, ExitSpeedMps = 2, WidthM = 4, LengthM = 2;
        public const int Columns = 8, Rows = 8;
    }

    /// <summary>One representative mass, its actual trajectory clock and an irreversible planar ground impact.</summary>
    [Serializable]
    public struct WaterParcel
    {
        public FlightVector PositionM, VelocityMps;
        public double MassKg, FlightSeconds;
        public bool HitGround;
    }

    /// <summary>A validated candidate tank debit plus all emitted masses; the caller installs it only after successful creation.</summary>
    [Serializable]
    public sealed class WaterRelease
    {
        public FlightState AircraftAfter;
        public WaterParcel[] Parcels;
        public double ReleasedKg;
    }

    /// <summary>Conserved kilograms and deterministic cell weights for the existing trusted FireWorld.ApplyWater boundary.</summary>
    [Serializable]
    public sealed class WaterCoverage
    {
        public double MassKg;
        public WaterHit[] Hits;
    }

    /// <summary>Pure release/trajectory/coverage prerequisite; no live pilot command, scoop, VFX or client physics is installed.</summary>
    public static class WaterDrop
    {
        /// <summary>
        /// Remove one instantaneous debug slug, preserving dry-body geometry/point velocity as CoG shifts.
        /// Emit a bounded rectangular mass quadrature with inherited linear/angular velocity and body-down exit speed.
        /// Validate every parcel before returning; invalid release geometry cannot debit the caller's value-type state.
        /// </summary>
        public static WaterRelease Create(FlightState aircraft, double massKg, double groundY = 0,
            int columns = DebugWaterProfile.Columns, int rows = DebugWaterProfile.Rows)
        {
            aircraft.Validate(); SimMath.Range(massKg, 1e-6, aircraft.WaterKg, nameof(massKg));
            SimMath.Range(groundY, -1000, 10000, nameof(groundY));
            if(columns<2 || rows<2 || columns>32 || rows>32 || columns*rows<32 || columns*rows>512)
                throw new ArgumentOutOfRangeException(nameof(columns));
            double beforeCg=FlightDynamics.Mass(aircraft.FuelKg,aircraft.WaterKg).CgZ;
            var next=aircraft; next.WaterKg-=massKg;
            double afterCg=FlightDynamics.Mass(next.FuelKg,next.WaterKg).CgZ;
            var shift=new FlightVector(0,0,afterCg-beforeCg);
            next.PositionM+=aircraft.Attitude.Rotate(shift);
            next.VelocityMps+=aircraft.Attitude.Rotate(FlightVector.Cross(aircraft.AngularVelocityRadps,shift));
            next.Validate();
            var parcels=new WaterParcel[columns*rows]; double emitted=0;
            for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
            {
                int index=row*columns+col;
                var lever=new FlightVector(((col+.5)/columns-.5)*DebugWaterProfile.WidthM,-1,
                    1+((row+.5)/rows-.5)*DebugWaterProfile.LengthM-beforeCg);
                double mass=index==parcels.Length-1 ? massKg-emitted : massKg/parcels.Length;
                var parcel=new WaterParcel {MassKg=mass,PositionM=aircraft.PositionM+aircraft.Attitude.Rotate(lever),
                    VelocityMps=aircraft.VelocityMps+aircraft.Attitude.Rotate(FlightVector.Cross(aircraft.AngularVelocityRadps,lever)
                        +new FlightVector(0,-DebugWaterProfile.ExitSpeedMps,0))};
                Validate(parcel,groundY); parcels[index]=parcel; emitted+=mass;
            }
            return new WaterRelease {AircraftAfter=next,ReleasedKg=massKg,Parcels=parcels};
        }

        /// <summary>
        /// Advance a bounded semi-implicit quadratic-drag/gravity step using a server-sampled wind value.
        /// Intersect the actual motion segment with the horizontal plane and stop at its first impact fraction.
        /// Impacted parcels are idempotent. No evaporation, breakup, splash, terrain slopes or fluid recoil is modeled.
        /// </summary>
        public static WaterParcel Step(WaterParcel current, FlightVector windMps, double seconds, double groundY = 0,
            double densityKgM3 = DebugFlightProfile.ReferenceDensity, double dragAreaPerKg = DebugWaterProfile.DragAreaPerKg)
        {
            Validate(current,groundY); windMps.Validate(100,nameof(windMps));
            SimMath.Range(seconds,1e-6,.02,nameof(seconds)); SimMath.Range(densityKgM3,0,1.5,nameof(densityKgM3));
            SimMath.Range(dragAreaPerKg,0,1,nameof(dragAreaPerKg));
            if(current.HitGround) return current;
            var relative=windMps-current.VelocityMps;
            var acceleration=new FlightVector(0,-DebugFlightProfile.GravityMps2,0)
                +relative*(.5*densityKgM3*dragAreaPerKg*relative.Length);
            var next=current; next.VelocityMps+=acceleration*seconds;
            next.PositionM+=next.VelocityMps*seconds; next.FlightSeconds+=seconds;
            if(next.PositionM.Y<=groundY)
            {
                double fraction=(current.PositionM.Y-groundY)/(current.PositionM.Y-next.PositionM.Y);
                next.PositionM=current.PositionM+(next.PositionM-current.PositionM)*fraction; next.PositionM.Y=groundY;
                next.VelocityMps=current.VelocityMps+(next.VelocityMps-current.VelocityMps)*fraction;
                next.FlightSeconds=current.FlightSeconds+seconds*fraction; next.HitGround=true;
            }
            Validate(next,groundY); return next;
        }

        /// <summary>
        /// Bilinearly distribute each landed mass around cell centers, including negative-coordinate chunk seams.
        /// Preserve total mass and sorted chunk/cell ordering. Unknown/unloaded cells are not silently discarded:
        /// the existing FireWorld.ApplyWater validates the whole returned coverage before any world mutation.
        /// </summary>
        public static WaterCoverage Rasterize(WaterParcel[] landed, int sector, int chunkSize, double cellSizeM, double groundY = 0)
        {
            if(landed==null || landed.Length<1 || landed.Length>512) throw new ArgumentException("Invalid impact count.");
            if(sector<0 || sector>ushort.MaxValue || chunkSize<4 || chunkSize>32) throw new ArgumentOutOfRangeException(nameof(sector));
            SimMath.Range(cellSizeM,5,100,nameof(cellSizeM));
            var cells=new SortedDictionary<ChunkId,SortedDictionary<int,double>>(); double total=0;
            foreach(var parcel in landed)
            {
                Validate(parcel,groundY); if(!parcel.HitGround) throw new ArgumentException("Coverage requires landed parcels.");
                double gx=parcel.PositionM.X/cellSizeM-.5, gz=parcel.PositionM.Z/cellSizeM-.5;
                int x=(int)Math.Floor(gx), z=(int)Math.Floor(gz); double fx=gx-x, fz=gz-z;
                for(int dz=0;dz<2;dz++) for(int dx=0;dx<2;dx++)
                {
                    double mass=parcel.MassKg*(dx==0 ? 1-fx : fx)*(dz==0 ? 1-fz : fz);
                    if(mass==0) continue;
                    int cx=(int)Math.Floor((double)(x+dx)/chunkSize), cz=(int)Math.Floor((double)(z+dz)/chunkSize);
                    var id=new ChunkId(sector,cx,cz); int index=(z+dz-cz*chunkSize)*chunkSize+x+dx-cx*chunkSize;
                    if(!cells.TryGetValue(id,out var chunk)) {chunk=new SortedDictionary<int,double>(); cells.Add(id,chunk);}
                    chunk.TryGetValue(index,out double previous); chunk[index]=previous+mass;
                }
                total+=parcel.MassKg;
            }
            SimMath.Range(total,1e-6,DebugFlightProfile.WaterCapacityKg,nameof(total));
            var hits=new List<WaterHit>();
            foreach(var chunk in cells) foreach(var cell in chunk.Value) hits.Add(new WaterHit(chunk.Key,cell.Key,cell.Value/total));
            return new WaterCoverage {MassKg=total,Hits=hits.ToArray()};
        }

        /// <summary>Reject nonfinite/unphysical point state and inconsistent impact flags before a candidate can be accepted.</summary>
        private static void Validate(WaterParcel parcel, double groundY)
        {
            SimMath.Range(groundY,-1000,10000,nameof(groundY));
            parcel.PositionM.Validate(1e7,nameof(parcel.PositionM)); parcel.VelocityMps.Validate(500,nameof(parcel.VelocityMps));
            SimMath.Range(parcel.MassKg,1e-9,DebugFlightProfile.WaterCapacityKg,nameof(parcel.MassKg));
            SimMath.Range(parcel.FlightSeconds,0,120,nameof(parcel.FlightSeconds));
            if(parcel.HitGround ? parcel.PositionM.Y!=groundY : parcel.PositionM.Y<=groundY)
                throw new ArgumentException("Parcel/ground state is inconsistent.");
        }
    }
}
