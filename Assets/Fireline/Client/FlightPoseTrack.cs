using System;
using Fireline.Simulation;

namespace Fireline.Client
{
    /// <summary>Cosmetic two-pose interpolation with 100 ms receipt-time delay and no extrapolation or local physics.</summary>
    public sealed class FlightPoseTrack
    {
        private FlightState older, newer;
        private double olderAt, newerAt;
        private ulong lastTick;
        private uint lastEpoch;
        /// <summary>Whether at least one validated pose was retained.</summary>
        public bool Ready { get; private set; }

        /// <summary>Keep increasing poses; reset on seat-generation changes or jumps above 100 m, never interpolate across a reset.</summary>
        public void Push(FlightState state, ulong tick, uint epoch, double receivedAt)
        {
            state.Validate();
            if (double.IsNaN(receivedAt) || double.IsInfinity(receivedAt) || receivedAt < 0
                || Ready && (tick <= lastTick || epoch < lastEpoch || receivedAt <= newerAt)) throw new ArgumentException("Nonmonotonic presentation pose.");
            if (!Ready || epoch != lastEpoch || (state.PositionM - newer.PositionM).Length > 100)
            { older = state; olderAt = receivedAt; }
            else { older = newer; olderAt = newerAt; }
            newer = state; newerAt = receivedAt; lastTick = tick; lastEpoch = epoch; Ready = true;
        }

        /// <summary>Interpolate position/attitude within retained endpoints; clamp beyond them instead of inventing future flight.</summary>
        public FlightState Sample(double now)
        {
            if (!Ready || double.IsNaN(now) || double.IsInfinity(now)) throw new InvalidOperationException("No finite presentation time/state.");
            double span = newerAt - olderAt;
            double t = span <= 0 ? 1 : Math.Max(0, Math.Min(1, (now - .1 - olderAt) / span));
            var result = newer;
            result.PositionM = older.PositionM * (1 - t) + newer.PositionM * t;
            var a = older.Attitude; var b = newer.Attitude;
            double sign = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W < 0 ? -1 : 1;
            result.Attitude = new FlightRotation(a.X * (1 - t) + b.X * t * sign, a.Y * (1 - t) + b.Y * t * sign,
                a.Z * (1 - t) + b.Z * t * sign, a.W * (1 - t) + b.W * t * sign).Normalized();
            return result;
        }
    }
}
