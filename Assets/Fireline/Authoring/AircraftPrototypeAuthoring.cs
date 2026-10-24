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
    /// <summary>Reproducible static import inspection; this fixture installs no aircraft simulation.</summary>
    public static class AircraftPrototypeAuthoring
    {
        public const string ScenePath = "Assets/Fireline/Scenes/M03_AircraftPreview.unity";
        public const string Content = "Assets/Fireline/Content/Aircraft/CL415Prototype";
        private const string ModelPath = Content + "/Import/cl415-unity.obj";
        private const string Evidence = "docs/reports/m03-aircraft-evidence/import.json";

        /// <summary>
        /// Import the audited OBJ, remap every original material to HDRP, and save a replaceable prefab/inspection scene.
        /// Refuse existing generated outputs and unsaved scenes; preserve the user's original scenes and settings.
        /// </summary>
        public static void CreatePreview()
        {
            if (File.Exists(ScenePath) || File.Exists(Content + "/CL415Prototype.prefab"))
                throw new InvalidOperationException("Aircraft preview already exists; edit or remove its generated outputs explicitly.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scenes before generating the preview.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            GameObject root = null;
            try
            {
                // Closed-Editor batch runs can start with no loaded scene; instantiate into a temporary scene.
                if (!setup.Any(s => s.isLoaded))
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureTextures();
                var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
                if (importer == null) throw new InvalidDataException("Extract the audited original OBJ first.");
                importer.globalScale = 1;
                importer.useFileScale = false;
                importer.isReadable = false;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SaveAndReimport();
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var originals = original.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials).Distinct().ToArray();
                if (originals.Length != 11 || originals.Any(m => m == null))
                    throw new InvalidDataException("Expected the eleven original MTL materials.");
                Directory.CreateDirectory(Content + "/Materials");
                AssetDatabase.Refresh();
                foreach (var source in originals)
                {
                    var material = ConvertMaterial(source.name);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
                }
                importer.SaveAndReimport();
                original = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                root = new GameObject("CL415 prototype exterior - static, uncalibrated, replace before launch");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(original);
                model.transform.SetParent(root.transform, false);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                Directory.CreateDirectory(Path.GetDirectoryName(Evidence));
                File.WriteAllLines("docs/reports/m03-aircraft-evidence/imported-nodes.txt", renderers.Select(r => r.name));
                var prop = renderers.First(r => r.name.StartsWith("heliceG", StringComparison.Ordinal));
                var tail = renderers.Where(r => r.name.StartsWith("derive", StringComparison.Ordinal)).OrderBy(r => r.name.Length).First();
                var nose = prop.bounds.center - tail.bounds.center;
                nose.y = 0;
                // Source nose/tail geometry determines heading, including Unity's OBJ handedness conversion.
                // Ignore the propeller's lateral offset by using the model's longitudinal X axis only.
                model.transform.localRotation = Quaternion.FromToRotation(new Vector3(Mathf.Sign(nose.x), 0, 0), Vector3.forward);
                var bounds = BoundsOf(renderers);
                model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                bounds = BoundsOf(renderers);
                if (bounds.size.x < 28 || bounds.size.x > 30 || bounds.size.z < 19 || bounds.size.z > 21)
                    throw new InvalidDataException($"Unexpected imported dimensions {bounds.size}; no automatic real-aircraft scaling is allowed.");
                if (renderers.SelectMany(r => r.sharedMaterials).Any(m => m == null || m.shader.name != "HDRP/Lit"))
                    throw new InvalidDataException("Every imported material must resolve to HDRP/Lit.");
                PrefabUtility.SaveAsPrefabAsset(root, Content + "/CL415Prototype.prefab");
                WriteAudit(root, bounds, renderers);
                if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath))
                    throw new IOException("Could not copy starter lighting for inspection.");
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                UnityEngine.Object.DestroyImmediate(root);
                root = null;
                var aircraft = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(Content + "/CL415Prototype.prefab"), scene);
                aircraft.transform.position = Vector3.zero;
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Static inspection pad - no runway or contact physics";
                ground.transform.position = new Vector3(0, -0.25f, 0);
                ground.transform.localScale = new Vector3(100, 0.5f, 100);
                UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
                var pad = new Material(Shader.Find("HDRP/Lit")) { name = "Inspection pad" };
                pad.SetColor("_BaseColor", new Color(0.32f, 0.34f, 0.35f));
                pad.SetFloat("_Smoothness", 0.18f);
                HDMaterial.ValidateMaterial(pad);
                AssetDatabase.CreateAsset(pad, Content + "/Materials/InspectionPad.mat");
                ground.GetComponent<Renderer>().sharedMaterial = pad;
                var camera = Camera.main;
                if (camera == null) throw new InvalidOperationException("Starter scene has no main camera.");
                camera.transform.position = new Vector3(32, 16, 35);
                camera.transform.LookAt(new Vector3(0, 3, 0));
                camera.fieldOfView = 42;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 2000;
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("FIRELINE_AIRCRAFT_IMPORT_SUCCESS static HDRP preview; not flight gameplay");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        /// <summary>Import the nine original diffuse/instrument maps as capped sRGB mipmapped color textures.</summary>
        private static void ConfigureTextures()
        {
            foreach (string path in Directory.GetFiles(Content + "/Source", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// Map original diffuse textures/opacity to HDRP/Lit; roughness is a declared visual estimate, not measured PBR data.
        /// Preserve both original glass opacities and validate keywords, passes and blend state with HDRP 17.6's API.
        /// </summary>
        private static Material ConvertMaterial(string name)
        {
            string path = Content + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool create = material == null;
            if (create) material = new Material(Shader.Find("HDRP/Lit")) { name = name };
            bool glass = name == "transparent" || name == "transparent2";
            if (!glass && name != "DefaultWhite" && !name.StartsWith("DefaultWhite_", StringComparison.Ordinal))
                throw new InvalidDataException("Unexpected source material: " + name);
            float alpha = name == "transparent" ? 0.15f : name == "transparent2" ? 0.30f : 1;
            material.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.8f, alpha));
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", glass ? 0.8f : 0.35f);
            if (glass)
            {
                material.SetFloat("_SurfaceType", 1);
                material.SetFloat("_BlendMode", 0);
                material.SetFloat("_ZWrite", 0);
                material.SetFloat("_TransparentZWrite", 0);
                material.SetFloat("_EnableBlendModePreserveSpecularLighting", 0);
            }
            else
            {
                string filename = name == "DefaultWhite" ? "texture.png" : name.Substring("DefaultWhite_".Length);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Content + "/Source/" + filename);
                if (texture == null) throw new InvalidDataException("Missing diffuse texture: " + filename);
                material.SetTexture("_BaseColorMap", texture);
            }
            if (!HDMaterial.ValidateMaterial(material)) throw new InvalidOperationException("HDRP material validation failed.");
            if (create) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Combine world-space renderer bounds after import orientation and ground placement.</summary>
        private static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        /// <summary>Record actual Unity geometry, materials, transforms and texture dimensions without claiming a rig.</summary>
        private static void WriteAudit(GameObject root, Bounds bounds, Renderer[] renderers)
        {
            var meshes = root.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
            var textures = Directory.GetFiles(Content + "/Source", "*.png").Select(path =>
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/'));
                return $"{texture.name}: {texture.width}x{texture.height}";
            }).ToArray();
            var record = new ImportEvidence
            {
                unity = Application.unityVersion, sourceUnits = "Assumed meters for prototype; not validated engineering scale",
                boundsMeters = bounds.size, centerMeters = bounds.center,
                renderers = renderers.Length, meshes = meshes.Length,
                vertices = meshes.Sum(m => m.vertexCount),
                triangles = meshes.Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(i => (long)m.GetIndexCount(i) / 3)),
                submeshes = meshes.Sum(m => m.subMeshCount),
                materials = renderers.SelectMany(r => r.sharedMaterials).Select(m => m.name).Distinct().OrderBy(n => n).ToArray(),
                objects = renderers.Select(r => r.name).ToArray(), textures = textures,
                animators = root.GetComponentsInChildren<Animator>(true).Length,
                colliders = root.GetComponentsInChildren<Collider>(true).Length,
                orientationEuler = root.transform.GetChild(0).localEulerAngles,
                limitations = "Static exterior/instrument geometry; no verified pivots, rig, LOD, complete cockpit, collisions or flight integration. User authorized temporary prototype use on 2026-10-24; replace before launch. Original conflicting notices retained."
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Evidence));
            File.WriteAllText(Evidence, JsonUtility.ToJson(record, true));
        }

        [Serializable]
        private sealed class ImportEvidence
        {
            public string unity, sourceUnits, limitations;
            public Vector3 boundsMeters, centerMeters, orientationEuler;
            public int renderers, meshes, vertices, submeshes, animators, colliders;
            public long triangles;
            public string[] materials, objects, textures;
        }
    }
}
