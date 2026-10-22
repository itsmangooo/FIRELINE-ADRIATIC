using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Fireline.Shared
{
    /// <summary>Explicit local/direct-IP startup. Unity's own arguments are left alone.</summary>
    public sealed class LaunchOptions
    {
        public ushort Port { get; private set; } = 7979;
        public string BindAddress { get; private set; } = "127.0.0.1";
        public string ConnectAddress { get; private set; }
        public int SmokeSeconds { get; private set; }
        public int MinimumClients { get; private set; } = 2;
        public string CapturePath { get; private set; }
        public string MetricsPath { get; private set; }

        public static LaunchOptions Parse(string[] args)
        {
            var options = new LaunchOptions();
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (!key.StartsWith("--fireline-", StringComparison.Ordinal))
                    continue;
                if (!seen.Add(key) || ++i >= args.Length)
                    throw new ArgumentException("Duplicate option or missing value: " + key);

                string value = args[i];
                switch (key)
                {
                    case "--fireline-port":
                        options.Port = (ushort)ReadInteger(value, 1, 65535);
                        break;
                    case "--fireline-bind":
                        options.BindAddress = ReadAddress(value, true);
                        break;
                    case "--fireline-connect":
                        options.ConnectAddress = ReadAddress(value, false);
                        break;
                    case "--fireline-smoke-seconds":
                        options.SmokeSeconds = ReadInteger(value, 1, 300);
                        break;
                    case "--fireline-min-clients":
                        options.MinimumClients = ReadInteger(value, 1, 100);
                        break;
                    case "--fireline-capture":
                        if (string.IsNullOrWhiteSpace(value))
                            throw new ArgumentException("Capture path is empty.");
                        options.CapturePath = value;
                        break;
                    case "--fireline-metrics":
                        if (string.IsNullOrWhiteSpace(value))
                            throw new ArgumentException("Metrics path is empty.");
                        options.MetricsPath = value;
                        break;
                    default:
                        throw new ArgumentException("Unknown FIRELINE option: " + key);
                }
            }
            return options;
        }

        private static int ReadInteger(string value, int min, int max)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                || number < min || number > max)
                throw new ArgumentException($"Expected integer in [{min}, {max}]: {value}");
            return number;
        }

        private static string ReadAddress(string value, bool allowWildcard)
        {
            if (!IPAddress.TryParse(value, out var address)
                || address.AddressFamily != AddressFamily.InterNetwork
                || (!allowWildcard && address.Equals(IPAddress.Any)))
                throw new ArgumentException("Expected numeric IPv4 address: " + value);
            return address.ToString();
        }
    }
}
