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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            new GameObject("M00 Server Probe").AddComponent<ServerProbe>();
        }
#endif

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

        private void FixedUpdate() => clock.Step();

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

        private void OnDestroy()
        {
            // ECS worlds own query storage and can be disposed before MonoBehaviours at exit.
            if (FoundationBootstrap.RuntimeWorld != null && FoundationBootstrap.RuntimeWorld.IsCreated)
                connections.Dispose();
        }
    }
}
