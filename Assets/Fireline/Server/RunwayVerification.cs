using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Fireline.Shared;
using Fireline.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fireline.Server
{
    /// <summary>A recorded post-step state and reevaluated contact forces, not the earlier integration force.</summary>
    [Serializable]
    public sealed class RunwayObservation
    {
        public double seconds;
        public FlightState state;
        public ContactForces contact;
    }

    /// <summary>All initial conditions and every physical sample for one reproducible numerical case.</summary>
    [Serializable]
    public sealed class RunwayCase
    {
        public string name;
        public bool before;
        public double dt, densityKgM3, brake;
        public FlightState initial;
        public FlightInput input;
        public FlightVector windMps;
        public RunwayObservation[] samples;
    }

    /// <summary>Actual headless evidence for the contact prerequisite; deliberately not a live aircraft or graphical flight proof.</summary>
    [Serializable]
    public sealed class RunwayProof
    {
        public string profile = DebugRunwayProfile.Identifier, flightProfile = DebugFlightProfile.Identifier;
        public bool passed, headless;
        public RunwayPlane plane;
        public RunwayCase[] cases;
        public double[] stepMs;
        public int warmupCalls = 100, timedCalls = 1000;
        public double checksumGuard;
    }

    /// <summary>Opt-in accelerated numerical verification in the actual dedicated executable; normal runtime remains unchanged.</summary>
    public sealed class RunwayVerification : MonoBehaviour
    {
#if UNITY_SERVER
        /// <summary>Install this proof only when its dedicated-server command-line option is explicitly supplied.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FoundationBootstrap.Options.RunwayProofPath != null) new GameObject("M03 Runway Verification").AddComponent<RunwayVerification>();
        }
#endif

        /// <summary>Require actual Null graphics/no cameras/pipelines, record JSON/CSV and exit 0 or explicit runway-proof failure 11.</summary>
        private void Start()
        {
            try
            {
                bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null && Camera.allCamerasCount == 0
                    && GraphicsSettings.defaultRenderPipeline == null && QualitySettings.renderPipeline == null;
                if (!headless) throw new InvalidOperationException("Runway proof requires a headless dedicated player.");
                var result = Run(); result.headless = headless; result.passed = true;
                string output = Path.GetFullPath(FoundationBootstrap.Options.RunwayProofPath);
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, JsonUtility.ToJson(result, true));
                using (var writer = new StreamWriter(Path.ChangeExtension(output, ".csv")))
                {
                    writer.WriteLine("case,seconds,x_m,height_m,z_m,vx_mps,vy_mps,vz_mps,qx,qy,qz,qw,normal_n,compression_m,friction_power_w,supported_wheels");
                    foreach (var run in result.cases) foreach (var row in run.samples)
                    {
                        var s = row.state; var c = row.contact;
                        writer.WriteLine(string.Join(",", run.name, N(row.seconds), N(s.PositionM.X), N(s.PositionM.Y), N(s.PositionM.Z),
                            N(s.VelocityMps.X), N(s.VelocityMps.Y), N(s.VelocityMps.Z), N(s.Attitude.X), N(s.Attitude.Y), N(s.Attitude.Z), N(s.Attitude.W),
                            N(c.NormalN), N(c.MaximumCompressionM), N(c.FrictionPowerW), c.SupportedWheels));
                    }
                }
                UnityEngine.Debug.Log($"FIRELINE_RUNWAY_PROOF passed=True headless={headless} cases={result.cases.Length}");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("FIRELINE_RUNWAY_PROOF passed=False error=" + error.GetType().Name + " " + error.Message);
                Application.Quit(11);
            }
        }

        /// <summary>Format actual double samples independently of the host's locale.</summary>
        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Construct an explicitly generic body; its gear layout is not derived from the imported visual mesh.</summary>
        public static FlightState Fixture(double height = 2.05, double speed = 0, double water = 0) => new FlightState {
            PositionM = new FlightVector(0, height, 0), VelocityMps = new FlightVector(0, 0, speed),
            Attitude = FlightRotation.Identity, FuelKg = 400, WaterKg = water };

        /// <summary>Record all steps for matched previous/free-body and new/contact paths, with explicit atmosphere, commands and SI duration.</summary>
        private static RunwayCase Case(string name, FlightState initial, double seconds = 10, double dt = .005,
            double brake = 1, double density = 0, FlightVector wind = default, bool before = false)
        {
            var state = initial; var input = new FlightInput { Gear = 1 };
            var rows = new List<RunwayObservation>();
            for (int i = 0; i < (int)Math.Round(seconds / dt); i++)
            {
                state = before ? FlightDynamics.Step(state, input, wind, density, dt)
                    : RunwayContact.Step(state, input, wind, density, dt, brake, RunwayPlane.Debug);
                rows.Add(new RunwayObservation { seconds = (i + 1) * dt, state = state,
                    contact = before ? new ContactForces() : RunwayContact.Evaluate(state, input, brake, RunwayPlane.Debug) });
            }
            return new RunwayCase { name = name, initial = initial, input = input, dt = dt, densityKgM3 = density,
                brake = brake, windMps = wind, before = before, samples = rows.ToArray() };
        }

        /// <summary>Exercise rest, payload, touchdown, brake/coast, lateral slip, crosswind and exact airborne compatibility; time isolated fresh-state contact calls separately.</summary>
        public static RunwayProof Run()
        {
            var lateral = Fixture(1.85); lateral.VelocityMps.X = 5;
            var touchdown = Fixture(4); touchdown.VelocityMps.Y = -1;
            var analytic = Fixture(2 - 10000 * DebugFlightProfile.GravityMps2 / (3 * DebugRunwayProfile.SpringNPerM)); analytic.FuelKg = 0;
            var result = new RunwayProof { plane = RunwayPlane.Debug, cases = new[] {
                Case("before_rest", Fixture(), seconds: 2, before: true),
                Case("rest", Fixture()), Case("rest_fine", Fixture(), dt: .0025),
                Case("loaded", Fixture(water: 4000)),
                Case("before_brake", Fixture(1.85, 25), before: true),
                Case("coast", Fixture(1.85, 25), brake: 0), Case("brake", Fixture(1.85, 25)),
                Case("touchdown", touchdown, seconds: 8), Case("lateral", lateral, seconds: 8),
                Case("crosswind", Fixture(), seconds: 5, density: 1.225, wind: new FlightVector(8, 0, 0)),
                Case("airborne_before", Fixture(1000, 60), seconds: 2, density: 1.225, before: true),
                Case("airborne_after", Fixture(1000, 60), seconds: 2, density: 1.225),
                Case("analytic_static", analytic, seconds: 2) } };
            var input = new FlightInput { Gear = 1 }; var initial = Fixture(1.85);
            for (int i = 0; i < result.warmupCalls; i++) RunwayContact.Step(initial, input, new FlightVector(), 1.225, .005, 1, RunwayPlane.Debug);
            result.stepMs = new double[result.timedCalls]; var watch = new Stopwatch();
            for (int i = 0; i < result.timedCalls; i++)
            {
                watch.Restart(); var next = RunwayContact.Step(initial, input, new FlightVector(), 1.225, .005, 1, RunwayPlane.Debug); watch.Stop();
                result.stepMs[i] = watch.Elapsed.TotalMilliseconds; result.checksumGuard += next.PositionM.Y;
            }
            return result;
        }
    }
}
