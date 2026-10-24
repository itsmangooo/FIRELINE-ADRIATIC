using System;
using System.IO;
using Fireline.Shared;
using Fireline.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Fireline.Client
{
    /// <summary>Read-only aircraft replica and opt-in scripted network probe. No local aircraft integrator or graphical binding exists yet.</summary>
    public sealed class ClientFlightSession : MonoBehaviour
    {
        [Serializable]
        private sealed class Evidence
        {
            public bool passed, sawOwner, sawVacantAfterOwner;
            public string role, reason;
            public int identity, snapshots, acceptedInputs, rejected, sequenceRejects, epochRejects, invalidRejects, tickRejects;
            public ulong firstTick, finalTick;
            public double seconds, firstZ, lastZ, maximumReceiveGapSeconds;
        }
        private readonly Evidence evidence = new Evidence();
        private EntityManager manager;
        private EntityQuery states, replies, connections;
        private Entity connection;
        private FlightStateRpc latest;
        private double started, receivedAt, ownedAt, nextInput;
        private uint sequence;
        private int attack;
        private bool connected, subscribed, claimed, received, finished;
        private StreamWriter snapshots;

        /// <summary>Install only for explicit debug flight sessions; the existing static preview remains unchanged by default.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FoundationBootstrap.Options.FlightSession) new GameObject("M03 Aircraft Replica").AddComponent<ClientFlightSession>();
        }

        /// <summary>Bind actual client transport queries; proof output is optional and ordinary debug sessions only receive state.</summary>
        private void Start()
        {
            if (FoundationBootstrap.Options.ConnectAddress == null) { enabled = false; return; }
            DontDestroyOnLoad(gameObject); manager = FoundationBootstrap.RuntimeWorld.EntityManager;
            states = manager.CreateEntityQuery(typeof(FlightStateRpc), typeof(ReceiveRpcCommandRequest));
            replies = manager.CreateEntityQuery(typeof(FlightReplyRpc), typeof(ReceiveRpcCommandRequest));
            connections = manager.CreateEntityQuery(typeof(NetworkId));
            started = receivedAt = Time.realtimeSinceStartupAsDouble;
            evidence.role = FoundationBootstrap.Options.FlightProbeRole ?? "replica";
            string path = FoundationBootstrap.Options.FlightNetworkProofPath;
            if (path != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                snapshots = new StreamWriter(Path.ChangeExtension(path, ".snapshots.jsonl"));
            }
        }

        /// <summary>Subscribe once, validate server-only monotonically advancing snapshots, acknowledge exact ticks, and run proof commands if requested.</summary>
        private void Update()
        {
            if (finished || FoundationBootstrap.Options.ConnectAddress == null) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (!connected && !connections.IsEmptyIgnoreFilter)
            {
                connection = connections.GetSingletonEntity(); evidence.identity = connections.GetSingleton<NetworkId>().Value;
                connected = true; Send(1);
            }
            if (connected && (!manager.Exists(connection) || !manager.HasComponent<NetworkId>(connection)))
            { Finish(false, "disconnected-before-proof"); return; }
            using (var messages = replies.ToEntityArray(Allocator.Temp))
                foreach (var entity in messages)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection;
                    var reply = manager.GetComponentData<FlightReplyRpc>(entity); manager.DestroyEntity(entity);
                    if (source != connection || reply.Version != 1) continue;
                    if (reply.Kind == 1 && reply.Result == 0) subscribed = true;
                    if (reply.Kind == 3 && reply.Result == 0) evidence.acceptedInputs++;
                    if (reply.Result != 0)
                    {
                        evidence.rejected++;
                        if (reply.Result == (byte)FlightResult.Sequence) evidence.sequenceRejects++;
                        if (reply.Result == (byte)FlightResult.NotOwner) evidence.epochRejects++;
                        if (reply.Result == (byte)FlightResult.Invalid) evidence.invalidRejects++;
                        if (reply.Result == (byte)FlightResult.Tick) evidence.tickRejects++;
                    }
                }
            using (var messages = states.ToEntityArray(Allocator.Temp))
                foreach (var entity in messages)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection;
                    var state = manager.GetComponentData<FlightStateRpc>(entity); manager.DestroyEntity(entity);
                    if (source != connection) continue;
                    try { Validate(state); }
                    catch (ArgumentException) { Finish(false, "invalid-server-snapshot"); return; }
                    if (state.Faulted != 0 || (received && (state.Tick <= latest.Tick || state.FireTick < latest.FireTick || state.Epoch < latest.Epoch)))
                    { Finish(false, "fault-or-nonmonotonic-state"); return; }
                    if (!received) { evidence.firstTick = state.Tick; evidence.firstZ = state.Z; }
                    else evidence.maximumReceiveGapSeconds = Math.Max(evidence.maximumReceiveGapSeconds, now - receivedAt);
                    received = true; receivedAt = now; latest = state; evidence.snapshots++; evidence.finalTick = state.Tick; evidence.lastZ = state.Z;
                    if (state.Owner != 0) evidence.sawOwner = true;
                    if (evidence.sawOwner && state.Owner == 0) evidence.sawVacantAfterOwner = true;
                    snapshots?.WriteLine(JsonUtility.ToJson(state)); Send(5, tick: state.Tick);
                }
            if (subscribed && FoundationBootstrap.Options.FlightNetworkProofPath != null) Probe(now);
            if (now - receivedAt > 15 || now - started > 45 && FoundationBootstrap.Options.FlightNetworkProofPath != null)
                Finish(false, "snapshot-or-proof-timeout");
        }

        /// <summary>Exercise ownership, stale/invalid commands, a lease timeout, owner disconnect and late observer state in real transport.</summary>
        private void Probe(double now)
        {
            if (evidence.role == "owner")
            {
                if (!claimed) { Send(2); claimed = true; }
                if (!received || latest.Owner != evidence.identity) return;
                if (ownedAt == 0) ownedAt = now;
                double age = now - ownedAt;
                if (age < 4 || age > 5.3 && age < 8)
                {
                    if (now >= nextInput) { Send(3, ++sequence, latest.Epoch, latest.Tick, .35f); nextInput = now + .05; }
                }
                if (attack < 4 && age > .3 + attack * .3)
                {
                    if (attack == 0) Send(3, 0, latest.Epoch, latest.Tick);
                    if (attack == 1) Send(3, uint.MaxValue, latest.Epoch - 1, latest.Tick);
                    if (attack == 2) Send(3, uint.MaxValue, latest.Epoch, latest.Tick, float.NaN);
                    if (attack == 3) Send(3, uint.MaxValue, latest.Epoch, latest.Tick + 100000);
                    attack++;
                }
                if (age > 9)
                {
                    bool pass = evidence.acceptedInputs >= 20 && evidence.snapshots >= 30 && evidence.lastZ - evidence.firstZ > 1
                        && evidence.sequenceRejects >= 1 && evidence.epochRejects >= 1 && evidence.invalidRejects >= 1 && evidence.tickRejects >= 1;
                    // The transport disconnect, rather than a release command, must make the server vacate the seat.
                    manager.AddComponent<NetworkStreamRequestDisconnect>(connection); Finish(pass, "owner-input-and-disconnect");
                }
            }
            else if (evidence.role == "observer" && received)
            {
                if (attack == 0 && latest.Owner != 0)
                {
                    Send(2); Send(3, 1, latest.Epoch, latest.Tick, 1);
                    FlightRpcIO.State(manager, connection, new FlightStateRpc { Version = 1, Owner = evidence.identity, Z = 999999 }); attack++;
                }
                if (evidence.sawVacantAfterOwner && evidence.snapshots >= 35 && evidence.rejected >= 2)
                    Finish(true, "observer-replica-and-seat-release");
            }
        }

        /// <summary>Send finite normal commands or deliberate proof-invalid payloads; no caller can attach a claimed connection identity.</summary>
        private void Send(byte kind, uint serial = 0, uint epoch = 0, ulong tick = 0, float throttle = 0)
        {
            FlightRpcIO.Request(manager, connection, new FlightRequestRpc { Version = 1, Kind = kind, Sequence = serial,
                Epoch = epoch, Tick = tick, Throttle = throttle, Gear = 1, Brake = kind == 3 ? 0 : 1 });
        }

        /// <summary>Check the received SI pose before installation without advancing any physical state on the client.</summary>
        private static void Validate(FlightStateRpc state)
        {
            if (state.Version != 1 || state.Owner < 0 || state.Faulted > 1) throw new ArgumentException("Invalid state header.");
            new FlightState { PositionM = new FlightVector(state.X, state.Y, state.Z), VelocityMps = new FlightVector(state.Vx, state.Vy, state.Vz),
                AngularVelocityRadps = new FlightVector(state.Wx, state.Wy, state.Wz), Attitude = new FlightRotation(state.Qx, state.Qy, state.Qz, state.Qw),
                FuelKg = state.FuelKg, WaterKg = state.WaterKg }.Validate();
        }

        /// <summary>Close the proof trace, record measured replica outcomes and exit with a distinct failure code.</summary>
        private void Finish(bool passed, string reason)
        {
            if (finished) return; finished = true; snapshots?.Dispose(); snapshots = null;
            evidence.passed = passed; evidence.reason = reason; evidence.seconds = Time.realtimeSinceStartupAsDouble - started;
            string path = FoundationBootstrap.Options.FlightNetworkProofPath;
            if (path != null) File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
            Debug.Log($"FIRELINE_FLIGHT_NETWORK role={evidence.role} passed={passed} reason={reason} snapshots={evidence.snapshots}");
            Application.Quit(passed ? 0 : 13);
        }

        /// <summary>Close optional raw output when the client stops outside its scripted proof.</summary>
        private void OnDestroy() { snapshots?.Dispose(); }
    }
}
