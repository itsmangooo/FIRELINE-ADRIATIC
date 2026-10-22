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
        public static LaunchOptions Options { get; private set; }
        public static World RuntimeWorld { get; private set; }

        public override bool Initialize(string defaultWorldName)
        {
            Options = LaunchOptions.Parse(Environment.GetCommandLineArgs());
            Application.runInBackground = true;
            // Explicit requests avoid the Editor play-mode endpoint overrides.
            AutoConnectPort = 0;
#if UNITY_SERVER
            RuntimeWorld = CreateServerWorld("Fireline.Server");
            var request = RuntimeWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
            RuntimeWorld.EntityManager.SetComponentData(request, new NetworkStreamRequestListen
            {
                Endpoint = NetworkEndpoint.Parse(Options.BindAddress, Options.Port)
            });
            Debug.Log($"FIRELINE_SERVER_START endpoint={Options.BindAddress}:{Options.Port}");
#else
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
