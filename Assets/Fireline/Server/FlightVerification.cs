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
    /// <summary>One post-step physical observation; the force telemetry is reevaluated at the recorded state, not the earlier integration input.</summary>
    [Serializable]
    public sealed class FlightObservation
    {
        public double seconds;
        public FlightState state;
        public FlightForces forces;
        public FlightVector windMps;
    }

    /// <summary>Explicit initial state, commands, environment, time step and every post-step sample for one isolated numerical case.</summary>
    [Serializable]
    public sealed class FlightCase
    {
        public string name;
        public double dt, densityKgM3;
        public FlightState initial;
        public FlightInput input;
        public FlightVector windMps;
        public bool sampleExistingWind;
        public FlightObservation[] samples;
    }

    /// <summary>Debug-only flight evidence: no mesh, runway, pilot RPC, aircraft calibration or production capacity claim.</summary>
    [Serializable]
    public sealed class FlightProofResult
    {
        public string profile = DebugFlightProfile.Identifier, runtime;
        public bool passed, headless;
        public FlightCase[] cases;
        public double[] stepMs;
        public int timingWarmupCalls = 100, timingCalls = 1000, gc0Collections;
        public double checksumGuard;
        public FlightPolarPoint[] polar;
        public FuelProof fuel;
        public FlightRouteProof route;
    }

    /// <summary>Actual debug lift-curve evaluations at two flap settings; angles are degrees for the report only.</summary>
    [Serializable]
    public sealed class FlightPolarPoint
    {
        public double alphaDegrees, cleanCl, flapsCl;
    }

    /// <summary>Opt-in dedicated-player numerical harness; ordinary fire/replication behavior is unchanged when the flag is absent.</summary>
    public sealed class FlightVerification : MonoBehaviour
    {
#if UNITY_SERVER
        /// <summary>Install only the isolated flight proof requested on the dedicated-server command line.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FoundationBootstrap.Options.FlightProofPath != null) new GameObject("M03 Debug Flight Verification").AddComponent<FlightVerification>();
        }
