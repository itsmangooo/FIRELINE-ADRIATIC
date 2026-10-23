using System;
using System.IO;
using Fireline.Shared;
using Fireline.Simulation;
using NUnit.Framework;
using UnityEditor;

namespace Fireline.Tests
{
    public class FoundationTests
    {
        /// <summary>
        /// Verify omitted options bind only to loopback and leave the client disconnected with no timed auto-exit.
        /// </summary>
        [Test]
        public void DefaultsKeepServerLocalAndClientDisconnected()
        {
            var options = LaunchOptions.Parse(Array.Empty<string>());
            Assert.That(options.BindAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(options.ConnectAddress, Is.Null);
            Assert.That(options.Port, Is.EqualTo(7979));
            Assert.That(options.SmokeSeconds, Is.Zero);
        }

        /// <summary>
        /// Verify explicit direct-IP/smoke options are parsed while Unity-specific arguments are ignored.
        /// </summary>
        [Test]
        public void ExplicitEndpointAndSmokeOptionsIgnoreUnityArguments()
        {
            var options = LaunchOptions.Parse(new[] { "-batchmode", "--fireline-port", "9010",
                "--fireline-connect", "192.168.1.20", "--fireline-bind", "0.0.0.0",
                "--fireline-smoke-seconds", "15", "--fireline-min-clients", "2" });
            Assert.That(options.Port, Is.EqualTo(9010));
            Assert.That(options.ConnectAddress, Is.EqualTo("192.168.1.20"));
            Assert.That(options.BindAddress, Is.EqualTo("0.0.0.0"));
            Assert.That(options.SmokeSeconds, Is.EqualTo(15));
        }

        /// <summary>
        /// For each malformed/out-of-range option, require an exception instead of accepting unsafe fallback settings.
        /// </summary>
        [TestCase("--fireline-port", "0")]
        [TestCase("--fireline-port", "65536")]
        [TestCase("--fireline-port", "-1")]
        [TestCase("--fireline-connect", "0.0.0.0")]
        [TestCase("--fireline-connect", "not-an-ip")]
        [TestCase("--fireline-smoke-seconds", "0")]
        [TestCase("--fireline-smoke-seconds", "301")]
        [TestCase("--fireline-min-clients", "0")]
        [TestCase("--fireline-typo", "1")]
        public void InvalidArgumentsFailClosed(string key, string value)
        {
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { key, value }));
        }

        /// <summary>
        /// Require missing values and repeated options to fail before process startup.
        /// </summary>
        [Test]
        public void MissingAndDuplicateValuesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-port" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] {
                "--fireline-port", "8000", "--fireline-port", "9000" }));
        }

        /// <summary>
        /// Verify the five-minute benchmark timer and existing preset selection, including conflicts with smoke mode.
        /// </summary>
        [Test]
        public void BenchmarkUsesSeparateFiveMinuteTimerAndExistingPresets()
        {
            var options = LaunchOptions.Parse(new[] { "--fireline-benchmark-seconds", "300",
                "--fireline-benchmark-output", "run.json", "--fireline-benchmark-scenario", "smoky",
                "--fireline-quality", "Balanced" });
            Assert.That(options.BenchmarkSeconds, Is.EqualTo(300));
            Assert.That(options.BenchmarkScenario, Is.EqualTo("smoky"));
            Assert.That(options.QualityPreset, Is.EqualTo("Balanced"));
            Assert.That(options.SmokeSeconds, Is.Zero);
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-benchmark-seconds", "300" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-benchmark-seconds", "300",
                "--fireline-benchmark-output", "run.json", "--fireline-smoke-seconds", "20" }));
        }

        /// <summary>
        /// Require unsupported benchmark durations, scenario names and invented presets to be rejected.
        /// </summary>
        [TestCase("--fireline-benchmark-seconds", "299")]
        [TestCase("--fireline-benchmark-seconds", "901")]
        [TestCase("--fireline-benchmark-scenario", "unknown")]
        [TestCase("--fireline-quality", "invented-preset")]
        public void InvalidBenchmarkConfigurationIsRejected(string key, string value)
        {
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { key, value }));
        }

        /// <summary>
        /// Verify 120 steps at 60 Hz mean two simulated seconds and a zero-rate clock is rejected.
        /// </summary>
        [Test]
        public void ClockUsesFixedStepSecondsAndMonotonicTicks()
        {
            var clock = new TickClock(60);
            for (int i = 0; i < 120; i++) clock.Step();
            Assert.That(clock.Tick, Is.EqualTo(120UL));
            Assert.That(clock.ElapsedSeconds, Is.EqualTo(2.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickClock(0));
        }

        /// <summary>
        /// Inspect assembly references so pure simulation cannot silently acquire Unity or client dependencies.
        /// </summary>
        [Test]
        public void SimulationAssemblyHasNoUnityOrPresentationDependencies()
        {
            foreach (var reference in typeof(TickClock).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name, Does.Not.StartWith("Unity").And.Not.StartWith("Fireline.Client"));
        }

        /// <summary>
        /// Check the dedicated scene exists and its recursive asset dependencies exclude HDRP settings/materials.
        /// </summary>
        [Test]
        public void ServerSceneHasNoVisualAssetDependencies()
        {
            const string scene = "Assets/Fireline/Scenes/M00_Server.unity";
            Assert.That(File.Exists(scene), Is.True);
            foreach (string path in AssetDatabase.GetDependencies(scene, true))
            {
                Assert.That(path, Does.Not.Contain("high-definition").And.Not.Contain("Assets/Settings"));
                Assert.That(Path.GetExtension(path), Is.Not.EqualTo(".mat"));
            }
        }
    }
}
