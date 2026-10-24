using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Fireline.Shared
{
    /// <summary>Explicit local/direct-IP startup. Unity's own arguments are left alone.</summary>
    public sealed class LaunchOptions
    {
        /// <summary>UDP port shared by the direct-IP server/client endpoint; defaults to 7979.</summary>
        public ushort Port { get; private set; } = 7979;
        /// <summary>Numeric IPv4 interface to listen on; loopback by default and wildcard only when explicitly requested.</summary>
        public string BindAddress { get; private set; } = "127.0.0.1";
        /// <summary>Explicit remote IPv4 address, or null to leave the client disconnected.</summary>
        public string ConnectAddress { get; private set; }
        /// <summary>Timed smoke-test seconds; zero leaves ordinary runtime running.</summary>
        public int SmokeSeconds { get; private set; }
        /// <summary>Peak connected identities needed for server smoke success; this is not a capacity qualification.</summary>
        public int MinimumClients { get; private set; } = 2;
        /// <summary>Optional PNG path for an actual rendered-frame capture.</summary>
        public string CapturePath { get; private set; }
        /// <summary>Optional JSON path for calibration callback metrics, with paired CSV.</summary>
        public string MetricsPath { get; private set; }
        /// <summary>Coastal recording seconds after warm-up; zero disables the timed benchmark.</summary>
        public int BenchmarkSeconds { get; private set; }
        /// <summary>Destination JSON path for the coastal benchmark summary and paired raw CSV.</summary>
        public string BenchmarkOutput { get; private set; }
        /// <summary>Named static clear/smoky workload; this does not select authoritative gameplay weather.</summary>
        public string BenchmarkScenario { get; private set; } = "clear";
        /// <summary>Optional existing Unity quality tier; no new preset is created.</summary>
        public string QualityPreset { get; private set; }
        /// <summary>Server-only JSON output path that enables accelerated M01 numerical acceptance instead of normal live operation.</summary>
        public string SimulationProofPath { get; private set; }

        /// <summary>Server-only M03 isolated debug-flight verification output; never enables playable aircraft or client authority.</summary>
        public string FlightProofPath { get; private set; }
        /// <summary>Server-only isolated runway-contact numerical proof; it does not enable live piloting or replication.</summary>
        public string RunwayProofPath { get; private set; }

        /// <summary>Optional role-specific M02 proof result JSON; enables the bounded scripted acceptance scenario.</summary>
        public string ReplicationProofPath { get; private set; }
        /// <summary>Explicit checkpoint to load; missing/corrupt input fails startup instead of creating a fresh world.</summary>
        public string WorldLoadPath { get; private set; }
        /// <summary>Optional server checkpoint destination, written every five seconds and on controlled shutdown.</summary>
        public string WorldSavePath { get; private set; }

        /// <summary>
        /// Read only --fireline-* options, leaving Unity arguments alone; return validated local/direct-IP defaults.
        /// Reject unknown/duplicate/missing options and incompatible benchmark/smoke timers before starting a role.
        /// </summary>
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
                    case "--fireline-runway-proof":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Runway proof path is empty.");
                        options.RunwayProofPath = value; break;
                    case "--fireline-flight-proof":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Flight proof path is empty.");
                        options.FlightProofPath = value; break;
                    case "--fireline-replication-proof":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Proof path is empty.");
                        options.ReplicationProofPath = value; break;
                    case "--fireline-world-load":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Load path is empty.");
                        options.WorldLoadPath = value; break;
                    case "--fireline-world-save":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Save path is empty.");
                        options.WorldSavePath = value; break;
                    case "--fireline-sim-proof":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Simulation proof output is empty.");
                        options.SimulationProofPath = value;
                        break;
                    case "--fireline-benchmark-seconds":
                        options.BenchmarkSeconds = ReadInteger(value, 300, 900);
                        break;
                    case "--fireline-benchmark-output":
                        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Benchmark output is empty.");
                        options.BenchmarkOutput = value;
                        break;
                    case "--fireline-benchmark-scenario":
                        if (value != "clear" && value != "smoky") throw new ArgumentException("Expected clear or smoky.");
                        options.BenchmarkScenario = value;
                        break;
                    case "--fireline-quality":
                        if (value != "Balanced" && value != "Performant" && value != "High Fidelity")
                            throw new ArgumentException("Unknown existing quality preset.");
                        options.QualityPreset = value;
                        break;
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
            if (options.BenchmarkSeconds > 0 && (options.SmokeSeconds > 0 || options.BenchmarkOutput == null))
                throw new ArgumentException("Benchmark requires output and cannot share the smoke exit timer.");
            if (options.FlightProofPath != null && (options.SimulationProofPath != null || options.ReplicationProofPath != null
                || options.RunwayProofPath != null || options.WorldLoadPath != null || options.WorldSavePath != null || options.SmokeSeconds > 0 || options.BenchmarkSeconds > 0))
                throw new ArgumentException("Flight proof requires an isolated server run.");
            if (options.RunwayProofPath != null && (options.SimulationProofPath != null || options.ReplicationProofPath != null
                || options.FlightProofPath != null || options.WorldLoadPath != null || options.WorldSavePath != null || options.SmokeSeconds > 0 || options.BenchmarkSeconds > 0))
                throw new ArgumentException("Runway proof requires an isolated server run.");
            if (options.SimulationProofPath != null && (options.ReplicationProofPath != null || options.WorldLoadPath != null || options.WorldSavePath != null))
                throw new ArgumentException("Accelerated M01 proof cannot share live checkpoint/replication modes.");
            return options;
        }

        /// <summary>
        /// Parse a culture-independent unsigned-looking integer and enforce the inclusive caller-supplied bounds.
        /// Throw on malformed or out-of-range input instead of falling back to a different port or duration.
        /// </summary>
        private static int ReadInteger(string value, int min, int max)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                || number < min || number > max)
                throw new ArgumentException($"Expected integer in [{min}, {max}]: {value}");
            return number;
        }

        /// <summary>
        /// Return a normalized numeric IPv4 address; hostnames and IPv6 are not supported by this baseline.
        /// Allow 0.0.0.0 only for server binding, since clients cannot connect to a wildcard.
        /// </summary>
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
