using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fireline.Shared;
using Fireline.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Fireline.Server
{
    /// <summary>Bounded stop-and-wait M02 prototype over reliable Netcode RPCs; not production interest management or admission security.</summary>
    public sealed class ServerFireReplication : MonoBehaviour
    {
        private sealed class Transfer
        {
            public uint Id;
            public byte Kind, Frozen;
            public string Text;
            public int Part;
            public double Started;
            public FireSnapshot Target;
        }

        [Serializable]
        private sealed class Peer
        {
            public int networkId, baselines, deltas, acknowledgements, repairs, pings, rejected;
            public long payloadCharacters;
            public string checksum;
            public ulong tick;
            [NonSerialized] public double Seen, Next;
            [NonSerialized] public FireSnapshot Acknowledged, Previous;
            [NonSerialized] public Transfer Pending;
            [NonSerialized] public bool Faulted;
        }

        [Serializable]
        private sealed class Evidence
        {
            public bool passed;
            public string role = "server", checksum, loadedChecksum;
            public ulong tick;
            public int charredCells;
            public double waterKg, seconds;
            public Peer[] peers;
        }

        private readonly Dictionary<Entity, Peer> peers = new Dictionary<Entity, Peer>();
        private readonly List<Peer> history = new List<Peer>();
        private EntityQuery controls, unexpectedFrames;
        private EntityManager manager;
        private uint serial;
        private double started, completed;
        private bool ready;

        /// <summary>Allow the new-world proof clock to start only after two real clients acknowledged their initial baselines.</summary>
        public static bool ProofMayAdvance { get; private set; }

#if UNITY_SERVER
        /// <summary>Install the server-only replication host after loading its intentionally empty headless scene.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() => new GameObject("M02 Fire Replication Server").AddComponent<ServerFireReplication>();
#endif

        /// <summary>Bind to the server ECS world and receive only requests carrying their actual source connection.</summary>
        private void Start()
        {
            DontDestroyOnLoad(gameObject); manager = FoundationBootstrap.RuntimeWorld.EntityManager;
            controls = manager.CreateEntityQuery(typeof(FireControlRpc), typeof(ReceiveRpcCommandRequest));
            unexpectedFrames = manager.CreateEntityQuery(typeof(FireFrameRpc), typeof(ReceiveRpcCommandRequest));
            started = Time.realtimeSinceStartupAsDouble; ready = true;
        }

        /// <summary>Process controls, expire silent connections and send at most eight fragments per peer/frame for up to eight peers.</summary>
        private void Update()
        {
            if (!ready || FireSimulationRunner.Authority == null) return;
            double now = Time.realtimeSinceStartupAsDouble;
            using (var requests = controls.ToEntityArray(Allocator.Temp))
                foreach (var entity in requests)
                {
                    var source = manager.GetComponentData<ReceiveRpcCommandRequest>(entity).SourceConnection;
                    var request = manager.GetComponentData<FireControlRpc>(entity); manager.DestroyEntity(entity);
                    Receive(source, request, now);
                }
            // No client is permitted to publish a server fire frame, regardless of claimed transfer metadata.
            using (var invalid = unexpectedFrames.ToEntityArray(Allocator.Temp))
                foreach (var entity in invalid) { manager.DestroyEntity(entity); Debug.Log("FIRELINE_FIRE_REJECT client-frame"); }
            foreach (var pair in peers.ToArray())
            {
                Entity connection = pair.Key; Peer peer = pair.Value;
                if (!manager.Exists(connection) || !manager.HasComponent<NetworkId>(connection)) { Forget(connection, peer); continue; }
                if (now - peer.Seen > 15)
                {
                    if (!manager.HasComponent<NetworkStreamRequestDisconnect>(connection)) manager.AddComponent<NetworkStreamRequestDisconnect>(connection);
                    Debug.Log($"FIRELINE_FIRE_TIMEOUT client={peer.networkId}"); Forget(connection, peer); continue;
                }
                if (peer.Pending != null && now - peer.Pending.Started > 8) { peer.Pending = null; peer.Acknowledged = null; peer.repairs++; }
                if (peer.Pending == null && now >= peer.Next
                    && (peer.Acknowledged == null || peer.Acknowledged.Tick != FireSimulationRunner.Authority.World.Tick))
                    Prepare(peer, now);
                var transfer = peer.Pending;
                if (transfer == null) continue;
                int count = (transfer.Text.Length + FireReplicationCodec.PartCharacters - 1) / FireReplicationCodec.PartCharacters;
                for (int budget = 0; budget < 8 && transfer.Part < count; budget++, transfer.Part++)
                {
                    int offset = transfer.Part * FireReplicationCodec.PartCharacters;
                    string part = transfer.Text.Substring(offset, Math.Min(FireReplicationCodec.PartCharacters, transfer.Text.Length - offset));
                    FireRpcIO.Frame(manager, connection, new FireFrameRpc { Transfer = transfer.Id, Index = transfer.Part, Count = count,
                        Characters = transfer.Text.Length, Kind = transfer.Kind, Frozen = transfer.Frozen, Body = new FixedString512Bytes(part) });
                    peer.payloadCharacters += part.Length;
                }
            }
            if (peers.Values.Count(p => p.Acknowledged != null) >= 2) ProofMayAdvance = true;
            VerifyProof(now);
        }

        /// <summary>Release retained transfer/baseline buffers when a peer leaves; keep only its bounded diagnostic counters.</summary>
        private void Forget(Entity connection, Peer peer)
        {
            peer.Pending = null; peer.Acknowledged = null; peer.Previous = null; peers.Remove(connection);
        }

        /// <summary>Accept bounded subscription/ACK/repair/heartbeat controls; no request can change authoritative fire, wind, water or score.</summary>
        private void Receive(Entity source, FireControlRpc request, double now)
        {
            if (!manager.Exists(source) || !manager.HasComponent<NetworkId>(source)) return;
            if (!peers.TryGetValue(source, out var peer))
            {
                if (request.Kind != 1 || peers.Count >= 8) return;
                peer = new Peer { networkId = manager.GetComponentData<NetworkId>(source).Value, Seen = now };
                peers.Add(source, peer);
                if (history.Count == 32) history.RemoveAt(0); // Bound diagnostic history across long-lived reconnect churn.
                history.Add(peer);
                Debug.Log($"FIRELINE_FIRE_SUBSCRIBE client={peer.networkId}");
            }
            peer.Seen = now;
            switch (request.Kind)
            {
                case 1: break; // A duplicate subscribe never resets an existing transfer.
                case 2:
                    var pending = peer.Pending;
                    if (pending != null && request.Transfer == pending.Id && request.Digest.ToString() == pending.Target.Checksum)
                    {
                        peer.Previous = peer.Acknowledged; peer.Acknowledged = pending.Target;
                        peer.checksum = pending.Target.Checksum; peer.tick = pending.Target.Tick; peer.acknowledgements++;
                        peer.Pending = null; peer.Next = now + 0.5;
                        Debug.Log($"FIRELINE_FIRE_ACK client={peer.networkId} kind={pending.Kind} tick={peer.tick} checksum={peer.checksum}");
                    }
                    break;
                case 3:
                    // Rate-limit repair resets; the heartbeat path does not allocate snapshots.
                    if (now >= peer.Next) { peer.Pending = null; peer.Acknowledged = null; peer.repairs++; peer.Next = now + 0.5; }
                    break;
                case 4: peer.pings++; FireRpcIO.Control(manager, source, 5); break;
                default: peer.rejected++; FireRpcIO.Control(manager, source, 6); Debug.Log($"FIRELINE_FIRE_REJECT client={peer.networkId} kind={request.Kind}"); break;
            }
        }

        /// <summary>Capture one immutable target and form a baseline or exact-base delta; fault injection is explicitly limited to new-world proof mode.</summary>
        private void Prepare(Peer peer, double now)
        {
            var target = FireSimulationRunner.Authority.World.CaptureSnapshot();
            byte kind = peer.Acknowledged == null ? (byte)1 : (byte)2;
            var basis = peer.Acknowledged;
            if (kind == 2 && FoundationBootstrap.Options.ReplicationProofPath != null && FireSimulationRunner.LoadedDigest == null
                && peer.networkId == 1 && !peer.Faulted && peer.Previous != null && target.Tick >= 30)
            {
                basis = peer.Previous; peer.Faulted = true;
                Debug.Log("FIRELINE_FIRE_FAULT injected-stale-base client=1");
            }
            byte[] raw = kind == 1 ? FireSnapshotCodec.Encode(target) : FireReplicationCodec.Delta(basis, target);
            if (raw == null) { kind = 1; raw = FireSnapshotCodec.Encode(target); }
            string text = FireReplicationCodec.Pack(raw);
            peer.Pending = new Transfer { Id = checked(++serial), Kind = kind, Target = target, Text = text,
                Frozen = FireSimulationRunner.Frozen ? (byte)1 : (byte)0, Started = now };
            if (kind == 1) peer.baselines++; else peer.deltas++;
            Debug.Log($"FIRELINE_FIRE_SEND client={peer.networkId} kind={kind} tick={target.Tick} rawBytes={raw.Length} payloadChars={text.Length}");
        }

        /// <summary>Finish only after the frozen target is acknowledged by the requested number of real connections; otherwise fail at 100 seconds.</summary>
        private void VerifyProof(double now)
        {
            string path = FoundationBootstrap.Options.ReplicationProofPath;
            if (path == null) return;
            var world = FireSimulationRunner.Authority.World; string digest = world.Checksum();
            bool passed = FireSimulationRunner.Frozen && peers.Count >= FoundationBootstrap.Options.MinimumClients
                && peers.Values.All(p => p.checksum == digest);
            if (passed && completed == 0) completed = now;
            if ((completed == 0 || now - completed < 4) && now - started < 100) return;
            // Once all clients acknowledged, allow four seconds for their actual rendered proof capture.
            passed = completed > 0;
            var snapshot = world.CaptureSnapshot();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(Path.ChangeExtension(path, ".fls"), FireSnapshotCodec.Encode(snapshot));
            File.WriteAllText(path, JsonUtility.ToJson(new Evidence { passed = passed, checksum = digest, loadedChecksum = FireSimulationRunner.LoadedDigest,
                tick = world.Tick, charredCells = snapshot.Chunks.Sum(c => c.Cells.Count(v => v.Vegetation == VegetationPhase.Charred)),
                waterKg = world.AppliedWaterKg, seconds = now - started, peers = history.ToArray() }, true));
            Debug.Log($"FIRELINE_REPLICATION_PROOF role=server passed={passed} tick={world.Tick} checksum={digest} peers={history.Count}");
            Application.Quit(passed ? 0 : 8); enabled = false;
        }

        /// <summary>Release query handles only while the ECS owner is still alive.</summary>
        private void OnDestroy()
        {
            if (ready && FoundationBootstrap.RuntimeWorld != null && FoundationBootstrap.RuntimeWorld.IsCreated)
            { controls.Dispose(); unexpectedFrames.Dispose(); }
        }
    }
}
