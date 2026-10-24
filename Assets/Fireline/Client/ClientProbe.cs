using System.Collections;
using System.IO;
using Fireline.Shared;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fireline.Client
{
    public sealed class ClientProbe : MonoBehaviour
    {
        private EntityQuery connections;
        private double started;
        private bool connected;
        private bool captureStarted;
        private bool captureSaved;
        private int renderedFrames;
        private double lastRenderTime;
        private double firstRenderTime;
        private readonly FoundationMetrics metrics = new FoundationMetrics();

        /// <summary>Flush the existing rendered callback diagnostic when an external proof owns the process exit timer.</summary>
        public void SaveMetrics()
        {
            if (FoundationBootstrap.Options.MetricsPath != null && renderedFrames > 1) metrics.Save(FoundationBootstrap.Options.MetricsPath);
        }

        /// <summary>
        /// Attach the client-only startup probe after scene loading; assembly separation keeps it out of servers.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            new GameObject("M00 Client Probe").AddComponent<ClientProbe>();
        }

        /// <summary>
        /// Apply an explicitly requested existing preset, query network identity, and subscribe to render callbacks.
        /// Record startup time and renderer details for later connection/render smoke checks.
        /// </summary>
        private void Start()
        {
            if (FoundationBootstrap.Options.QualityPreset != null)
                QualitySettings.SetQualityLevel(System.Array.IndexOf(QualitySettings.names, FoundationBootstrap.Options.QualityPreset), true);
            DontDestroyOnLoad(gameObject);
            started = Time.realtimeSinceStartupAsDouble;
            connections = FoundationBootstrap.RuntimeWorld.EntityManager.CreateEntityQuery(typeof(NetworkId));
            RenderPipelineManager.endCameraRendering += OnCameraRendered;
            Debug.Log($"FIRELINE_CLIENT_RENDERER graphics={SystemInfo.graphicsDeviceType} pipeline={GraphicsSettings.currentRenderPipeline?.GetType().Name} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        }

        /// <summary>
        /// Record the first successful connection, request a capture after five rendered seconds, and enforce the smoke timer.
        /// Exit with failure unless connection, actual rendering and any requested screenshot all succeeded.
        /// </summary>
        private void Update()
        {
            if (!connected && !connections.IsEmptyIgnoreFilter)
            {
                connected = true;
                var id = connections.GetSingleton<NetworkId>().Value;
                Debug.Log($"FIRELINE_CLIENT_CONNECTED networkId={id}");
            }

            var options = FoundationBootstrap.Options;
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = firstRenderTime > 0 ? now - firstRenderTime : 0;
            if (!captureStarted && elapsed > (options.FlightViewProofPath != null ? 9 : 5) && options.CapturePath != null)
            {
                captureStarted = true;
                StartCoroutine(Capture(options.CapturePath));
            }
            if (options.SmokeSeconds > 0 && (elapsed >= options.SmokeSeconds || now - started >= options.SmokeSeconds + 60))
            {
                bool passed = connected && renderedFrames > 1
                    && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null
                    && GraphicsSettings.currentRenderPipeline != null
                    && (options.CapturePath == null || captureSaved);
                Debug.Log($"FIRELINE_CLIENT_SMOKE passed={passed} frames={Time.frameCount} renderedFrames={renderedFrames}");
                if (options.MetricsPath != null && renderedFrames > 1)
                    metrics.Save(options.MetricsPath);
                Application.Quit(passed ? 0 : 4);
                enabled = false;
            }
        }

        /// <summary>
        /// Measure spacing between main-camera callbacks in seconds after five seconds of rendered warm-up.
        /// Callback spacing includes waits; it is not pure CPU/GPU execution time or a dropped-presentation count.
        /// </summary>
        private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
        {
            if (camera != Camera.main) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (firstRenderTime == 0) firstRenderTime = now;
            renderedFrames++;
            if (now - firstRenderTime > 5 && lastRenderTime > 0 && FoundationBootstrap.Options.MetricsPath != null)
                metrics.Sample((float)(now - lastRenderTime));
            lastRenderTime = now;
        }

        /// <summary>
        /// Wait for the rendered frame, save its actual pixels as PNG, and release the temporary texture.
        /// Set the success flag only after the screenshot has been written.
        /// </summary>
        private IEnumerator Capture(string path)
        {
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image == null)
            {
                Debug.LogError("FIRELINE_CAPTURE_FAILED no rendered screenshot texture");
                yield break;
            }
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
            captureSaved = true;
            Debug.Log("FIRELINE_CAPTURE_SAVED " + Path.GetFileName(path));
        }

        /// <summary>
        /// Unsubscribe rendering and release the connection query only while its owning ECS world still exists.
        /// </summary>
        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnCameraRendered;
            if (FoundationBootstrap.RuntimeWorld != null && FoundationBootstrap.RuntimeWorld.IsCreated)
                connections.Dispose();
        }
    }
}
