using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Fireline.Client
{
    /// <summary>Diagnostic frame intervals for the calibration scene, with five-second warm-up.</summary>
    public sealed class FoundationMetrics
    {
        private readonly List<double> frameMilliseconds = new List<double>();
        private long peakAllocatedBytes;

        public void Sample(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            frameMilliseconds.Add(seconds * 1000.0);
            peakAllocatedBytes = Math.Max(peakAllocatedBytes, Profiler.GetTotalAllocatedMemoryLong());
        }

        public void Save(string path)
        {
            if (frameMilliseconds.Count == 0)
                throw new InvalidOperationException("No frame samples were collected.");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllLines(Path.ChangeExtension(path, ".csv"), new[] { "frame_interval_ms" }
                .Concat(frameMilliseconds.Select(x => x.ToString("R", CultureInfo.InvariantCulture))));
            var sorted = frameMilliseconds.OrderBy(x => x).ToArray();
            int slowFrames = Math.Max(1, (int)Math.Ceiling(sorted.Length * 0.01));
            var evidence = new MetricsEvidence
            {
                utc = DateTime.UtcNow.ToString("o"), cpu = SystemInfo.processorType,
                gpu = SystemInfo.graphicsDeviceName, driver = SystemInfo.graphicsDeviceVersion,
                os = SystemInfo.operatingSystem, ramMB = SystemInfo.systemMemorySize,
                width = Screen.width, height = Screen.height,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                samples = sorted.Length, sampledSeconds = sorted.Sum() / 1000.0,
                averageFps = 1000.0 / sorted.Average(),
                onePercentLowFps = 1000.0 / sorted.Skip(sorted.Length - slowFrames).Average(),
                p95FrameMs = sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1],
                p99FrameMs = sorted[(int)Math.Ceiling(sorted.Length * 0.99) - 1],
                peakUnityAllocatedBytes = peakAllocatedBytes,
                unavailable = "CPU/GPU execution timings, draw calls, VRAM, network bandwidth and server tick duration were not instrumented. Frame intervals include presentation/wait time."
            };
            File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
            Debug.Log("FIRELINE_METRICS_SAVED " + Path.GetFileName(path));
        }

        [Serializable]
        private sealed class MetricsEvidence
        {
            public string utc, cpu, gpu, driver, os, quality, unavailable;
            public int ramMB, width, height, samples;
            public double sampledSeconds, averageFps, onePercentLowFps, p95FrameMs, p99FrameMs;
            public long peakUnityAllocatedBytes;
        }
    }
}
