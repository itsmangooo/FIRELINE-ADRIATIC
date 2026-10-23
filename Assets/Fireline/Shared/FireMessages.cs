using Unity.Collections;
using Unity.NetCode;
using Unity.Entities;

namespace Fireline.Shared
{
    /// <summary>Reliable RPC fragment below one packet: one server-owned baseline/delta transfer, never a client fire command.</summary>
    public struct FireFrameRpc : IRpcCommand
    {
        public uint Transfer;
        public int Index, Count, Characters;
        public byte Kind, Frozen;
        public FixedString512Bytes Body;
    }

    /// <summary>Subscribe=1, exact-digest ACK=2, repair=3, ping=4, pong=5. No ignition/water/score mutation is represented.</summary>
    public struct FireControlRpc : IRpcCommand
    {
        public int Kind;
        public uint Transfer;
        public FixedString64Bytes Digest;
    }

    /// <summary>Small role-neutral ECS send helpers; protocol decisions remain in the server/client adapters.</summary>
    public static class FireRpcIO
    {
        /// <summary>Queue one reliable control RPC for a specific existing connection.</summary>
        public static void Control(EntityManager manager, Entity connection, int kind, uint transfer = 0, string digest = "")
        {
            var entity = manager.CreateEntity(typeof(FireControlRpc), typeof(SendRpcCommandRequest));
            manager.SetComponentData(entity, new FireControlRpc { Kind = kind, Transfer = transfer, Digest = new FixedString64Bytes(digest) });
            manager.SetComponentData(entity, new SendRpcCommandRequest { TargetConnection = connection });
        }

        /// <summary>Queue one bounded frame fragment; Netcode serializes and reliably sends it on its existing UDP connection.</summary>
        public static void Frame(EntityManager manager, Entity connection, FireFrameRpc value)
        {
            var entity = manager.CreateEntity(typeof(FireFrameRpc), typeof(SendRpcCommandRequest));
            manager.SetComponentData(entity, value);
            manager.SetComponentData(entity, new SendRpcCommandRequest { TargetConnection = connection });
        }
    }
}
