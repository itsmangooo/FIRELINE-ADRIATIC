using System;
using System.IO;
using Fireline.Shared;
using Fireline.Simulation;
using UnityEngine;

namespace Fireline.Server
{
    /// <summary>Actual substep route gates; durations count accepted physical steps rather than wall callbacks.</summary>
    [Serializable]
    public sealed class LiveRouteProgress
    {
        public bool passed, touchdown;
        public double maximumHeightM, longestAirborneSeconds, settledSeconds;
        public int recordedSteps;
    }

    /// <summary>Explicit live-QA recorder. Never supplies a pilot command or installs an aircraft state.</summary>
    public sealed class ServerRouteProbe : IDisposable
    {
        [Serializable]
        private sealed class StepRow
        {
            public ulong tick, fireTick;
            public uint sequence, epoch;
            public int owner, supportedWheels;
            public double wallSeconds, brake, compressionM;
            public FlightState state;
            public FlightInput input;
        }
        [Serializable]
        private sealed class RequestRow
        {
            public int identity;
            public ulong appliedAtTick;
            public byte result;
            public FlightRequestRpc request;
        }
        private readonly FlightAuthority authority;
        private readonly StreamWriter steps, requests;
        private ulong fireTick;
        private int airborneSteps, longestSteps, stoppedSteps;
        private bool airborneLogged;
        /// <summary>Detached-by-convention result data written with the server outcome, never received from a client.</summary>
        public LiveRouteProgress Progress { get; } = new LiveRouteProgress();

        /// <summary>Create paired raw substep/request streams only for the explicit role output path.</summary>
        public ServerRouteProbe(FlightAuthority host, string path)
        {
            authority=host;
            steps=new StreamWriter(Path.ChangeExtension(path,".route.jsonl"));
            requests=new StreamWriter(Path.ChangeExtension(path,".requests.jsonl"));
        }
        /// <summary>Retain the actual fire tick used by this host callback for all its accepted aircraft substeps.</summary>
        public void BeginUpdate(ulong tick) { fireTick=tick; }
        /// <summary>Record accepted and rejected normal input RPCs with actual transport identity and the pre-advance aircraft tick.</summary>
        public void Request(int identity, FlightRequestRpc request, FlightResult result)
        {
            if(request.Kind==3) requests.WriteLine(JsonUtility.ToJson(new RequestRow {identity=identity,appliedAtTick=authority.Tick,result=(byte)result,request=request}));
        }
        /// <summary>Inspect every actual accepted post-step state, reject footprint departure, and require sustained flight/contact/settling.</summary>
        public void Observe(FlightState state, FlightInput input, double brake, ulong tick)
        {
            var contact=RunwayContact.Evaluate(state,input,brake,RunwayPlane.Debug);
            if(!contact.LeftMain.OnRunway || !contact.RightMain.OnRunway || !contact.Nose.OnRunway)
                throw new InvalidOperationException("ROUTE_QA: tire left finite runway footprint.");
            airborneSteps=contact.SupportedWheels==0 && state.PositionM.Y>5 ? airborneSteps+1 : 0;
            longestSteps=Math.Max(longestSteps,airborneSteps);
            if(longestSteps>=1000 && contact.SupportedWheels>=2) Progress.touchdown=true;
            stoppedSteps=Progress.touchdown && contact.SupportedWheels==3 && state.VelocityMps.Length<.2
                && Math.Abs(state.VelocityMps.Y)<.1 && input.Throttle==0 && brake>=.99 ? stoppedSteps+1 : 0;
            Progress.maximumHeightM=Math.Max(Progress.maximumHeightM,state.PositionM.Y);
            Progress.longestAirborneSeconds=longestSteps*FlightAuthority.StepSeconds;
            Progress.settledSeconds=stoppedSteps*FlightAuthority.StepSeconds;
            Progress.passed |= stoppedSteps>=1000;
            Progress.recordedSteps++;
            steps.WriteLine(JsonUtility.ToJson(new StepRow { tick=tick,fireTick=fireTick,sequence=authority.Sequence,epoch=authority.Epoch,
                owner=authority.Owner,supportedWheels=contact.SupportedWheels,wallSeconds=Time.realtimeSinceStartupAsDouble,
                state=state,input=input,brake=brake,compressionM=contact.MaximumCompressionM }));
            if(!airborneLogged && state.PositionM.Y>5 && contact.SupportedWheels==0)
            { airborneLogged=true; Debug.Log("FIRELINE_ROUTE_AIRBORNE tick="+tick); }
        }
        /// <summary>Flush only owned proof files on controlled completion or process destruction.</summary>
        public void Dispose() { steps.Dispose(); requests.Dispose(); }
    }
}
