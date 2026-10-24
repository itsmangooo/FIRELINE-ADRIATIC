using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Fireline.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fireline.Client
{
    /// <summary>Opt-in rendered acceptance through synthetic Input System devices, normal view controls and actual server RPCs.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class FlightViewVerification : MonoBehaviour
    {
        [Serializable]
        private sealed class Evidence
        {
            public bool passed, keyboardThrottle, gamepadThrottle, gamepadAxes, neutralOnFocusLoss, released, reclaimed, inspectionCapture;
            public string role="rendered-owner", reason;
            public int identity, snapshots, acceptedInputs, appliedFrames, keyboardEvents, gamepadEvents, cameraMask;
            public double seconds, distanceM, maximumAnchorErrorM, maximumVisualOffsetErrorM;
        }
        private readonly Evidence evidence=new Evidence();
        private ClientFlightSession session;
        private FlightTestView view;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private double launched,started,ownedAt;
        private double initialZ;
        private bool captured,finished;
        private StreamWriter trace, instrumentTrace;
        [Serializable]
        private sealed class InstrumentFrame
        {
            public FlightStateRpc source;
            public string text;
        }
        private InputSettings.BackgroundBehavior previousBackground;

        /// <summary>Use isolated devices so the proof never sends native OS keystrokes or claims physical gamepad hardware was exercised.</summary>
        private void Start()
        {
            session=FindAnyObjectByType<ClientFlightSession>(); view=GetComponent<FlightTestView>();
            // Keep synthetic queued devices alive in an unfocused QA window; the separate view gate still tests focus-neutral input.
            previousBackground=InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>(); gamepad=InputSystem.AddDevice<Gamepad>();
            view.UseProofControls(new FlightTestControls(keyboard,gamepad)); InputSystem.onEvent+=OnInput;
            launched=Time.realtimeSinceStartupAsDouble;
            string path=FoundationBootstrap.Options.FlightViewProofPath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            trace=new StreamWriter(Path.ChangeExtension(path,".csv"));
            instrumentTrace=new StreamWriter(Path.ChangeExtension(path,".instruments.jsonl"));
            trace.WriteLine("wall_s,received_at_s,aircraft_tick,epoch,owner,received_z_m,displayed_x_m,displayed_y_m,displayed_z_m,qx,qy,qz,qw,anchor_x_m,anchor_y_m,anchor_z_m,visual_y_m,visual_z_m,throttle,pitch,roll,yaw,brake,focused,camera");
        }

        /// <summary>Count processed device events for these two isolated devices rather than events merely queued by the script.</summary>
        private void OnInput(InputEventPtr data,InputDevice device)
        {
            if(device==keyboard) evidence.keyboardEvents++; if(device==gamepad) evidence.gamepadEvents++;
        }

        /// <summary>Queue keyboard/pad state through Unity's normal input update, including a focus pause and an explicit release/reclaim.</summary>
        private void Update()
        {
            if(finished) return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(started==0 && session.Ready && session.HasState) { started=now; initialZ=session.Latest.Z; }
            if(started==0) { if(now-launched>20) Finish(false,"no-live-pose"); return; }
            if(ownedAt==0 && session.Latest.Owner==session.Identity) ownedAt=now;
            double age=ownedAt==0 ? -1 : now-ownedAt;
            view.ProofFocus=!(age>=4 && age<5.3);
            KeyboardState keys=new KeyboardState();
            var pad=new GamepadState();
            if(ownedAt==0 && now-started<.5) keys=new KeyboardState(Key.Enter);
            if(age>=0 && age<.7) keys=new KeyboardState(Key.LeftShift,Key.Space);
            if(age>=2 && age<4 || age>=9 && age<17 || age>=20) keys=new KeyboardState(Key.Space);
            if(age>=6 && age<6.9) pad.rightTrigger=.6f;
            if(age>=7 && age<7.25) { pad.leftStick=new Vector2(.2f,.2f); pad.rightStick=new Vector2(.3f,0); }
            if(age>=11 && age<11.2) pad=pad.WithButton(GamepadButton.RightShoulder);
            if(age>=17 && age<17.2) keys=new KeyboardState(Key.Backspace);
            if(age>=18 && age<18.2) pad=pad.WithButton(GamepadButton.Start);
            InputSystem.QueueStateEvent(keyboard,keys); InputSystem.QueueStateEvent(gamepad,pad);
            if(age>17.4 && age<18 && session.Latest.Owner==0) evidence.released=true;
            if(age>18.3 && evidence.released && session.Latest.Owner==session.Identity) evidence.reclaimed=true;
            if(age>13 && !captured) { captured=true; StartCoroutine(CaptureInspection()); }
            if(age>29)
            {
                evidence.identity=session.Identity; evidence.snapshots=session.SnapshotCount; evidence.acceptedInputs=session.AcceptedInputs;
                evidence.appliedFrames=view.AppliedFrames; evidence.distanceM=session.Latest.Z-initialZ;
                bool pass=evidence.keyboardThrottle && evidence.gamepadThrottle && evidence.gamepadAxes && evidence.neutralOnFocusLoss
                    && evidence.released && evidence.reclaimed && evidence.inspectionCapture && evidence.cameraMask==3
                    && evidence.keyboardEvents>100 && evidence.gamepadEvents>100 && evidence.snapshots>100
                    && evidence.acceptedInputs>100 && evidence.distanceM>1 && evidence.maximumAnchorErrorM<.0001 && evidence.maximumVisualOffsetErrorM<.0001;
                session.Disconnect(); Finish(pass,"rendered-device-and-pose-proof");
            }
            if(now-launched>43) Finish(false,"rendered-proof-timeout");
        }

        /// <summary>Check actual scene transforms after the view's LateUpdate and retain every display sample with source tick/controls.</summary>
        private void LateUpdate()
        {
            if(finished || started==0 || !session.HasState) return;
            double now=Time.realtimeSinceStartupAsDouble,age=ownedAt==0 ? -1 : now-ownedAt;
            var p=view.Displayed.PositionM;
            double error=(view.AnchorPosition-new Vector3((float)p.X,(float)p.Y,(float)p.Z)).magnitude;
            var expected=new Vector3(0,-2,(float)-Fireline.Simulation.FlightDynamics.Mass(session.Latest.FuelKg,session.Latest.WaterKg).CgZ);
            double offset=(view.VisualOffset-expected).magnitude;
            evidence.maximumAnchorErrorM=Math.Max(evidence.maximumAnchorErrorM,error); evidence.maximumVisualOffsetErrorM=Math.Max(evidence.maximumVisualOffsetErrorM,offset);
            var c=view.LocalControls; evidence.cameraMask|=1<<view.CameraMode;
            instrumentTrace.WriteLine(JsonUtility.ToJson(new InstrumentFrame { source=view.InstrumentSource, text=view.InstrumentText }));
            if(age>.2 && age<2 && c.Throttle>.05) evidence.keyboardThrottle=true;
            if(age>6.2 && age<8 && c.Throttle>.05) evidence.gamepadThrottle=true;
            if(age>7 && age<7.5 && Math.Abs(c.Yaw)>.01 && Math.Abs(c.Roll)>.01 && Math.Abs(c.Pitch)>.01) evidence.gamepadAxes=true;
            if(age>4.2 && age<5.1 && c.Throttle==0 && c.Brake==1) evidence.neutralOnFocusLoss=true;
            // Write exact decimal scalars independently of the operating system's locale.
            string N(double n)=>n.ToString("R",CultureInfo.InvariantCulture);
            var q=view.Displayed.Attitude; var anchor=view.AnchorPosition; var visual=view.VisualOffset;
            trace.WriteLine(string.Join(",",N(view.SampledAt-started),N(session.ReceivedAt-started),session.Latest.Tick,session.Latest.Epoch,session.Latest.Owner,N(session.Latest.Z),N(p.X),N(p.Y),N(p.Z),N(q.X),N(q.Y),N(q.Z),N(q.W),N(anchor.x),N(anchor.y),N(anchor.z),N(visual.y),N(visual.z),
                N(c.Throttle),N(c.Pitch),N(c.Roll),N(c.Yaw),N(c.Brake),view.InputFocused?1:0,view.CameraMode));
        }

        /// <summary>Capture the second actual external camera at the rendered end of frame, keeping the first screenshot intact.</summary>
        private IEnumerator CaptureInspection()
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(texture==null) yield break;
            string output=Path.ChangeExtension(FoundationBootstrap.Options.CapturePath,".inspection.png");
            if(output!=null) { File.WriteAllBytes(output,texture.EncodeToPNG()); evidence.inspectionCapture=true; }
            Destroy(texture);
        }

        /// <summary>Save checked view outcomes, flush actual rendered callback metrics and exit with a distinct failure code.</summary>
        private void Finish(bool passed,string reason)
        {
            if(finished) return; finished=true; trace?.Dispose(); trace=null; instrumentTrace?.Dispose(); instrumentTrace=null;
            evidence.passed=passed; evidence.reason=reason; evidence.seconds=Time.realtimeSinceStartupAsDouble-launched;
            FindAnyObjectByType<ClientProbe>().SaveMetrics();
            File.WriteAllText(FoundationBootstrap.Options.FlightViewProofPath,JsonUtility.ToJson(evidence,true));
            Debug.Log($"FIRELINE_FLIGHT_VIEW_PROOF passed={passed} snapshots={evidence.snapshots} inputs={evidence.acceptedInputs} reason={reason}");
            Application.Quit(passed?0:14);
        }

        /// <summary>Release only the proof's synthetic input devices, event subscription and trace on controlled shutdown.</summary>
        private void OnDestroy()
        {
            trace?.Dispose(); instrumentTrace?.Dispose(); InputSystem.onEvent-=OnInput;
            if(keyboard!=null) InputSystem.RemoveDevice(keyboard); if(gamepad!=null) InputSystem.RemoveDevice(gamepad);
            InputSystem.settings.backgroundBehavior=previousBackground;
        }
    }
}
