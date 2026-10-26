using System;
using Fireline.Shared;
using Fireline.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fireline.Client
{
    /// <summary>M03 engineering view: received-pose model placement, two external cameras, telemetry and validated test controls.</summary>
    public sealed class FlightTestView : MonoBehaviour
    {
        private ClientFlightSession session;
        private readonly FlightPoseTrack track = new FlightPoseTrack();
        private FlightTestControls controls = new FlightTestControls();
        private Transform anchor, visual;
        private Camera camera;
        private UnityEngine.UI.Text detail, status, instruments;
        private GameObject canvas;
        private ulong shownTick;
        private double nextInput;
        private readonly FlightRouteControls routeControls = new FlightRouteControls();
        /// <summary>Actual enqueued QA control and its received source; absent subscribers impose no trace writes in normal play.</summary>
        public event Action<FlightStateRpc, FlightRequestRpc, int> RouteInputQueued;
        /// <summary>Latched QA phase for explicit route evidence only.</summary>
        public int RoutePhase => routeControls.Phase;
        /// <summary>Latest cosmetic pose used for the scene transform; it is not an independently simulated aircraft.</summary>
        public FlightState Displayed { get; private set; }
        /// <summary>Exact receipt-clock sample time used for the cosmetic pose, retained for independent interpolation checks.</summary>
        public double SampledAt { get; private set; }
        /// <summary>Normalized local command sampled this frame, before server metadata is attached.</summary>
        public FlightRequestRpc LocalControls { get; private set; }
        /// <summary>External chase=0 or external inspection=1; neither is a verified cockpit.</summary>
        public int CameraMode { get; private set; }
        /// <summary>Number of frames in which a retained pose was applied to the visible model.</summary>
        public int AppliedFrames { get; private set; }
        /// <summary>Proof-only logical focus gate; normal play uses the real application focus.</summary>
        public bool ProofFocus { get; set; } = true;
        /// <summary>Focus value actually used for this frame's control sample, distinct from a proof gate scheduled later in the frame.</summary>
        public bool InputFocused { get; private set; }
        /// <summary>Actual displayed CoG world position, for render-adapter verification.</summary>
        public Vector3 AnchorPosition => anchor.position;
        /// <summary>Actual body-origin offset under the CoG anchor, preserving the imported mesh's original orientation.</summary>
        public Vector3 VisualOffset => visual.localPosition;
        /// <summary>The actual visible instrument text; proof code records it rather than reconstructing a substitute display.</summary>
        public string InstrumentText => instruments.text;
        /// <summary>The exact server snapshot used to format the visible instruments this Update, retained across later network callbacks.</summary>
        public FlightStateRpc InstrumentSource { get; private set; }

        /// <summary>Install only in the separate opt-in test scene, preserving every earlier scene and static preview.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!FoundationBootstrap.Options.FlightView) return;
            if (SceneManager.GetActiveScene().name != "M03_FlightTest")
            { Debug.LogError("FIRELINE_FLIGHT_VIEW wrong-scene"); Application.Quit(15); return; }
            new GameObject("M03 Flight Engineering View").AddComponent<FlightTestView>();
        }

        /// <summary>Bind named generated scene anchors and create one engineering overlay; missing references fail visibly.</summary>
        private void Start()
        {
            session = FindAnyObjectByType<ClientFlightSession>();
            anchor = GameObject.Find("M03 Aircraft CoG Pose").transform; visual = anchor.GetChild(0); camera = Camera.main;
            if (session == null || camera == null) throw new InvalidOperationException("Flight test scene/session is incomplete.");
            CreateOverlay();
            if (FoundationBootstrap.Options.FlightViewProofPath != null) gameObject.AddComponent<FlightViewVerification>();
            Debug.Log("FIRELINE_FLIGHT_VIEW ready=True mode=engineering physics=server cameras=external");
        }

        /// <summary>Replace only the local input reader with explicit proof devices; commands still pass through the normal adapter/server guards.</summary>
        public void UseProofControls(FlightTestControls value) { controls = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>Read physical or proof devices, send 20 Hz fresh-owner inputs, and let focus loss expire the server lease.</summary>
        private void Update()
        {
            if (session == null) return;
            double now = Time.realtimeSinceStartupAsDouble;
            bool fresh = session.HasState && now - session.ReceivedAt <= .5;
            bool owns = fresh && session.Latest.Owner == session.Identity;
            bool route = FoundationBootstrap.Options.FlightRouteRole == "owner";
            bool focus = route || (FoundationBootstrap.Options.FlightViewProofPath != null ? ProofFocus : Application.isFocused);
            InputFocused=focus;
            var inputSource = session.Latest;
            LocalControls = route ? (owns ? routeControls.Sample(inputSource) : new FlightRequestRpc { Gear=1,Brake=1 })
                : controls.Sample(Time.unscaledDeltaTime, focus, owns);
            if (!route && controls.Claim) session.RequestSeat();
            if (!route && controls.Release) session.ReleaseSeat();
            if (!route && controls.Camera) CameraMode = 1 - CameraMode;
            if (owns && focus && now >= nextInput)
            {
                if (session.SubmitControls(LocalControls) && route) RouteInputQueued?.Invoke(inputSource,session.LastSubmitted,routeControls.Phase);
                nextInput = now + .05;
            }
            status.text = !session.Ready ? "CONNECTING TO DEDICATED SERVER" : !fresh ? "STALE POSE / INPUT BLOCKED"
                : owns ? "PILOT SEAT / SERVER AUTHORITY" : "OBSERVER / ENTER TO REQUEST SEAT";
            if (!focus) status.text += " / INPUT UNFOCUSED";
            var s = session.Latest;
            if (session.HasState)
            {
                InstrumentSource = s;
                instruments.text = FlightTelemetry.Format(FlightTelemetry.Read(s));
            }
            detail.text = $"CLIENT {session.Identity}   PILOT {s.Owner}   SEAT EPOCH {s.Epoch}\n"
                + $"AIRCRAFT TICK {s.Tick}   FIRE TICK {s.FireTick}\n"
                + $"Ground speed {Math.Sqrt(s.Vx*s.Vx+s.Vz*s.Vz):F2} m/s\n"
                + $"CoG altitude {s.Y:F2} m   Vertical {s.Vy:F2} m/s\n"
                + $"Local throttle {LocalControls.Throttle:P0}   Flaps {LocalControls.Flaps:P0}\n"
                + $"Local gear {LocalControls.Gear:F0}   Brake {LocalControls.Brake:F0}\n"
                + $"Snapshots {session.SnapshotCount}   Accepted inputs {session.AcceptedInputs}\n"
                + $"EXTERNAL {(CameraMode == 0 ? "CHASE" : "INSPECTION")} / 100 ms interpolation";
        }

        /// <summary>Apply interpolated authoritative CoG/attitude and explicit debug body offset, then frame it with an external camera.</summary>
        private void LateUpdate()
        {
            if (session == null || !session.HasState) return;
            var s = session.Latest;
            if (!track.Ready || s.Tick != shownTick)
            {
                track.Push(new FlightState { PositionM = new FlightVector(s.X,s.Y,s.Z), VelocityMps = new FlightVector(s.Vx,s.Vy,s.Vz),
                    AngularVelocityRadps = new FlightVector(s.Wx,s.Wy,s.Wz), Attitude = new FlightRotation(s.Qx,s.Qy,s.Qz,s.Qw),
                    FuelKg=s.FuelKg, WaterKg=s.WaterKg }, s.Tick, s.Epoch, session.ReceivedAt);
                shownTick = s.Tick;
            }
            SampledAt=Time.realtimeSinceStartupAsDouble; Displayed = track.Sample(SampledAt);
            var p = Displayed.PositionM; var q = Displayed.Attitude;
            anchor.SetPositionAndRotation(new Vector3((float)p.X,(float)p.Y,(float)p.Z), new Quaternion((float)q.X,(float)q.Y,(float)q.Z,(float)q.W));
            visual.localPosition = new Vector3(0,-2,(float)-FlightDynamics.Mass(s.FuelKg,s.WaterKg).CgZ);
            var heading = Quaternion.Euler(0,anchor.eulerAngles.y,0);
            camera.transform.position = anchor.position + heading * (CameraMode == 0 ? new Vector3(0,12,-44) : new Vector3(32,16,35));
            camera.transform.LookAt(anchor.position + Vector3.up*3, Vector3.up); AppliedFrames++;
        }

        /// <summary>Create readable anchored uGUI telemetry and a compact control guide without changing any saved UI asset.</summary>
        private void CreateOverlay()
        {
            canvas = new GameObject("M03 Engineering HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            var panel = Panel("Telemetry",new Vector2(0,1),new Vector2(16,-16),new Vector2(435,252));
            Label(panel,"FIRELINE / M03 FLIGHT TEST",new Vector2(14,-12),new Vector2(410,26),20);
            status = Label(panel,"CONNECTING",new Vector2(14,-44),new Vector2(410,22),14); status.color = new Color(.64f,.89f,.8f);
            detail = Label(panel,"Waiting for server pose",new Vector2(14,-74),new Vector2(410,170),16);
            var data = Panel("AirData",new Vector2(1,1),new Vector2(-16,-16),new Vector2(360,250));
            Label(data,"INSTRUMENTS / SERVER SAMPLE",new Vector2(14,-12),new Vector2(334,25),17);
            instruments = Label(data,"Waiting for validated server data",new Vector2(14,-48),new Vector2(334,194),14);
            var guide = Panel("Controls",new Vector2(0,0),new Vector2(16,16),new Vector2(1248,91));
            Label(guide,"ENTER claim | BACKSPACE release | SHIFT / CTRL throttle | W/S pitch | A/D roll | Q/E yaw | SPACE brake | F flaps | G gear | C camera\n"
                + "PAD: START / SELECT seat | triggers throttle | left stick pitch/roll | right X yaw | A brake | Y gear | D-pad UP flaps | RB camera\n"
                + "ESTIMATED DEBUG BODY / SYNTHETIC RUNWAY / NO WATER / EXTERNAL CAMERAS / TIRE-ONLY CONTACT",new Vector2(12,-10),new Vector2(1224,76),14);
        }

        /// <summary>Create one noninteractive corner-anchored panel with an opaque-enough background for HDRP daylight.</summary>
        private RectTransform Panel(string name, Vector2 corner, Vector2 position, Vector2 size)
        {
            var item = new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); item.transform.SetParent(canvas.transform,false);
            var rect = item.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=corner; rect.anchoredPosition=position; rect.sizeDelta=size;
            var background = item.GetComponent<UnityEngine.UI.Image>(); background.color = new Color(.025f,.045f,.065f,.9f);
            background.raycastTarget = false; return rect;
        }

        /// <summary>Attach built-in-font text to the panel's top-left with explicit bounds and no input raycast.</summary>
        private static UnityEngine.UI.Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var item = new GameObject("Label",typeof(RectTransform),typeof(UnityEngine.UI.Text)); item.transform.SetParent(parent,false);
            var label=item.GetComponent<UnityEngine.UI.Text>(); label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text=text; label.fontSize=fontSize; label.color=Color.white; label.raycastTarget=false;
            var rect=label.rectTransform; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=position; rect.sizeDelta=size;
            return label;
        }

        /// <summary>Remove only the runtime engineering overlay when this adapter is destroyed.</summary>
        private void OnDestroy() { if(canvas!=null) Destroy(canvas); }
    }
}
