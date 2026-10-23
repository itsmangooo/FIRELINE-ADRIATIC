using System;
using System.Globalization;
using System.IO;
using Fireline.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fireline.Server
{
    /// <summary>Dedicated-server adapter. The synthetic M01 fixture is not regional incident generation.</summary>
    public sealed class FireSimulationRunner : MonoBehaviour
    {
        public static AuthoritativeFireService Authority { get; private set; }
        private double accumulated;

#if UNITY_SERVER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() => new GameObject("M01 Authoritative Fire").AddComponent<FireSimulationRunner>();
#endif

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            Authority = new AuthoritativeFireService(FireVerification.Fixture(8, 0));
            Debug.Log("FIRELINE_SIMULATION_START hz=5 grid=32 cellM=10 seed=415 authority=server fixture=synthetic");
            string path = FoundationBootstrap.Options.SimulationProofPath;
            if (path == null) return;
            try
            {
                bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null && Camera.allCamerasCount == 0
                    && GraphicsSettings.defaultRenderPipeline == null && QualitySettings.renderPipeline == null;
                if (!headless) throw new InvalidOperationException("Simulation acceptance requires a graphics-free dedicated server.");
                var result = FireVerification.Run();
                string output = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(result, true));
                using (var writer = new StreamWriter(Path.ChangeExtension(output, ".csv")))
                {
                    writer.WriteLine("scenario,tick,seconds,burned_area_m2,fuel_consumed_kg,water_retained_kg,intensity_sum,smoke_sum,centroid_east_m,centroid_north_m,step_ms,visited_cells");
                    foreach (var run in result.cases)
                    foreach (var row in run.samples)
                    {
                        string N(double n) => n.ToString("R", CultureInfo.InvariantCulture);
                        writer.WriteLine(string.Join(",", row.scenario, row.tick.ToString(CultureInfo.InvariantCulture), N(row.seconds),
                            N(row.burnedAreaM2), N(row.fuelConsumedKg), N(row.waterRetainedKg), N(row.intensitySum), N(row.smokeSum),
                            N(row.centroidEastM), N(row.centroidNorthM), N(row.stepMs), row.visitedCells.ToString(CultureInfo.InvariantCulture)));
                    }
                }
                foreach (var run in result.cases) Debug.Log($"FIRELINE_SIMULATION_CASE name={run.scenario} ticks=600 checksum={run.checksum}");
                Debug.Log($"FIRELINE_SIMULATION_PROOF passed=True headless={headless} cases={result.cases.Length}");
                Application.Quit(0); enabled = false;
            }
            catch (Exception error)
            {
                Debug.LogError("FIRELINE_SIMULATION_PROOF passed=False " + error.Message);
                Application.Quit(5); enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (Authority == null) return;
            accumulated += Time.fixedDeltaTime;
            while (accumulated + 1e-9 >= Authority.World.StepSeconds)
            { Authority.World.Step(); accumulated -= Authority.World.StepSeconds; }
        }

        private void OnDestroy()
        {
            if (Authority != null) Debug.Log($"FIRELINE_SIMULATION_STOP ticks={Authority.World.Tick} checksum={Authority.World.Checksum()}");
            Authority = null;
        }
    }
}
