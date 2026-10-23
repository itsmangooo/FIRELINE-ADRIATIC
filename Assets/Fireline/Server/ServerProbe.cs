using Fireline.Shared;
using Fireline.Simulation;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fireline.Server
{
    /// <summary>M00 process/connection smoke probe, not gameplay authority validation.</summary>
    public sealed class ServerProbe : MonoBehaviour
    {
        private readonly TickClock clock = new TickClock(60);
        private EntityQuery connections;
        private int peakClients;
        private int previousClients = -1;
        private double started;

#if UNITY_SERVER
        /// <summary>
        /// Attach the connection/clock probe only in dedicated-server builds after the scene loads.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            new GameObject("M00 Server Probe").AddComponent<ServerProbe>();
        }
#endif

        /// <summary>
        /// Set a 60 Hz foundation clock, query connected identities, and verify no graphics device/pipeline/camera exists.
        /// Exit code 3 signals a failed headless boundary; this clock does not measure simulation execution cost.
        /// </summary>
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            Time.fixedDeltaTime = 1f / clock.TicksPerSecond;
            Application.targetFrameRate = 60;
            started = Time.realtimeSinceStartupAsDouble;
            connections = FoundationBootstrap.RuntimeWorld.EntityManager.CreateEntityQuery(typeof(NetworkId));
            bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                && GraphicsSettings.defaultRenderPipeline == null
                && QualitySettings.renderPipeline == null
                && Camera.allCamerasCount == 0;
            Debug.Log($"FIRELINE_SERVER_HEADLESS valid={headless} graphics={SystemInfo.graphicsDeviceType} cameras={Camera.allCamerasCount}");
            if (!headless)
                Application.Quit(3);
        }

        /// <summary>
        /// Advance the foundation clock once per Unity fixed callback; fire has a separate 5 Hz accumulator.
        /// </summary>
        private void FixedUpdate() => clock.Step();

        /// <summary>
        /// Track current/peak client identities and, at the smoke deadline, verify the requested peak and clock progress.
        /// This proves connections and process activity, not replicated fire state or multiplayer capacity.
        /// </summary>
        private void Update()
        {
            int count = connections.CalculateEntityCount();
            peakClients = Mathf.Max(peakClients, count);
            if (count != previousClients)
            {
                Debug.Log($"FIRELINE_SERVER_CONNECTIONS count={count}");
                previousClients = count;
            }

            var options = FoundationBootstrap.Options;
            if (options.SmokeSeconds > 0 && Time.realtimeSinceStartupAsDouble - started >= options.SmokeSeconds)
            {
                bool passed = peakClients >= options.MinimumClients && clock.Tick > 0;
                Debug.Log($"FIRELINE_SERVER_SMOKE passed={passed} peakClients={peakClients} ticks={clock.Tick}");
                Application.Quit(passed ? 0 : 4);
                enabled = false;
            }
        }

        /// <summary>
        /// Release the query only if its ECS owner survived shutdown ordering.
        /// </summary>
        private void OnDestroy()
        {
            // ECS worlds own query storage and can be disposed before MonoBehaviours at exit.
            if (FoundationBootstrap.RuntimeWorld != null && FoundationBootstrap.RuntimeWorld.IsCreated)
                connections.Dispose();
        }
    }
}
