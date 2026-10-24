using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Fireline.Authoring
{
    /// <summary>Create a separate synthetic engineering runway scene without editing the static preview, model or original lighting assets.</summary>
    public static class FlightTestAuthoring
    {
        public const string ScenePath="Assets/Fireline/Scenes/M03_FlightTest.unity";
        /// <summary>Copy the actual preview, add a CoG anchor and metre-scale debug surfaces, and preserve prior loaded scenes.</summary>
        public static void CreateScene()
        {
            if(File.Exists(ScenePath) && File.ReadAllText(ScenePath)!=File.ReadAllText(AircraftPrototypeAuthoring.ScenePath))
                throw new InvalidOperationException("Flight test scene already exists; edit it in place.");
            if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("Save open scenes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                // An interrupted attempt may leave only the byte-identical copy; never reset an edited/generated scene.
                if(!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(AircraftPrototypeAuthoring.ScenePath,ScenePath)) throw new IOException("Could not copy preview scene.");
                var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
                string prefab=AircraftPrototypeAuthoring.Content+"/CL415Prototype.prefab";
                var model=scene.GetRootGameObjects().Single(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)==prefab);
                var pad=GameObject.Find("Static inspection pad - no runway or contact physics");
                if(pad==null) throw new InvalidOperationException("Expected original static pad.");
                UnityEngine.Object.DestroyImmediate(pad);
                var anchor=new GameObject("M03 Aircraft CoG Pose"); anchor.transform.position=new Vector3(0,2.05f,0);
                model.transform.SetParent(anchor.transform,false); model.transform.localPosition=new Vector3(0,-2,0);
                var runway=Material("FlightTestRunway",new Color(.07f,.085f,.10f));
                var surround=Material("FlightTestSurround",new Color(.12f,.19f,.15f));
                var marking=Material("FlightTestMarking",new Color(.8f,.82f,.77f));
                Surface("Synthetic surround - visual only",new Vector3(0,-.12f,1000),new Vector3(1000,.1f,4000),surround);
                Surface("Debug runway 45 x 3000 m - server rectangle y=0",new Vector3(0,-.05f,1000),new Vector3(45,.1f,3000),runway);
                for(int i=-4;i<=24;i++) Surface("Centreline "+i,new Vector3(0,.011f,i*100),new Vector3(.8f,.02f,30),marking);
                var camera=Camera.main; camera.transform.position=new Vector3(0,14,-44); camera.transform.LookAt(new Vector3(0,5,0));
                camera.fieldOfView=55; camera.farClipPlane=5000;
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("FIRELINE_FLIGHT_SCENE_CREATED runway=45x3000 synthetic=True");
            }
            finally
            {
                if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
        }

        /// <summary>Save a project-authored flat-colour HDRP material; no external texture or measured PBR surface is implied.</summary>
        private static Material Material(string name,Color color)
        {
            string path="Assets/Fireline/Content/Foundation/"+name+".mat";
            if(File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite "+path);
            var result=new Material(Shader.Find("HDRP/Lit")){name=name}; result.SetColor("_BaseColor",color); result.SetFloat("_Smoothness",.15f);
            HDMaterial.ValidateMaterial(result); AssetDatabase.CreateAsset(result,path); return result;
        }

        /// <summary>Create only a visible surface; the server's pure contact rectangle remains the sole collision authority.</summary>
        private static void Surface(string name,Vector3 position,Vector3 size,Material material)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube); item.name=name; item.transform.position=position; item.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>()); item.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
