using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fireline.Shared;
using Fireline.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Fireline.Client
{
    /// <summary>Read-only replica: installs validated server state and never runs FireWorld.Step or client fire mutations.</summary>
    public sealed class ClientFireReplication : MonoBehaviour
    {
        [Serializable]
        private sealed class Evidence
        {
            public bool passed;
            public string role = "client", checksum, reason;
            public ulong tick;
            public int networkId, baselines, deltas, repairs, pongs, rejected, charredCells, initialCharredCells, frameSamples;
            public ulong initialTick;
            public long payloadCharacters;
            public double seconds;
        }

        private EntityManager manager;
        private EntityQuery frames, controls, connections;
        private Entity connection;
        private FireSnapshot snapshot;
        private FireFrameAssembly assembly;
        private FireFrameRpc header;
        private FireStateView view;
        private uint latestTransfer;
        private double started, lastPong, nextPing, assemblyStarted;
        private bool ready, subscribed, finishing, failed;
        private readonly FoundationMetrics metrics = new FoundationMetrics();
        private double lastRender;
        private readonly Evidence evidence = new Evidence();

        /// <summary>Attach only to the calibration client scene; the coastal performance fixture is kept unchanged.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name == "M00_Client") new GameObject("M02 Read-only Fire Replica").AddComponent<ClientFireReplication>();
        }

        /// <summary>Bind receive queries only for an explicitly connected client and create its engineering state display.</summary>
        private void Start()
        {
            if (FoundationBootstrap.Options.ConnectAddress == null) { enabled = false; return; }
            DontDestroyOnLoad(gameObject); manager = FoundationBootstrap.RuntimeWorld.EntityManager;
            frames = manager.CreateEntityQuery(typeof(FireFrameRpc), typeof(ReceiveRpcCommandRequest));
            controls = manager.CreateEntityQuery(typeof(FireControlRpc), typeof(ReceiveRpcCommandRequest));
            connections = manager.CreateEntityQuery(typeof(NetworkId));
            view = gameObject.AddComponent<FireStateView>();
            started = lastPong = Time.realtimeSinceStartupAsDouble; ready = true;
            RenderPipelineManager.endCameraRendering += Rendered;
        }

        /// <summary>Subscribe on handshake, process bounded transfers, send one-second pings and detect missing heartbeat/disconnect.</summary>
        private void Update()
        {
            if (!ready || failed) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (!subscribed && !connections.IsEmptyIgnoreFilter)
            {
                connection = connections.GetSingletonEntity(); evidence.networkId = connections.GetSingleton<NetworkId>().Value;
                FireRpcIO.Control(manager, connection, 1); subscribed = true; lastPong = now;
                if (FoundationBootstrap.Options.ReplicationProofPath != null) FireRpcIO.Control(manager, connection, 99);
            }
            if (subscribed)
            {
                if (!manager.Exists(connection) || !manager.HasComponent<NetworkId>(connection)) { Fail("disconnected", 9); return; }
                if (now >= nextPing) { FireRpcIO.Control(manager, connection, 4); nextPing = now + 1; }
            }
            using (var messages = controls.ToEntityArray(Allocator.Temp))
                foreach (var entity in messages)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection;
                    var message = manager.GetComponentData<FireControlRpc>(entity); manager.DestroyEntity(entity);
                    if (source != connection) continue;
                    if (message.Kind == 5) { lastPong = now; evidence.pongs++; }
                    if (message.Kind == 6) evidence.rejected++;
                }
            using (var messages = frames.ToEntityArray(Allocator.Temp))
                foreach (var entity in messages)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection;
                    var frame = manager.GetComponentData<FireFrameRpc>(entity); manager.DestroyEntity(entity);
                    if (source == connection) Receive(frame, now);
                }
            if (assembly != null && now - assemblyStarted > 10) Repair("fragment-timeout");
            if (subscribed && now - lastPong > 15) { Fail("heartbeat-timeout", 9); return; }
            if (FoundationBootstrap.Options.ReplicationProofPath != null && now - started > 100) Fail("proof-timeout", 8);
        }

        /// <summary>Assemble one exact transfer, check base/revisions/full state, then ACK its digest only after installing the result.</summary>
        private void Receive(FireFrameRpc frame, double now)
        {
            if (frame.Transfer < latestTransfer || finishing) return;
            try
            {
                if (frame.Kind < 1 || frame.Kind > 2 || frame.Frozen > 1) throw new InvalidDataException("Unknown frame kind.");
                if (frame.Transfer > latestTransfer)
                {
                    assembly = new FireFrameAssembly(frame.Count, frame.Characters); header = frame;
                    latestTransfer = frame.Transfer; assemblyStarted = now;
                }
                if (assembly == null) return;
                if (header.Count != frame.Count || header.Characters != frame.Characters || header.Kind != frame.Kind || header.Frozen != frame.Frozen)
                    throw new InvalidDataException("Inconsistent fragments.");
                evidence.payloadCharacters += frame.Body.Length;
                string text = assembly.Add(frame.Index, frame.Body.ToString()); if (text == null) return;
                byte[] raw = FireReplicationCodec.Unpack(text);
                var target = frame.Kind == 1 ? FireSnapshotCodec.Decode(raw) : FireReplicationCodec.Apply(snapshot, raw);
                if (snapshot != null && target.Tick < snapshot.Tick) throw new InvalidDataException("Stale target tick.");
                snapshot = target; assembly = null;
                if (evidence.baselines == 0)
                { evidence.initialTick = snapshot.Tick; evidence.initialCharredCells = snapshot.Chunks.Sum(c => c.Cells.Count(v => v.Vegetation == VegetationPhase.Charred)); }
                if (frame.Kind == 1) evidence.baselines++; else evidence.deltas++;
                evidence.checksum = snapshot.Checksum; evidence.tick = snapshot.Tick;
                evidence.charredCells = snapshot.Chunks.Sum(c => c.Cells.Count(v => v.Vegetation == VegetationPhase.Charred));
                FireRpcIO.Control(manager, connection, 2, frame.Transfer, snapshot.Checksum);
                view.Show(snapshot, evidence.networkId, evidence.baselines, evidence.deltas, evidence.repairs);
                Debug.Log($"FIRELINE_FIRE_APPLY kind={frame.Kind} tick={snapshot.Tick} checksum={snapshot.Checksum} charred={evidence.charredCells}");
                if (frame.Frozen == 1 && FoundationBootstrap.Options.ReplicationProofPath != null)
                { finishing = true; StartCoroutine(FinishProof()); }
            }
            catch (Exception error) when (error is InvalidDataException || error is ArgumentException || error is IOException)
            { Repair(error.Message); }
        }

        /// <summary>Keep the last valid state visible and request a new baseline when a transfer cannot be applied in order.</summary>
        private void Repair(string reason)
        {
            assembly = null; evidence.repairs++;
            FireRpcIO.Control(manager, connection, 3);
            Debug.Log("FIRELINE_FIRE_GAP repair=True reason=" + reason);
        }

        /// <summary>Capture actual final Game View pixels and the canonical state file after a rendered frame; exit only after proof output succeeds.</summary>
        private IEnumerator FinishProof()
        {
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            string path = Path.GetFullPath(FoundationBootstrap.Options.ReplicationProofPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            bool rendered = image != null && image.width >= 640 && image.height >= 360;
            if (rendered) { File.WriteAllBytes(Path.ChangeExtension(path, ".png"), image.EncodeToPNG()); Destroy(image); }
            File.WriteAllBytes(Path.ChangeExtension(path, ".fls"), FireSnapshotCodec.Encode(snapshot));
            evidence.passed = rendered && snapshot.Tick == 120 && evidence.charredCells > 0 && evidence.pongs > 0;
            evidence.seconds = Time.realtimeSinceStartupAsDouble - started;
            evidence.frameSamples = metrics.SampleCount;
            if (metrics.SampleCount > 0) metrics.Save(Path.ChangeExtension(path, ".metrics.json"));
            File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
            Debug.Log($"FIRELINE_REPLICATION_PROOF role=client passed={evidence.passed} tick={snapshot.Tick} checksum={snapshot.Checksum}");
            Application.Quit(evidence.passed ? 0 : 8);
        }

        /// <summary>Report stale/disconnected state without running local fire; proof runs return an explicit nonzero process exit.</summary>
        private void Fail(string reason, int code)
        {
            Debug.Log("FIRELINE_FIRE_DISCONNECTED reason=" + reason); finishing = true; failed = true;
            if (view != null) view.Disconnected(reason);
            string path = FoundationBootstrap.Options.ReplicationProofPath;
            if (path == null) return;
            evidence.reason = reason; evidence.passed = false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
            Application.Quit(code);
        }

        /// <summary>Record actual main-camera callback spacing after five wall seconds of warm-up; this is not pure GPU/CPU execution time.</summary>
        private void Rendered(ScriptableRenderContext context, Camera camera)
        {
            if (camera != Camera.main) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - started > 5 && lastRender > 0) metrics.Sample((float)(now - lastRender));
            lastRender = now;
        }

        /// <summary>Dispose ECS queries only while their owner remains valid at shutdown.</summary>
        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= Rendered;
            if (ready && FoundationBootstrap.RuntimeWorld != null && FoundationBootstrap.RuntimeWorld.IsCreated)
            { frames.Dispose(); controls.Dispose(); connections.Dispose(); }
        }
    }
}
