using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Fireline.Authoring
{
    public static class FoundationBuild
    {
        public const string ClientScene = "Assets/Fireline/Scenes/M00_Client.unity";
        public const string ServerScene = "Assets/Fireline/Scenes/M00_Server.unity";
        /// <summary>
        /// Directory for build reports; an environment override keeps milestone evidence separate.
        /// </summary>
        private static string Evidence => Environment.GetEnvironmentVariable("FIRELINE_BUILD_EVIDENCE") ?? "docs/reports/m00-evidence";

        /// <summary>
        /// Create only missing foundation scenes and calibration assets, preserving the starter scene.
        /// Refuse unsaved scene edits and restore the previous open-scene setup even if generation fails.
        /// </summary>
        [MenuItem("FIRELINE/M00/Create missing foundation scenes")]
        public static void CreateScenes()
        {
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save your open scenes before generating foundation scenes.");

            Directory.CreateDirectory("Assets/Fireline/Scenes");
            Directory.CreateDirectory("Assets/Fireline/Content/Foundation");
            AssetDatabase.Refresh();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                if (!File.Exists(ServerScene))
                {
                    var server = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(server, ServerScene);
                }
                if (!File.Exists(ClientScene))
                {
                    // Preserve the user's starter scene and HDRP lighting; extend only the copy.
                    if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ClientScene))
                        throw new InvalidOperationException("Could not copy starter scene.");
                    var client = EditorSceneManager.OpenScene(ClientScene, OpenSceneMode.Single);
                    var camera = Camera.main;
                    camera.transform.SetPositionAndRotation(new Vector3(12, 9, -17), Quaternion.Euler(24, -34, 0));
                    camera.fieldOfView = 55;

                    var material = new Material(Shader.Find("HDRP/Lit")) { name = "M00 Neutral Calibration" };
                    material.SetColor("_BaseColor", new Color(0.24f, 0.32f, 0.35f));
                    material.SetFloat("_Smoothness", 0.25f);
                    AssetDatabase.CreateAsset(material, "Assets/Fireline/Content/Foundation/Calibration.mat");
                    var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ground.name = "M00 calibration ground - not Adriatic terrain";
                    ground.transform.position = new Vector3(0, -0.25f, 0);
                    ground.transform.localScale = new Vector3(24, 0.5f, 24);
                    ground.GetComponent<Renderer>().sharedMaterial = material;
                    // Small deterministic render workload; not a populated gameplay benchmark.
                    for (int row = 0; row < 5; row++)
                    for (int column = 0; column < 5; column++)
                    {
                        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        marker.name = $"Calibration {row}-{column}";
                        float height = 1 + (row + column) % 3;
                        marker.transform.position = new Vector3(column * 3 - 6, height / 2, row * 3 - 6);
                        marker.transform.localScale = new Vector3(1, height, 1);
                        marker.GetComponent<Renderer>().sharedMaterial = material;
                    }
                    EditorSceneManager.SaveScene(client);
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        /// <summary>
        /// Build the Windows rendered calibration client and write its asset inventory/result report.
        /// </summary>
        public static void BuildClient() => Build(false);
        /// <summary>
        /// Build the Windows headless server with an empty scene and temporarily remove render pipelines.
        /// </summary>
        public static void BuildServer() => Build(true);
        /// <summary>
        /// Build the Windows coastal render fixture with timing counters and bundled asset notices.
        /// </summary>
        public static void BuildBenchmarkClient() => Build(false, benchmark: true);
        /// <summary>Build the isolated static aircraft inspection client with its original asset notices.</summary>
        public static void BuildAircraftPreviewClient() => Build(false, aircraftPreview: true);
        /// <summary>
        /// Cross-build the Linux x64 headless server; running that player is a separate verification step.
        /// </summary>
        public static void BuildLinuxServer() => Build(true, linux: true);

        /// <summary>
        /// Build the requested role and fail on build errors or detected server presentation assets.
        /// Temporarily select pipeline/backend/quality options, write evidence, and restore settings in finally.
        /// The inventory check covers named presentation assets, not every possible graphics resource.
        /// </summary>
        private static void Build(bool server, bool benchmark = false, bool linux = false, bool aircraftPreview = false)
        {
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Wait until the Editor is idle before building.");

            string role = aircraftPreview ? "aircraft-preview-client" : linux ? "linux-server" : benchmark ? "benchmark-client" : server ? "server" : "client";
            Directory.CreateDirectory(Evidence);
            string resultPath = $"{Evidence}/{role}-build.json";
            string scene = aircraftPreview ? AircraftPrototypeAuthoring.ScenePath : benchmark ? CoastalBenchmarkAuthoring.ScenePath : server ? ServerScene : ClientScene;
            if (!File.Exists(scene))
                throw new BuildFailedException("Run FoundationBuild.CreateScenes first.");

            // Netcode 6.6 removes this generated folder with FileUtil after a build.
            // On a second build the AssetDatabase can still consider the absent folder
            // valid, so its preprocessor skips creation. Ensure the physical folder too.
            const string netcodeTemp = "Assets/netcode-build-assets-temp";
            Directory.CreateDirectory(netcodeTemp);
            AssetDatabase.ImportAsset(netcodeTemp, ImportAssetOptions.ForceSynchronousImport);

            // BuildPipeline can unload assets after their settings references are removed.
            // Persist paths rather than managed Object wrappers that may become invalid.
            string defaultPipeline = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            var qualityPipelines = Enumerable.Range(0, QualitySettings.names.Length)
                .Select(i => AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(i))).ToArray();
            int quality = QualitySettings.GetQualityLevel();
            var oldSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            var oldBackend = PlayerSettings.GetScriptingBackend(server ? NamedBuildTarget.Server : NamedBuildTarget.Standalone);
            bool oldFrameTiming = PlayerSettings.enableFrameTimingStats;
            try
            {
                if (server)
                {
                    // No HDRP scene or pipeline asset may enter the headless player.
                    GraphicsSettings.defaultRenderPipeline = null;
                    for (int i = 0; i < qualityPipelines.Length; i++)
                        SetQualityPipeline(i, null);
                }
                else
                {
                    QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names, "Balanced"), false);
                }
                PlayerSettings.SetScriptingBackend(server ? NamedBuildTarget.Server : NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                if (benchmark || aircraftPreview) PlayerSettings.enableFrameTimingStats = true;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scene },
                    locationPathName = aircraftPreview ? "Builds/WindowsAircraftPreview/FirelineAircraftPreview.exe" : linux ? "Builds/LinuxServer/FirelineServer.x86_64" : benchmark ? "Builds/WindowsBenchmark/FirelineBenchmark.exe" : server ? "Builds/WindowsServer/FirelineServer.exe" : "Builds/WindowsClient/FirelineClient.exe",
                    target = linux ? BuildTarget.StandaloneLinux64 : BuildTarget.StandaloneWindows64,
                    subtarget = (int)(server ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player),
                    options = BuildOptions.DetailedBuildReport | BuildOptions.StrictMode | (server ? BuildOptions.CleanBuildCache : BuildOptions.None),
                    extraScriptingDefines = server ? Array.Empty<string>() : new[] { "UNITY_CLIENT" }
                };
                var report = BuildPipeline.BuildPlayer(options);
                var assetPaths = report.packedAssets.SelectMany(p => p.contents).Select(p => p.sourceAssetPath)
                    .Where(p => !string.IsNullOrEmpty(p)).Distinct().OrderBy(p => p).ToArray();
                File.WriteAllLines($"{Evidence}/{role}-packed-assets.txt", assetPaths);
                // Detailed reports also list source scripts compiled into assemblies. Those
                // are not serialized render assets and must not cause a false isolation failure.
                bool visualAssets = assetPaths.Where(p => !p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    .Any(p => p.Contains("high-definition") || p.StartsWith("Assets/Settings/") || p.EndsWith(".mat")
                        || p.StartsWith("Assets/Fireline/Content/Aircraft/"));
                var result = new BuildEvidence
                {
                    role = role, unity = Application.unityVersion, result = report.summary.result.ToString(),
                    errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                    seconds = report.summary.totalTime.TotalSeconds, bytes = report.summary.totalSize,
                    development = (report.summary.options & BuildOptions.Development) != 0,
                    subtarget = options.subtarget, containsVisualAssets = visualAssets
                };
                File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
                if (report.summary.result != BuildResult.Succeeded || (server && visualAssets))
                    throw new BuildFailedException("M00 build or server asset isolation failed: " + resultPath);
                if (benchmark)
                {
                    string legal = Path.Combine(Path.GetDirectoryName(options.locationPathName), "Legal");
                    Directory.CreateDirectory(legal);
                    File.Copy("THIRD_PARTY_ASSETS.md", Path.Combine(legal, "THIRD_PARTY_ASSETS.md"), true);
                    File.Copy("AssetSources/Environment/Copernicus/cop_dem_licenses_2025.pdf", Path.Combine(legal, "cop_dem_licenses_2025.pdf"), true);
                }
                if (aircraftPreview)
                {
                    string legal = Path.Combine(Path.GetDirectoryName(options.locationPathName), "Legal");
                    Directory.CreateDirectory(legal);
                    File.Copy("THIRD_PARTY_ASSETS.md", Path.Combine(legal, "THIRD_PARTY_ASSETS.md"), true);
                    File.Copy("AssetSources/Aircraft/CL415/Read-Me.upstream.txt", Path.Combine(legal, "CL415-Read-Me.upstream.txt"), true);
                    File.Copy("AssetSources/Aircraft/CL415/AUDIT.md", Path.Combine(legal, "CL415-AUDIT.md"), true);
                }
                string failurePath = $"{Evidence}/{role}-build-failure.txt";
                if (File.Exists(failurePath)) File.Delete(failurePath);
                Debug.Log($"FIRELINE_BUILD_SUCCESS role={role}");
            }
            catch (Exception exception)
            {
                File.WriteAllText($"{Evidence}/{role}-build-failure.txt", exception.ToString());
                throw;
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = LoadPipeline(defaultPipeline);
                for (int i = 0; i < qualityPipelines.Length; i++)
                    SetQualityPipeline(i, LoadPipeline(qualityPipelines[i]));
                QualitySettings.SetQualityLevel(quality, false);
                EditorUserBuildSettings.standaloneBuildSubtarget = oldSubtarget;
                PlayerSettings.SetScriptingBackend(server ? NamedBuildTarget.Server : NamedBuildTarget.Standalone, oldBackend);
                PlayerSettings.enableFrameTimingStats = oldFrameTiming;
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Reload a saved pipeline asset path after a build may have unloaded the original Unity object.
        /// An empty path means no pipeline; a missing nonempty path fails rather than silently changing settings.
        /// </summary>
        private static RenderPipelineAsset LoadPipeline(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            if (pipeline == null) throw new InvalidOperationException("Cannot restore pipeline: " + path);
            return pipeline;
        }

        /// <summary>
        /// Select a quality tier without applying expensive changes, then assign its render pipeline.
        /// </summary>
        private static void SetQualityPipeline(int index, RenderPipelineAsset pipeline)
        {
            QualitySettings.SetQualityLevel(index, false);
            QualitySettings.renderPipeline = pipeline;
        }

        [Serializable]
        private sealed class BuildEvidence
        {
            public string role;
            public string unity;
            public string result;
            public int errors;
            public int warnings;
            public double seconds;
            public ulong bytes;
            public bool development;
            public int subtarget;
            public bool containsVisualAssets;
        }
    }
}
