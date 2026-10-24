using System.Linq;
using Fireline.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fireline.Tests
{
    /// <summary>Scene geometry and explicit visual-frame contract, independent of network controls or rendering success.</summary>
    public sealed class FlightSceneTests
    {
        /// <summary>The visible debug runway matches the pure server rectangle in metres, with no competing Unity colliders.</summary>
        [Test]
        public void SyntheticSceneMatchesServerRectangleAndExplicitCoGOffset()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Fireline/Scenes/M03_FlightTest.unity",OpenSceneMode.Additive);
                var roots=scene.GetRootGameObjects();
                var anchor=roots.Single(g=>g.name=="M03 Aircraft CoG Pose");
                Assert.That(anchor.transform.childCount,Is.EqualTo(1));
                Assert.That(anchor.transform.GetChild(0).localPosition,Is.EqualTo(new Vector3(0,-2,0)));
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(anchor.transform.GetChild(0).gameObject),Does.EndWith("CL415Prototype.prefab"));
                var runway=roots.Single(g=>g.name.StartsWith("Debug runway"));
                var bounds=runway.GetComponent<Renderer>().bounds; var plane=RunwayPlane.Debug;
                Assert.That(bounds.min.x,Is.EqualTo(-plane.HalfWidthM).Within(1e-5));
                Assert.That(bounds.max.x,Is.EqualTo(plane.HalfWidthM).Within(1e-5));
                Assert.That(bounds.min.z,Is.EqualTo(plane.StartZM).Within(1e-5)); Assert.That(bounds.max.z,Is.EqualTo(plane.EndZM).Within(1e-5));
                Assert.That(bounds.max.y,Is.EqualTo(plane.HeightM).Within(1e-5));
                Assert.That(roots.SelectMany(g=>g.GetComponentsInChildren<Collider>(true)),Is.Empty);
            }
            finally
            {
                if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }
    }
}
