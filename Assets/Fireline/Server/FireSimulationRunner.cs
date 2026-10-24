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
        /// <summary>
        /// Server simulation service installed for this process; cleared when the runner is destroyed.
        /// </summary>
        public static AuthoritativeFireService Authority { get; private set; }
        private double accumulated;
        private double nextSave;
        /// <summary>Proof-only freeze at 120 ticks; normal servers continue their authoritative simulation.</summary>
        public static bool Frozen { get; private set; }
        /// <summary>Digest read from an explicit checkpoint, or null for a newly created world.</summary>
        public static string LoadedDigest { get; private set; }


#if UNITY_SERVER
        /// <summary>
        /// Create the authoritative fire adapter after scene loading only in a dedicated-server build.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() => new GameObject("M01 Authoritative Fire").AddComponent<FireSimulationRunner>();
#endif

        /// <summary>
        /// Start the synthetic live fixture; in proof mode require a graphics-free server, run acceptance and write JSON/CSV.
        /// Log case digests and quit with 0 on success or 5 on failure; this fixture is not a production incident.
        /// </summary>
        private void Start()
        {
            if (FoundationBootstrap.Options.FlightProofPath != null || FoundationBootstrap.Options.RunwayProofPath != null) { enabled = false; return; }
            DontDestroyOnLoad(gameObject);
            try
            {
                var options = FoundationBootstrap.Options;
                var world = options.WorldLoadPath != null ? Fireline.Simulation.FireWorld.Restore(new FileFireSnapshotStore(options.WorldLoadPath).Read())
                    : options.ReplicationProofPath != null ? ReplicationFixture() : FireVerification.Fixture(8, 0);
                LoadedDigest = options.WorldLoadPath != null ? world.Checksum() : null;
                Authority = new AuthoritativeFireService(world);
                Frozen = options.ReplicationProofPath != null && world.Tick >= 120;
                nextSave = Time.realtimeSinceStartupAsDouble + 5;
                Debug.Log($"FIRELINE_WORLD_READY restored={LoadedDigest != null} tick={world.Tick} checksum={world.Checksum()}");
            }
            catch (Exception error)
            {
                Debug.LogError("FIRELINE_WORLD_LOAD_FAILED " + error.GetType().Name);
                Application.Quit(6); enabled = false; return;
            }
            Debug.Log($"FIRELINE_SIMULATION_START hz=5 grid={Authority.World.Size} cellM={Authority.World.CellSizeM} seed={Authority.World.Wind.Seed} authority=server fixture=synthetic");
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
                        // Write a number without locale-dependent commas and without losing double round-trip precision.
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

        /// <summary>
        /// Accumulate Unity fixed-callback seconds and advance fire in complete 0.2 s steps, retaining fractional time.
        /// Multiple catch-up steps can run in one callback; this adapter does not establish whole-host capacity.
        /// </summary>
        private void FixedUpdate()
        {
            if (Authority == null || Frozen || (FoundationBootstrap.Options.ReplicationProofPath != null && !ServerFireReplication.ProofMayAdvance)) return;
            accumulated += Time.fixedDeltaTime;
            while (accumulated + 1e-9 >= Authority.World.StepSeconds)
            {
                Authority.World.Step(); accumulated -= Authority.World.StepSeconds;
                if (FoundationBootstrap.Options.ReplicationProofPath != null)
                {
                    var world = Authority.World; var id = new Fireline.Simulation.ChunkId(1, 0, 0);
                    if (world.Tick == 20) { world.Wind.Set(0, 8, 2); Debug.Log("FIRELINE_PROOF_EVENT wind tick=20"); }
                    if (world.Tick == 40)
                    {
                        var hits = new System.Collections.Generic.List<Fireline.Simulation.WaterHit>();
                        for (int z = 5; z <= 11; z++) for (int x = 5; x <= 11; x++) hits.Add(new Fireline.Simulation.WaterHit(id, z * 16 + x, 1.0 / 49));
                        Authority.ApplyManualTestWater(14700, hits.ToArray()); Debug.Log("FIRELINE_PROOF_EVENT waterKg=14700 tick=40");
                    }
                    if (world.Tick == 60) { world.Ignite(id, 8 * 16 + 12); Debug.Log("FIRELINE_PROOF_EVENT ignition tick=60"); }
                    if (world.Tick >= 120) { Frozen = true; SaveCheckpoint(); Debug.Log($"FIRELINE_WORLD_FROZEN tick={world.Tick} checksum={world.Checksum()}"); break; }
                }
            }
        }

        /// <summary>Create a named two-chunk synthetic test: brush plus fast-burning low fuel for visible persistent char.</summary>
        private static Fireline.Simulation.FireWorld ReplicationFixture()
        {
            var world = new Fireline.Simulation.FireWorld(415, 16);
            world.AddChunk(new Fireline.Simulation.ChunkId(1, 0, 0), true);
            world.AddChunk(new Fireline.Simulation.ChunkId(1, -1, 0), true, 0.1);
            world.Ignite(new Fireline.Simulation.ChunkId(1, 0, 0), 8 * 16 + 8);
            world.Ignite(new Fireline.Simulation.ChunkId(1, -1, 0), 8 * 16 + 8);
            world.Wind.Set(8, 0, 2); return world;
        }

        /// <summary>Persist complete server-owned state every five wall seconds when an explicit destination was supplied.</summary>
        private void Update()
        {
            if (Authority != null && Time.realtimeSinceStartupAsDouble >= nextSave)
            { SaveCheckpoint(); nextSave = Time.realtimeSinceStartupAsDouble + 5; }
        }

        /// <summary>Flush a validated local checkpoint; a failed write is a visible fatal persistence error, never a fresh-world reset.</summary>
        public static void SaveCheckpoint()
        {
            if (Authority == null || FoundationBootstrap.Options.WorldSavePath == null) return;
            try
            {
                new FileFireSnapshotStore(FoundationBootstrap.Options.WorldSavePath).Write(Authority.World.CaptureSnapshot());
                Debug.Log($"FIRELINE_WORLD_SAVED tick={Authority.World.Tick} checksum={Authority.World.Checksum()}");
            }
            catch (Exception error) { Debug.LogError("FIRELINE_WORLD_SAVE_FAILED " + error.GetType().Name); Application.Quit(7); }
        }

        /// <summary>Capture the complete current state before a controlled application shutdown.</summary>
        private void OnApplicationQuit() => SaveCheckpoint();

        /// <summary>
        /// Record actual live fire tick/digest at shutdown and clear the process-wide service reference.
        /// </summary>
        private void OnDestroy()
        {
            if (Authority != null) Debug.Log($"FIRELINE_SIMULATION_STOP ticks={Authority.World.Tick} checksum={Authority.World.Checksum()}");
            Authority = null;
        }
    }
}
