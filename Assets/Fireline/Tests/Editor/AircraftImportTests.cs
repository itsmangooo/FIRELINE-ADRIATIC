using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fireline.Tests
{
    /// <summary>Guard the actual imported prototype contract independently of flight-model estimates.</summary>
    public sealed class AircraftImportTests
    {
        private const string Content = "Assets/Fireline/Content/Aircraft/CL415Prototype";

        /// <summary>Require all eleven HDRP materials, nine diffuse maps and the two original glass opacity values.</summary>
        [Test]
        public void MaterialsRetainTexturesAndGlassOpacity()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Content + "/CL415Prototype.prefab");
            Assert.That(prefab, Is.Not.Null);
            var materials = prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            Assert.That(materials.Length, Is.EqualTo(11));
            foreach (var material in materials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(material.shader.name, Is.EqualTo("HDRP/Lit"));
                if (material.name.StartsWith("transparent"))
                {
                    Assert.That(material.GetFloat("_SurfaceType"), Is.EqualTo(1));
                    Assert.That(material.GetColor("_BaseColor").a,
                        Is.EqualTo(material.name == "transparent" ? 0.15f : 0.30f).Within(1e-6));
                    Assert.That(material.renderQueue, Is.GreaterThanOrEqualTo(3000));
                }
                else Assert.That(material.GetTexture("_BaseColorMap"), Is.Not.Null);
            }
            Assert.That(materials.Select(m => m.GetTexture("_BaseColorMap")).Where(t => t != null).Distinct().Count(), Is.EqualTo(9));
        }

        /// <summary>Guard prototype dimensions and static scope without claiming these are certified aircraft dimensions.</summary>
        [Test]
        public void GeometryIsOrientedGroundedAndExplicitlyStatic()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Content + "/CL415Prototype.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.size.x, Is.InRange(28f, 30f));
                Assert.That(bounds.size.z, Is.InRange(19f, 21f));
                Assert.That(bounds.min.y, Is.EqualTo(0).Within(1e-4));
                Assert.That(instance.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(instance.GetComponentsInChildren<Animator>(true), Is.Empty);
                Assert.That(instance.GetComponentsInChildren<LODGroup>(true), Is.Empty);
                var prop = renderers.First(r => r.name.StartsWith("heliceG"));
                var tail = renderers.Where(r => r.name.StartsWith("derive")).OrderBy(r => r.name.Length).First();
                Assert.That(prop.bounds.center.z, Is.GreaterThan(tail.bounds.center.z));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        /// <summary>Keep the contributor notice text intact, allowing Git's Windows line endings in the audit copy.</summary>
        [Test]
        public void OriginalContributorNoticeIsUnchanged()
        {
            Assert.That(File.ReadAllText(Content + "/Source/Read-Me.txt").Replace("\r\n", "\n"),
                Is.EqualTo(File.ReadAllText("AssetSources/Aircraft/CL415/Read-Me.upstream.txt").Replace("\r\n", "\n")));
        }
    }
}