#endif

        /// <summary>Run actual pure steps in a graphics-free player, write JSON/CSV, and return exit 0 or explicit proof failure 10.</summary>
        private void Start()
        {
            try
            {
                bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null && Camera.allCamerasCount == 0
                    && GraphicsSettings.defaultRenderPipeline == null && QualitySettings.renderPipeline == null;
                if (!headless) throw new InvalidOperationException("Flight proof requires a headless dedicated player.");
                var result = Run(); result.headless = headless; result.passed = true;
                string output = Path.GetFullPath(FoundationBootstrap.Options.FlightProofPath);
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, JsonUtility.ToJson(result, true));
                using (var writer = new StreamWriter(Path.ChangeExtension(output, ".csv")))
                {
                    writer.WriteLine("case,seconds,east_m,height_m,north_m,vx_mps,vy_mps,vz_mps,wx_radps,wy_radps,wz_radps,airspeed_mps,alpha_rad,lift_n,drag_n,mass_kg,cg_z_m");
                    foreach (var run in result.cases) foreach (var row in run.samples)
                    {
                        var s = row.state; var f = row.forces;
                        writer.WriteLine(string.Join(",", run.name, N(row.seconds), N(s.PositionM.X), N(s.PositionM.Y), N(s.PositionM.Z),
                            N(s.VelocityMps.X), N(s.VelocityMps.Y), N(s.VelocityMps.Z), N(s.AngularVelocityRadps.X), N(s.AngularVelocityRadps.Y), N(s.AngularVelocityRadps.Z),
                            N(f.AirspeedMps), N(f.AlphaRad), N(f.LiftN), N(f.DragN), N(f.Mass.Kg), N(f.Mass.CgZ)));
                    }
                }
                UnityEngine.Debug.Log($"FIRELINE_FLIGHT_PROOF passed=True headless={headless} profile={result.profile} cases={result.cases.Length}");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("FIRELINE_FLIGHT_PROOF passed=False error=" + error.GetType().Name + " " + error.Message);
                Application.Quit(10);
            }
        }

        /// <summary>Write locale-independent round-trip numeric CSV values; no thousands separators or decimal commas.</summary>
        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Make a deliberately generic, untrimmed body at 1000 m; this is not an actual CL-415 configuration.</summary>
        public static FlightState Fixture(double water = 0) => new FlightState { PositionM = new FlightVector(0, 1000, 0),
            VelocityMps = new FlightVector(0, 0, 60), Attitude = FlightRotation.Identity, FuelKg = 400, WaterKg = water };

        /// <summary>Run a bounded accelerated case; optional wind uses the actual M01 field, held at each 0.2 s authoritative weather tick.</summary>
        private static FlightCase Case(string name, double dt, double seconds, double water, FlightInput input,
            FlightVector wind, double density = 1.225, bool existingWind = false)
        {
            var state = Fixture(water); var initial = state; var rows = new List<FlightObservation>();
            var field = new WindField(415); field.Set(8, 0, 2);
            int count = (int)Math.Round(seconds / dt);
            for (int i = 0; i < count; i++)
            {
                var currentWind = wind;
                if (existingWind)
                {
                    var sample = field.Sample(state.PositionM.X, state.PositionM.Z, state.PositionM.Y, (ulong)Math.Floor(i * dt / 0.2));
                    currentWind = new FlightVector(sample.X, 0, sample.Z);
                }
                state = FlightDynamics.Step(state, input, currentWind, density, dt);
                rows.Add(new FlightObservation { seconds = (i + 1) * dt, state = state, windMps = currentWind,
                    forces = FlightDynamics.Evaluate(state, input, currentWind, density) });
            }
            return new FlightCase { name = name, dt = dt, densityKgM3 = density, initial = initial,
                input = input, windMps = wind, sampleExistingWind = existingWind, samples = rows.ToArray() };
        }

        /// <summary>Collect repeatable trajectories and a separately warmed single-Step microdiagnostic; no real-time host/client load is represented.</summary>
        public static FlightProofResult Run()
        {
            var result = new FlightProofResult { runtime = Environment.Version.ToString(), cases = new[] {
                Case("ballistic_20ms", .02, 4, 0, new FlightInput(), new FlightVector(), 0),
                Case("ballistic_10ms", .01, 4, 0, new FlightInput(), new FlightVector(), 0),
                Case("ballistic_5ms", .005, 4, 0, new FlightInput(), new FlightVector(), 0),
                Case("dry", .005, 4, 0, new FlightInput { Throttle = .5 }, new FlightVector()),
                Case("loaded", .005, 4, 4000, new FlightInput { Throttle = .5 }, new FlightVector()),
                Case("dry_10ms", .01, 4, 0, new FlightInput { Throttle = .5 }, new FlightVector()),
                Case("crosswind", .005, 4, 0, new FlightInput { Throttle = .5 }, new FlightVector(8, 0, 0)),
                Case("server_wind", .005, 4, 0, new FlightInput { Throttle = .5 }, new FlightVector(), existingWind: true),
                Case("controls", .005, 2, 0, new FlightInput { Throttle = .5, Pitch = .2, Roll = .1, Yaw = .1 }, new FlightVector(8, 0, 0)) } };
            var polar = new List<FlightPolarPoint>();
            for (int angle = -30; angle <= 30; angle += 2)
                polar.Add(new FlightPolarPoint { alphaDegrees = angle, cleanCl = FlightDynamics.LiftCurve(angle * Math.PI / 180, 0),
                    flapsCl = FlightDynamics.LiftCurve(angle * Math.PI / 180, 1) });
            result.polar = polar.ToArray();
            var initial = Fixture(); var input = new FlightInput { Throttle = .5 };
            for (int i = 0; i < result.timingWarmupCalls; i++) FlightDynamics.Step(initial, input, new FlightVector(), 1.225, .005);
            result.stepMs = new double[result.timingCalls]; int before = GC.CollectionCount(0); var watch = new Stopwatch();
            for (int i = 0; i < result.stepMs.Length; i++)
            {
                watch.Restart(); var next = FlightDynamics.Step(initial, input, new FlightVector(), 1.225, .005); watch.Stop();
                result.stepMs[i] = watch.Elapsed.TotalMilliseconds; result.checksumGuard += next.PositionM.Y;
            }
            result.gc0Collections = GC.CollectionCount(0) - before;
            result.fuel = FuelVerification.Run(); result.route = FlightRouteVerification.Run(); return result;
        }
    }
}
