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
        [Test]
        public void DefaultsKeepServerLocalAndClientDisconnected()
        {
            var options = LaunchOptions.Parse(Array.Empty<string>());
            Assert.That(options.BindAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(options.ConnectAddress, Is.Null);
            Assert.That(options.Port, Is.EqualTo(7979));
            Assert.That(options.SmokeSeconds, Is.Zero);
        }

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

        [Test]
        public void MissingAndDuplicateValuesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-port" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] {
                "--fireline-port", "8000", "--fireline-port", "9000" }));
        }

        [Test]
        public void ClockUsesFixedStepSecondsAndMonotonicTicks()
        {
            var clock = new TickClock(60);
            for (int i = 0; i < 120; i++) clock.Step();
            Assert.That(clock.Tick, Is.EqualTo(120UL));
            Assert.That(clock.ElapsedSeconds, Is.EqualTo(2.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickClock(0));
        }

        [Test]
        public void SimulationAssemblyHasNoUnityOrPresentationDependencies()
        {
            foreach (var reference in typeof(TickClock).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name, Does.Not.StartWith("Unity").And.Not.StartWith("Fireline.Client"));
        }

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
