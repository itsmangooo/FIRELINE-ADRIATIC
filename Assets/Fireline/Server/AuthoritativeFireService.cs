using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>Only server code owns this world. No M01 client mutation protocol is exposed.</summary>
    public sealed class AuthoritativeFireService
    {
        public FireWorld World { get; }
        public AuthoritativeFireService(FireWorld world) { World = world; }
        public void ApplyManualTestWater(double massKg, WaterHit[] coverage) => World.ApplyWater(massKg, coverage);
        public bool TryApplyClientWater(double massKg, WaterHit[] coverage, out string rejection)
        {
            rejection = "M01 exposes no client water command; future equipment commands require server validation.";
            return false;
        }
    }
}
