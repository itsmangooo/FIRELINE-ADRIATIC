using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Fireline.Shared;
using UnityEngine;

namespace Fireline.Client
{
    /// <summary>Explicit live QA driver/recorder; normal sessions install neither an automatic pilot nor proof files.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class FlightRealtimeProof : MonoBehaviour
    {
        [Serializable]
        private sealed class Result
        {
            public bool passed, sawOwner, sawAirborne, sawVacant, airborneCapture, landedCapture, released;
            public string role, reason;
            public int identity, snapshots, acceptedInputs, frames, queuedInputs;
            public double seconds, maximumHeightM;
        }
        [Serializable]
        private sealed class Instruments { public FlightStateRpc source; public string text; }
        [Serializable]
        private sealed class Command { public FlightStateRpc source; public FlightRequestRpc request; public int phase; }
        private readonly Result result=new Result();
        private ClientFlightSession session;
        private FlightTestView view;
        private StreamWriter trace, instruments, commands;
        private double started, nextClaim, vacantAt;
        private ulong stoppedAt;
        private uint releaseEpoch;
        private bool releasing;
        private bool finished, capturingAir, capturingLand;

        /// <summary>Install only after role/connection/render requirements have passed the explicit launch parser.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(FoundationBootstrap.Options.FlightRouteProofPath!=null && FoundationBootstrap.Options.FlightRouteRole!=null)
                new GameObject("M03 Real-time Route QA").AddComponent<FlightRealtimeProof>();
        }
        /// <summary>Bind the normal replica/view and open owner traces; observers only retain the actual replica stream.</summary>
        private void Start()
        {
            session=FindAnyObjectByType<ClientFlightSession>(); view=FindAnyObjectByType<FlightTestView>();
            result.role=FoundationBootstrap.Options.FlightRouteRole; started=Time.realtimeSinceStartupAsDouble;
            if(result.role!="owner") return;
            if(view==null) throw new InvalidOperationException("Rendered route owner has no engineering view.");
            string path=FoundationBootstrap.Options.FlightRouteProofPath;
            trace=new StreamWriter(Path.ChangeExtension(path,".csv"));
            instruments=new StreamWriter(Path.ChangeExtension(path,".instruments.jsonl"));
            commands=new StreamWriter(Path.ChangeExtension(path,".commands.jsonl"));
            trace.WriteLine("wall_s,received_at_s,aircraft_tick,epoch,owner,received_z_m,displayed_x_m,displayed_y_m,displayed_z_m,qx,qy,qz,qw,anchor_x_m,anchor_y_m,anchor_z_m,visual_y_m,visual_z_m,throttle,pitch,roll,yaw,brake,focused,camera");
            view.RouteInputQueued+=Queued;
        }
        /// <summary>Archive the exact metadata-bearing control RPC actually enqueued, paired with the snapshot used for its calculation.</summary>
        private void Queued(FlightStateRpc source, FlightRequestRpc request, int phase)
        {
            result.queuedInputs++; commands.WriteLine(JsonUtility.ToJson(new Command {source=source,request=request,phase=phase}));
        }
        /// <summary>Request the real seat, observe actual received flight/stop state, capture real frames and disconnect on completion.</summary>
        private void Update()
        {
            if(finished) return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(now-started>140) { Finish(false,"route-client-timeout"); return; }
            if(!session.Ready || !session.HasState) return;
            var s=session.Latest; result.identity=session.Identity;
            result.sawOwner |= s.Owner!=0; result.sawAirborne |= s.Y>5; result.maximumHeightM=Math.Max(result.maximumHeightM,s.Y);
            if(result.role=="owner")
            {
                if(releasing)
                {
                    if(s.Owner==0 && s.Epoch>releaseEpoch) { result.released=true; Finish(true,"rendered-network-route-and-release"); }
                    return;
                }
                if(s.Owner!=session.Identity)
                {
                    if(result.sawOwner) { Finish(false,"route-owner-lost-seat"); return; }
                    if(now>=nextClaim) { session.RequestSeat(); nextClaim=now+1; }
                    return;
                }
                if(!capturingAir && s.Y>5 && view.Displayed.PositionM.Y>5)
                { capturingAir=true; StartCoroutine(Capture("airborne")); }
                double speed=Math.Sqrt(s.Vx*s.Vx+s.Vy*s.Vy+s.Vz*s.Vz);
                if(view.RoutePhase==3 && speed<.2 && Math.Abs(s.Vy)<.1)
                {
                    if(stoppedAt==0) stoppedAt=s.Tick;
                    if(s.Tick-stoppedAt>=1100 && !capturingLand) { capturingLand=true; StartCoroutine(Capture("landed")); }
                }
                else stoppedAt=0;
                if(result.airborneCapture && result.landedCapture && result.frames>1000 && result.acceptedInputs>100)
                { releaseEpoch=s.Epoch; releasing=true; session.ReleaseSeat(); }
            }
            else if(result.sawOwner && result.sawAirborne && s.Owner==0)
            {
                result.sawVacant=true;
                if(vacantAt==0) vacantAt=now;
                // Leave time for actual vacancy ACK transport before disconnecting; server waits for that ACK and disconnect.
                if(now-vacantAt>1) Finish(true,"observer-airborne-and-vacant");
            }
            result.acceptedInputs=session.AcceptedInputs;
        }
        /// <summary>Retain real displayed doubles, float transforms and exact UI text after the existing presentation adapter has run.</summary>
        private void LateUpdate()
        {
            if(finished || result.role!="owner" || !session.HasState) return;
            var p=view.Displayed.PositionM; var q=view.Displayed.Attitude; var a=view.AnchorPosition; var v=view.VisualOffset;
            var c=view.LocalControls; var s=session.Latest;
            /// <summary>Keep every CSV scalar round-trip safe with decimal dots regardless of the operating-system locale.</summary>
            string N(double number)=>number.ToString("R",CultureInfo.InvariantCulture);
            trace.WriteLine(string.Join(",",N(view.SampledAt),N(session.ReceivedAt),s.Tick,s.Epoch,s.Owner,N(s.Z),N(p.X),N(p.Y),N(p.Z),N(q.X),N(q.Y),N(q.Z),N(q.W),
                N(a.x),N(a.y),N(a.z),N(v.y),N(v.z),N(c.Throttle),N(c.Pitch),N(c.Roll),N(c.Yaw),N(c.Brake),view.InputFocused?1:0,view.CameraMode));
            instruments.WriteLine(JsonUtility.ToJson(new Instruments {source=view.InstrumentSource,text=view.InstrumentText})); result.frames++;
        }
        /// <summary>Capture actual end-of-frame pixels for an airborne or settled state without altering scene assets or generating illustrative imagery.</summary>
        private IEnumerator Capture(string stage)
        {
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image==null) { Finish(false,"route-capture-missing-frame"); yield break; }
            string original=FoundationBootstrap.Options.CapturePath;
            string path=Path.Combine(Path.GetDirectoryName(original),Path.GetFileNameWithoutExtension(original)+"."+stage+".png");
            File.WriteAllBytes(path,image.EncodeToPNG()); Destroy(image);
            if(stage=="airborne") result.airborneCapture=true; else result.landedCapture=true;
            Debug.Log("FIRELINE_ROUTE_CAPTURE stage="+stage);
        }
        /// <summary>Flush owned evidence/metrics, terminate the normal transport safely and return a distinct actual process result.</summary>
        private void Finish(bool passed,string reason)
        {
            if(finished) return; finished=true;
            result.passed=passed; result.reason=reason; result.seconds=Time.realtimeSinceStartupAsDouble-started;
            result.snapshots=session.SnapshotCount; result.acceptedInputs=session.AcceptedInputs; result.identity=session.Identity;
            trace?.Dispose(); instruments?.Dispose(); commands?.Dispose(); trace=null; instruments=null; commands=null;
            FindAnyObjectByType<ClientProbe>()?.SaveMetrics(); session.EndRouteProof();
            File.WriteAllText(FoundationBootstrap.Options.FlightRouteProofPath,JsonUtility.ToJson(result,true));
            Debug.Log("FIRELINE_ROUTE_CLIENT role="+result.role+" passed="+passed+" reason="+reason);
            StartCoroutine(QuitAfterDisconnect(passed));
        }
        /// <summary>Allow the queued transport disconnect to run through real player-loop updates before ending the process.</summary>
        private IEnumerator QuitAfterDisconnect(bool passed)
        {
            yield return new WaitForSecondsRealtime(.15f);
            Application.Quit(passed?0:16);
        }
        /// <summary>Release only proof-owned streams/subscriptions when the process shuts down outside normal proof completion.</summary>
        private void OnDestroy()
        {
            trace?.Dispose(); instruments?.Dispose(); commands?.Dispose();
            if(view!=null) view.RouteInputQueued-=Queued;
        }
    }
}
