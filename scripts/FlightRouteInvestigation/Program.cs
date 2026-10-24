using System;
using System.Globalization;
using Fireline.Simulation;

/// <summary>Offline .NET control experiment over the actual pure kernel; neither a Unity-player result nor a shipped autopilot.</summary>
internal static class Program
{
    /// <summary>Run one documented calm-air control attempt and return 2 unless it actually takes off and stops inside the debug runway.</summary>
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        bool damped=args.Length==1 && args[0]=="--damped";
        if(args.Length>1 || args.Length==1 && !damped) throw new ArgumentException("Only --damped is supported.");
        var state=new FlightState { PositionM=new FlightVector(0,2.05,0), Attitude=FlightRotation.Identity, FuelKg=400 };
        double dt=.005, throttle=1, maximum=0;
        bool airborne=false, landing=false;
        Console.WriteLine("seconds,north_m,height_m,vertical_mps,speed_mps,pitch_deg,pitch_request,throttle,landing");
        for(int i=0;i<24000;i++)
        {
            double speed=state.VelocityMps.Length, flaps=damped?1:.5;
            double pitch=Math.Asin(Math.Clamp(state.Attitude.Rotate(new FlightVector(0,0,1)).Y,-1,1));
            double command=speed>40?(damped?.15:.8):0;
            if(airborne)
            {
                landing=state.PositionM.Z>(damped?1400:950);
                double targetVy=landing ? -Math.Clamp((state.PositionM.Y-1.85)*.16,.2,2)
                    : damped ? Math.Clamp((10-state.PositionM.Y)*.3,-1,1.5) : Math.Clamp((25-state.PositionM.Y)*.4,-2,3);
                double cl=FlightDynamics.Mass(state.FuelKg,0).Kg*9.80665/(.5*1.225*Math.Max(speed*speed,100)*100);
                double alpha=(cl-.2-.4*flaps)/4.5;
                command=Math.Clamp(7.5*alpha-.05+.15*(targetVy-state.VelocityMps.Y)+(damped?5*state.AngularVelocityRadps.X:0),-1,1);
            }
            if(airborne && landing && state.PositionM.Y<2.1) { throttle=0; command=0; }
            try { state=FuelFlight.Step(state,new FlightInput { Throttle=throttle,Pitch=command,Flaps=flaps,Gear=1 },
                new FlightVector(),1.225,dt,throttle==0?1:0,RunwayPlane.Debug).State; }
            catch(Exception error) { Console.Error.WriteLine($"ROUTE_NOT_QUALIFIED envelope at {i*dt:F3}s: {error.Message}"); return 2; }
            maximum=Math.Max(maximum,state.PositionM.Y);
            if(state.PositionM.Y>(damped?2.1:5)) airborne=true;
            if(i%400==0)
            {
                double shownPitch=Math.Asin(Math.Clamp(state.Attitude.Rotate(new FlightVector(0,0,1)).Y,-1,1));
                Console.WriteLine($"{(i+1)*dt:F3},{state.PositionM.Z:R},{state.PositionM.Y:R},{state.VelocityMps.Y:R},{state.VelocityMps.Length:R},{shownPitch*180/Math.PI:R},{command:R},{throttle:R},{landing}");
            }
            if(airborne && throttle==0 && speed<.2 && RunwayPlane.Debug.Contains(state.PositionM) && maximum>5)
            { Console.Error.WriteLine("ROUTE_QUALIFIED_OFFLINE_ONLY"); return 0; }
        }
        Console.Error.WriteLine($"ROUTE_NOT_QUALIFIED duration=120s maximum_height_m={maximum:R} final_north_m={state.PositionM.Z:R}");
        return 2;
    }
}
