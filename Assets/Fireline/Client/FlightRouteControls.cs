using System;
using Fireline.Shared;

namespace Fireline.Client
{
    /// <summary>Opt-in calm-air QA requests computed only from a received snapshot; no client physical integration or gameplay autopilot.</summary>
    public sealed class FlightRouteControls
    {
        /// <summary>Latched QA roll=0, climb=1, approach=2, throttle-cut/brake=3 phases.</summary>
        public int Phase { get; private set; }

        /// <summary>Validate the authoritative snapshot and derive bounded full-flap requests suitable for the explicit delayed-replica fixture.</summary>
        public FlightRequestRpc Sample(FlightStateRpc source)
        {
            var data = FlightTelemetry.Read(source);
            if (Math.Abs(source.WindX) > 1e-9 || Math.Abs(source.WindZ) > 1e-9)
                throw new InvalidOperationException("Route QA requires the explicit calm-air fixture.");
            if (Phase == 0 && source.Y > 2.2) Phase = 1;
            if (Phase == 1 && source.Z > 1200) Phase = 2;
            if (Phase == 2 && source.Y < 2.1) Phase = 3;
            double speed = data.AirspeedMps, targetVy = Phase == 2 ? -1 : Math.Max(-1, Math.Min(1.5, (8-source.Y)*.4));
            double cl = data.MassKg*9.80665/(.5*1.225*Math.Max(speed*speed,100)*100);
            double alpha = (cl-.6)/4.5, targetPitch = alpha+Math.Atan2(targetVy,Math.Max(speed,1));
            double pitch = Phase == 0 ? (speed>46 ? 1 : 0)
                : Math.Max(-1,Math.Min(1,7.5*alpha+16*(targetPitch-data.PitchDegrees*Math.PI/180)+3*source.Wx));
            return new FlightRequestRpc { Gear=1,Flaps=1,Throttle=Phase==3 ? 0 : 1,
                Pitch=Phase==3 ? 0 : (float)pitch,Brake=Phase==3 ? 1 : 0 };
        }
    }
}
