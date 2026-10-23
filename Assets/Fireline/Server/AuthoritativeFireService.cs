using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>Only server code owns this world. No M01 client mutation protocol is exposed.</summary>
    public sealed class AuthoritativeFireService
    {
        /// <summary>
        /// The simulation instance owned by this server adapter; it is not a replicated client world.
        /// </summary>
        public FireWorld World { get; }
        /// <summary>
        /// Wrap the supplied simulation for server-side callers; the caller creates/configures the world.
        /// </summary>
        public AuthoritativeFireService(FireWorld world) { World = world; }
        /// <summary>
        /// Apply a trusted test dose in kilograms over validated cell weights; this is not a player equipment command.
        /// </summary>
        public void ApplyManualTestWater(double massKg, WaterHit[] coverage) => World.ApplyWater(massKg, coverage);
        /// <summary>
        /// Always reject a client-supplied water mutation and return a human-readable reason without touching state.
        /// Future equipment commands need independent server validation; this method is not an authentication protocol.
        /// </summary>
        public bool TryApplyClientWater(double massKg, WaterHit[] coverage, out string rejection)
        {
            rejection = "M01 exposes no client water command; future equipment commands require server validation.";
            return false;
        }
    }
}
