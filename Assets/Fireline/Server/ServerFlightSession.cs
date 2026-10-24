using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Fireline.Shared;
using Fireline.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Fireline.Server
{
    /// <summary>Opt-in live single-aircraft host over bounded reliable RPCs; production ghosts/admission/interest management remain pending.</summary>
    public sealed class ServerFlightSession : MonoBehaviour
    {
        [Serializable]
        private sealed class Peer
        {
            public int identity, accepted, rejected, acknowledgements, snapshots, forgedPoses;
            public bool disconnected;
            [NonSerialized] public ulong PendingTick;
            [NonSerialized] public bool Pending;
            [NonSerialized] public double SentAt, Next;
            [NonSerialized] public int Budget;
        }
        [Serializable]
        private sealed class Evidence
        {
            public bool passed, faulted;
            public string reason, role = "server", profile = DebugFlightProfile.Identifier;
            public ulong tick, fireTick;
            public int owner, disconnectReleases, expirations, overruns, updates, steps;
            public double seconds, discardedSeconds, maximumUpdateMs;
            public FlightState finalState;
            public Peer[] peers;
        }
        private readonly Dictionary<Entity, Peer> peers = new Dictionary<Entity, Peer>();
        private readonly List<Peer> history = new List<Peer>();
        private readonly Evidence evidence = new Evidence();
        private FlightAuthority authority;
        private EntityManager manager;
        private EntityQuery requests, invalidStates, invalidReplies;
        private double started, previous;
        private bool ready, finished;
        private StreamWriter samples;
        private StreamWriter snapshots;

        [Serializable]
        private sealed class Published
        {
            public int identity;
            public FlightStateRpc state;
        }

#if UNITY_SERVER
        /// <summary>Install only for an explicitly selected debug flight session; ordinary M00/M01/M02 operation remains unchanged.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FoundationBootstrap.Options.FlightSession) new GameObject("M03 Server Aircraft").AddComponent<ServerFlightSession>();
        }
#endif
        /// <summary>Bind the actual server world and create one estimated aircraft without any render asset reference.</summary>
        private void Start()
        {
            DontDestroyOnLoad(gameObject); manager = FoundationBootstrap.RuntimeWorld.EntityManager;
            requests = manager.CreateEntityQuery(typeof(FlightRequestRpc), typeof(ReceiveRpcCommandRequest));
            invalidStates = manager.CreateEntityQuery(typeof(FlightStateRpc), typeof(ReceiveRpcCommandRequest));
            invalidReplies = manager.CreateEntityQuery(typeof(FlightReplyRpc), typeof(ReceiveRpcCommandRequest));
            authority = new FlightAuthority(); started = previous = Time.realtimeSinceStartupAsDouble; ready = true;
            string path = FoundationBootstrap.Options.FlightNetworkProofPath;
            if (path != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                samples = new StreamWriter(Path.ChangeExtension(path, ".csv"));
                snapshots = new StreamWriter(Path.ChangeExtension(path, ".snapshots.jsonl"));
                samples.WriteLine("elapsed_s,callback_gap_s,steps,tick,fire_tick,owner,epoch,z_m,speed_mps,discarded_s,host_advance_ms");
            }
            Debug.Log("FIRELINE_FLIGHT_SESSION ready=True hz=200 maximumSteps=8 authority=server profile=DEBUG_FLIGHT_ONLY_V1");
        }

        /// <summary>Validate requests before stepping, use the actual authoritative wind/fire tick, and publish at most one pending pose per peer.</summary>
        private void Update()
        {
            if (!ready || finished || FireSimulationRunner.Authority == null) return;
            double now = Time.realtimeSinceStartupAsDouble, gap = now - previous; previous = now;
            foreach (var peer in peers.Values) peer.Budget = 0;
            using (var messages = requests.ToEntityArray(Allocator.Temp))
            {
                int budget = 64;
                foreach (var entity in messages)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection;
                    var message = manager.GetComponentData<FlightRequestRpc>(entity); manager.DestroyEntity(entity);
                    if (--budget >= 0) Receive(source, message, now);
                    else Disconnect(source);
                }
            }
            RejectPublishedState(invalidStates); RejectPublishedState(invalidReplies);
            foreach (var pair in peers.ToArray())
            {
                if (!Live(pair.Key))
                {
                    if (authority.Owner == pair.Value.identity)
                    { authority.Release(authority.Owner, authority.Epoch); evidence.disconnectReleases++; }
                    pair.Value.disconnected = true; peers.Remove(pair.Key); continue;
                }
                if (pair.Value.Pending && now - pair.Value.SentAt > 5) Disconnect(pair.Key);
            }
            var world = FireSimulationRunner.Authority.World;
            var timer = Stopwatch.StartNew(); int count = authority.Advance(gap, now, world.Wind, world.Tick); timer.Stop();
            double ms = timer.Elapsed.TotalMilliseconds;
            evidence.updates++; evidence.steps += count; evidence.maximumUpdateMs = Math.Max(evidence.maximumUpdateMs, ms);
            if (samples != null)
            {
                // Keep each recorded scalar culture-independent and round-trip safe for the external checker.
                string N(double number) => number.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                samples.WriteLine(string.Join(",", N(now - started), N(gap), count, authority.Tick, world.Tick, authority.Owner,
                    authority.Epoch, N(authority.State.PositionM.Z), N(authority.State.VelocityMps.Length), N(authority.DiscardedSeconds), N(ms)));
            }
            foreach (var pair in peers)
            {
                var peer = pair.Value;
                if (!Live(pair.Key) || peer.Pending || now < peer.Next) continue;
                var snapshot = Snapshot(world);
                FlightRpcIO.State(manager, pair.Key, snapshot);
                snapshots?.WriteLine(JsonUtility.ToJson(new Published { identity = peer.identity, state = snapshot }));
                peer.Pending = true; peer.PendingTick = authority.Tick; peer.SentAt = now; peer.Next = now + .1; peer.snapshots++;
            }
            if (authority.Faulted) { Finish(false, "physics-envelope"); return; }
            if (FoundationBootstrap.Options.FlightNetworkProofPath != null)
            {
                // Completion depends on real peer ACKs, a pilot disconnect and the observer still receiving state.
                if (evidence.disconnectReleases > 0 && authority.Owner == 0 && history.Any(p => p.accepted >= 20 && p.disconnected)
                    && history.Any(p => p.acknowledgements >= 30 && p.rejected >= 2 && p.forgedPoses >= 1)
                    && authority.Expirations >= 1 && now - started >= 14) Finish(true, "two-client-authority-proof");
                else if (now - started > 50) Finish(false, "network-proof-timeout");
            }
        }

        /// <summary>Read actual transport identity, bound per-peer work, and dispatch only the versioned command vocabulary.</summary>
        private void Receive(Entity source, FlightRequestRpc message, double now)
        {
            if (!Live(source)) return;
            if (!peers.TryGetValue(source, out var peer))
            {
                if (message.Kind != 1 || message.Version != 1 || history.Count >= 8) { Disconnect(source); return; }
                peer = new Peer { identity = manager.GetComponentData<NetworkId>(source).Value }; peers.Add(source, peer); history.Add(peer);
            }
            if (++peer.Budget > 8) { Disconnect(source); return; }
            FlightResult result = FlightResult.Protocol;
            if (message.Version == 1)
            {
                switch (message.Kind)
                {
                    case 1: result = FlightResult.Accepted; break;
                    case 2: result = authority.Claim(peer.identity); break;
                    case 3:
                        result = authority.Submit(peer.identity, message.Epoch, message.Sequence, message.Tick,
                            new FlightInput { Throttle = message.Throttle, Pitch = message.Pitch, Roll = message.Roll, Yaw = message.Yaw,
                                Flaps = message.Flaps, Gear = message.Gear }, message.Brake, now); break;
                    case 4: result = authority.Release(peer.identity, message.Epoch); break;
                    case 5:
                        if (peer.Pending && message.Tick == peer.PendingTick)
                        { peer.Pending = false; peer.acknowledgements++; return; }
                        result = FlightResult.Tick; break;
                }
            }
            if (result == FlightResult.Accepted) peer.accepted++; else peer.rejected++;
            FlightRpcIO.Reply(manager, source, new FlightReplyRpc { Version = 1, Kind = message.Kind, Result = (byte)result,
                Sequence = message.Sequence, Epoch = authority.Epoch, Owner = authority.Owner });
        }

        /// <summary>Reject every client-published pose/reply without touching the aircraft; never trust the claimed owner in that payload.</summary>
        private void RejectPublishedState(EntityQuery query)
        {
            using (var messages = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in messages)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection; manager.DestroyEntity(entity);
                    if (peers.TryGetValue(source, out var peer)) { peer.forgedPoses++; peer.rejected++; }
                }
        }

        /// <summary>Determine connection liveness from the server ECS entity and handshake identity, never a client-supplied integer.</summary>
        private bool Live(Entity connection) => manager.Exists(connection) && manager.HasComponent<NetworkId>(connection)
            && !manager.HasComponent<NetworkStreamRequestDisconnect>(connection);

        /// <summary>Ask Netcode to close a flooding or unacknowledging connection; ownership cleanup follows liveness detection.</summary>
        private void Disconnect(Entity connection)
        {
            if (manager.Exists(connection) && !manager.HasComponent<NetworkStreamRequestDisconnect>(connection))
                manager.AddComponent<NetworkStreamRequestDisconnect>(connection);
        }

        /// <summary>Copy the complete immutable physical state plus seat and wind metadata into the small role-neutral wire schema.</summary>
        private FlightStateRpc Snapshot(FireWorld world)
        {
            var s = authority.State;
            return new FlightStateRpc { Version = 1, Faulted = (byte)(authority.Faulted ? 1 : 0), Epoch = authority.Epoch,
                Owner = authority.Owner, Sequence = authority.Sequence, Tick = authority.Tick, FireTick = world.Tick, WindRevision = world.Wind.State.Revision,
                X = s.PositionM.X, Y = s.PositionM.Y, Z = s.PositionM.Z, Vx = s.VelocityMps.X, Vy = s.VelocityMps.Y, Vz = s.VelocityMps.Z,
                Wx = s.AngularVelocityRadps.X, Wy = s.AngularVelocityRadps.Y, Wz = s.AngularVelocityRadps.Z,
                Qx = s.Attitude.X, Qy = s.Attitude.Y, Qz = s.Attitude.Z, Qw = s.Attitude.W, FuelKg = s.FuelKg, WaterKg = s.WaterKg };
        }

        /// <summary>Write bounded acceptance evidence and return a distinct process failure; no capacity or rendered-frame claim is made.</summary>
        private void Finish(bool passed, string reason)
        {
            if (finished) return; finished = true; samples?.Dispose(); samples = null; snapshots?.Dispose(); snapshots = null;
            evidence.passed = passed; evidence.reason = reason; evidence.faulted = authority.Faulted; evidence.tick = authority.Tick;
            evidence.owner = authority.Owner; evidence.finalState = authority.State; evidence.peers = history.ToArray();
            evidence.seconds = Time.realtimeSinceStartupAsDouble - started; evidence.discardedSeconds = authority.DiscardedSeconds;
            evidence.expirations = authority.Expirations; evidence.overruns = authority.Overruns;
            evidence.fireTick = FireSimulationRunner.Authority.World.Tick;
            string path = FoundationBootstrap.Options.FlightNetworkProofPath;
            if (path != null) File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
            Debug.Log($"FIRELINE_FLIGHT_NETWORK role=server passed={passed} reason={reason} tick={authority.Tick} owner={authority.Owner}");
            Application.Quit(passed ? 0 : 12);
        }

        /// <summary>Close the optional raw trace on controlled application shutdown.</summary>
        private void OnDestroy() { samples?.Dispose(); snapshots?.Dispose(); }
    }
}
