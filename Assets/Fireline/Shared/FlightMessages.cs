using Unity.Entities;
using Unity.NetCode;

namespace Fireline.Shared
{
    /// <summary>One version for the bounded aircraft protocol; version two adds the server's sampled horizontal wind to each pose.</summary>
    public static class FlightWire
    {
        public const byte Version = 2;
    }
    /// <summary>Stable request outcomes, independent of transport or localized UI text.</summary>
    public enum FlightResult : byte { Accepted = 0, NotOwner = 1, Sequence = 2, Tick = 3, Invalid = 4, Rate = 5, Protocol = 6 }
    /// <summary>Versioned single-aircraft commands. Identity is always ReceiveRpcCommandRequest.SourceConnection.</summary>
    [System.Serializable]
    public struct FlightRequestRpc : IRpcCommand
    {
        // Subscribe=1, claim=2, input=3, release=4, exact snapshot ACK=5. No pose, wind, fuel or water mutation is exposed.
        public byte Version, Kind;
        public uint Epoch, Sequence;
        public ulong Tick;
        public float Throttle, Pitch, Roll, Yaw, Flaps, Gear, Brake;
    }

    /// <summary>Targeted request outcome; the sequence correlates a reply, never a claimed client identity.</summary>
    public struct FlightReplyRpc : IRpcCommand
    {
        public byte Version, Kind, Result;
        public uint Sequence, Epoch;
        public int Owner;
    }

    /// <summary>Complete server pose in SI units, using unquantized doubles and one stop-and-wait snapshot per peer.</summary>
    [System.Serializable]
    public struct FlightStateRpc : IRpcCommand
    {
        public byte Version, Faulted;
        public uint Epoch, Sequence;
        public int Owner;
        public ulong Tick, FireTick, WindRevision;
        public double X, Y, Z, Vx, Vy, Vz, Wx, Wy, Wz, Qx, Qy, Qz, Qw, FuelKg, WaterKg;
        public double WindX, WindZ;
    }

    /// <summary>Typed targeted RPC enqueue helpers without simulation or presentation dependencies.</summary>
    public static class FlightRpcIO
    {
        /// <summary>Send one small command to a known server connection.</summary>
        public static void Request(EntityManager manager, Entity connection, FlightRequestRpc value)
        {
            var entity = manager.CreateEntity(typeof(FlightRequestRpc), typeof(SendRpcCommandRequest));
            manager.SetComponentData(entity, value);
            manager.SetComponentData(entity, new SendRpcCommandRequest { TargetConnection = connection });
        }
        /// <summary>Return one bounded request outcome to its actual source connection.</summary>
        public static void Reply(EntityManager manager, Entity connection, FlightReplyRpc value)
        {
            var entity = manager.CreateEntity(typeof(FlightReplyRpc), typeof(SendRpcCommandRequest));
            manager.SetComponentData(entity, value);
            manager.SetComponentData(entity, new SendRpcCommandRequest { TargetConnection = connection });
        }
        /// <summary>Publish one complete authoritative snapshot to a subscribed peer.</summary>
        public static void State(EntityManager manager, Entity connection, FlightStateRpc value)
        {
            var entity = manager.CreateEntity(typeof(FlightStateRpc), typeof(SendRpcCommandRequest));
            manager.SetComponentData(entity, value);
            manager.SetComponentData(entity, new SendRpcCommandRequest { TargetConnection = connection });
        }
    }
}
