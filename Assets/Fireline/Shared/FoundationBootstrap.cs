using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Fireline.Shared
{
    /// <summary>Creates exactly one role per process; no listen-host or cloud dependency.</summary>
    public sealed class FoundationBootstrap : ClientServerBootstrap
    {
        /// <summary>
        /// Validated startup settings shared by the process adapters after bootstrap initialization.
        /// </summary>
        public static LaunchOptions Options { get; private set; }
        /// <summary>
        /// The single server or client ECS world created for this process; consumers must check shutdown lifetime.
        /// </summary>
        public static World RuntimeWorld { get; private set; }

        /// <summary>
        /// Parse direct-IP options and create exactly one Netcode role according to the build target.
        /// Servers enqueue a listen request; clients connect only when an address was explicitly supplied.
        /// Disable automatic Editor endpoint overrides and reject server proof mode in client builds.
        /// </summary>
        public override bool Initialize(string defaultWorldName)
        {
            Options = LaunchOptions.Parse(Environment.GetCommandLineArgs());
            Application.runInBackground = true;
            // Explicit requests avoid the Editor play-mode endpoint overrides.
            AutoConnectPort = 0;
#if UNITY_SERVER
            if (Options.FlightProbeRole != null) throw new ArgumentException("Flight probe roles are client-only.");
            RuntimeWorld = CreateServerWorld("Fireline.Server");
            var request = RuntimeWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
            RuntimeWorld.EntityManager.SetComponentData(request, new NetworkStreamRequestListen
            {
                Endpoint = NetworkEndpoint.Parse(Options.BindAddress, Options.Port)
            });
            Debug.Log($"FIRELINE_SERVER_START endpoint={Options.BindAddress}:{Options.Port}");
#else
            if (Options.FlightNetworkProofPath != null && (Options.FlightProbeRole == null || Options.ConnectAddress == null))
                throw new ArgumentException("Client flight proof requires a role and explicit direct-IP connection.");
            if (Options.SimulationProofPath != null || Options.FlightProofPath != null || Options.RunwayProofPath != null || Options.WorldLoadPath != null || Options.WorldSavePath != null)
                throw new ArgumentException("Simulation proof is available only in a dedicated-server build.");
            RuntimeWorld = CreateClientWorld("Fireline.Client");
            if (Options.ConnectAddress != null)
            {
                var request = RuntimeWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestConnect));
                RuntimeWorld.EntityManager.SetComponentData(request, new NetworkStreamRequestConnect
                {
                    Endpoint = NetworkEndpoint.Parse(Options.ConnectAddress, Options.Port)
                });
            }
            Debug.Log("FIRELINE_CLIENT_START");
#endif
            return true;
        }
    }
}
