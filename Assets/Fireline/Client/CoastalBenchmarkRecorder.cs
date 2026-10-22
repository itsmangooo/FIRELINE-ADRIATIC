using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fireline.Shared;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Fireline.Client
{
    /// <summary>Render-only benchmark adapter. Static fog is not authoritative gameplay smoke.</summary>
    public sealed class CoastalBenchmarkRecorder : MonoBehaviour
    {
        private readonly List<Sample> samples = new List<Sample>(20000);
        private readonly FrameTiming[] timing = new FrameTiming[1];
        private ProfilerRecorder draws;
        private double firstRender;
        private double previousRender;
        private bool finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name == "M00_CoastalBenchmark")
                new GameObject("M00 Coastal Benchmark Recorder").AddComponent<CoastalBenchmarkRecorder>();
        }

        private void Start()
        {
            var options = FoundationBootstrap.Options;
            if (options.BenchmarkScenario == "smoky") CreateStaticFogWorkload();
            // Ordinary scene inspection must not accumulate an unbounded diagnostic recording.
            if (options.BenchmarkSeconds == 0) { enabled = false; return; }
            draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            RenderPipelineManager.endCameraRendering += Rendered;
            Debug.Log($"FIRELINE_BENCHMARK_START scenario={options.BenchmarkScenario} seconds={options.BenchmarkSeconds} warmup=30");
        }

        private void Rendered(ScriptableRenderContext context, Camera camera)
        {
            if (camera != Camera.main || finished) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (firstRender == 0) firstRender = now;
            FrameTimingManager.CaptureFrameTimings();
            uint available = FrameTimingManager.GetLatestTimings(1, timing);
            double elapsed = now - firstRender;
            if (elapsed >= 30 && previousRender > 0)
            {
                var frame = available > 0 ? timing[0] : default;
                samples.Add(new Sample { end = elapsed - 30, interval = (now - previousRender) * 1000,
                    cpu = available > 0 && frame.cpuFrameTime > 0 ? frame.cpuFrameTime : double.NaN,
                    main = available > 0 && frame.cpuMainThreadFrameTime > 0 ? frame.cpuMainThreadFrameTime : double.NaN,
                    gpu = available > 0 && frame.gpuFrameTime > 0 ? frame.gpuFrameTime : double.NaN,
                    drawCalls = draws.Valid && draws.Count > 0 ? draws.LastValue : -1,
                    allocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() });
            }
            previousRender = now;
            var options = FoundationBootstrap.Options;
            if (options.BenchmarkSeconds > 0 && elapsed >= options.BenchmarkSeconds + 30)
            {
                finished = true;
                Save(options.BenchmarkOutput);
                Debug.Log($"FIRELINE_BENCHMARK_COMPLETE samples={samples.Count} renderedSeconds={elapsed:F3}");
                Application.Quit(samples.Count > 0 ? 0 : 4);
            }
        }

        private void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string Number(double value) => double.IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : "";
            using (var writer = new StreamWriter(Path.ChangeExtension(path, ".csv")))
            {
                writer.WriteLine("end_s,callback_interval_ms,cpu_frame_ms,cpu_main_ms,gpu_frame_ms,draw_calls,unity_allocated_bytes");
                foreach (var row in samples)
                    writer.WriteLine(string.Join(",", Number(row.end), Number(row.interval), Number(row.cpu), Number(row.main),
                        Number(row.gpu), row.drawCalls >= 0 ? row.drawCalls.ToString(CultureInfo.InvariantCulture) : "", row.allocated));
            }
            var sorted = samples.Select(s => s.interval).OrderBy(x => x).ToArray();
            int lowCount = Math.Max(1, (int)Math.Ceiling(sorted.Length * 0.01));
            File.WriteAllText(path, JsonUtility.ToJson(new Evidence { utc = DateTime.UtcNow.ToString("o"),
                scenario = FoundationBootstrap.Options.BenchmarkScenario, scene = SceneManager.GetActiveScene().name,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()], cpu = SystemInfo.processorType,
                gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceVersion,
                os = SystemInfo.operatingSystem, width = Screen.width, height = Screen.height, samples = sorted.Length,
                seconds = sorted.Sum() / 1000, averageFps = 1000 / sorted.Average(),
                onePercentLowFps = 1000 / sorted.Skip(sorted.Length - lowCount).Average(),
                p95FrameMs = sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1],
                p99FrameMs = sorted[(int)Math.Ceiling(sorted.Length * 0.99) - 1],
                counters = "CSV blank = unavailable; timing values may lag callbacks. CPU frame includes waits, not pure execution. Draw calls from latest profiler frame. No VRAM, server tick, GC or bandwidth attribution.",
                workload = "Copernicus 6km DSM, 640 generated tree proxies, 24 building proxies, static sea; smoky adds 8 local fog volumes. No gameplay entities or fire model."
            }, true));
        }

        private static void CreateStaticFogWorkload()
        {
            // Scene-local profile copy: the original starter/shared profile stays unchanged.
            foreach (var volume in FindObjectsByType<Volume>())
            {
                if (!volume.isGlobal) continue;
                var profile = volume.profile;
                if (!profile.TryGet<Fog>(out var fog)) fog = profile.Add<Fog>();
                fog.enabled.Override(true);
                fog.enableVolumetricFog.Override(true);
                // HDRP defaults to a 64 m volumetric depth; the fixture columns are ~700 m away.
                fog.depthExtent.Override(2500);
                fog.meanFreePath.Override(100000);
            }
            var cameraData = Camera.main.GetComponent<HDAdditionalCameraData>();
            cameraData.customRenderingSettings = true;
            foreach (var field in new[] { FrameSettingsField.AtmosphericScattering, FrameSettingsField.Volumetrics })
            {
                cameraData.renderingPathCustomFrameSettings.SetEnabled(field, true);
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            }
            for (int i = 0; i < 8; i++)
            {
                var cloud = new GameObject("Static smoke-overdraw proxy " + i).AddComponent<LocalVolumetricFog>();
                cloud.transform.position = new Vector3(3300 + i % 2 * 160, 35 + i / 2 * 35, 2850);
                cloud.parameters.albedo = new Color(0.12f, 0.12f, 0.12f);
                cloud.parameters.meanFreePath = 12;
                cloud.parameters.size = new Vector3(100 + i / 2 * 20, 70, 100 + i / 2 * 20);
                cloud.parameters.positiveFade = cloud.parameters.negativeFade = new Vector3(0.3f, 0.3f, 0.3f);
            }
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= Rendered;
            draws.Dispose();
        }

        private struct Sample
        {
            public double end, interval, cpu, main, gpu;
            public long drawCalls, allocated;
        }

        [Serializable]
        private sealed class Evidence
        {
            public string utc, scenario, scene, quality, cpu, gpu, graphicsApi, os, counters, workload;
            public int width, height, samples;
            public double seconds, averageFps, onePercentLowFps, p95FrameMs, p99FrameMs;
        }
    }
}
