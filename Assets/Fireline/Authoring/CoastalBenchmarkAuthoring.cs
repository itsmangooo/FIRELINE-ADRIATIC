using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fireline.Authoring
{
    /// <summary>Initial render workload, not a playable coastal sector or fire model.</summary>
    public static class CoastalBenchmarkAuthoring
    {
        public const string ScenePath = "Assets/Fireline/Scenes/M00_CoastalBenchmark.unity";
        private const string Content = "Assets/Fireline/Content/Environment/M00/";
        private static readonly string[] Surfaces = {
            "rocky_terrain", "dry_ground_rocks", "forrest_ground_01", "coast_sand_01", "burned_ground_01"
        };

        public static void CreateScene()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Benchmark already exists; edit it explicitly.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scenes before creating the benchmark.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                AssetDatabase.Refresh();
                ConfigureTextures();
                if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath))
                    throw new IOException("Could not copy starter lighting into benchmark scene.");
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var data = new TerrainData { name = "Petrcane GLO90 DSM - 6km crop", heightmapResolution = 257,
                    size = new Vector3(6000, 128, 6000) };
                byte[] bytes = File.ReadAllBytes(Content + "petrcane_heights.bytes");
                if (bytes.Length != 257 * 257 * sizeof(float)) throw new InvalidDataException("Invalid heightfield length.");
                var heights = new float[257, 257];
                for (int z = 0; z < 257; z++)
                for (int x = 0; x < 257; x++)
                {
                    float meters = BitConverter.ToSingle(bytes, (z * 257 + x) * sizeof(float));
                    if (!float.IsFinite(meters) || meters < 0 || meters > 128)
                        throw new InvalidDataException("Height outside declared SI range.");
                    heights[z, x] = meters / 128;
                }
                data.SetHeights(0, 0, heights);
                var layers = new TerrainLayer[Surfaces.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    string folder = Content + Surfaces[i] + "/";
                    layers[i] = new TerrainLayer { name = Surfaces[i],
                        diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "albedo.jpg"),
                        normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "normal_dx.jpg"),
                        maskMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "terrain_mask.png"),
                        tileSize = new Vector2(i == 0 ? 90 : i == 3 ? 14 : 8, i == 0 ? 90 : i == 3 ? 14 : 8) };
                    AssetDatabase.CreateAsset(layers[i], folder + "terrain.terrainlayer");
                }
                data.terrainLayers = layers;
                data.alphamapResolution = 256;
                var alpha = new float[256, 256, layers.Length];
                for (int z = 0; z < 256; z++)
                for (int x = 0; x < 256; x++)
                {
                    float h = data.GetInterpolatedHeight(x / 255f, z / 255f);
                    int layer = h < 3 ? 3 : h > 45 ? 0 : (x + z) % 32 < 16 ? 1 : 2;
                    // Static char swatch exercises the imported material; it is not simulated fire.
                    if (h > 8 && x > 138 && x < 158 && z > 106 && z < 126) layer = 4;
                    alpha[z, x, layer] = 1;
                }
                data.SetAlphamaps(0, 0, alpha);
                AssetDatabase.CreateAsset(data, Content + "Petrcane.asset");
                var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                terrain.name = "Petrcane DSM - east X / north Z / meters";
                terrain.materialTemplate = Material("Terrain", "HDRP/TerrainLit", Color.white);
                terrain.drawInstanced = true;
                terrain.heightmapPixelError = 5;

                var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
                sea.name = "Static sea reference plane - no hydrodynamics";
                sea.transform.position = new Vector3(3000, 0.3f, 3000);
                sea.transform.localScale = new Vector3(1000, 1, 1000);
                var water = Material("SeaReference", "HDRP/Lit", new Color(0.025f, 0.25f, 0.31f));
                water.SetFloat("_Smoothness", 0.94f);
                sea.GetComponent<Renderer>().sharedMaterial = water;
                UnityEngine.Object.DestroyImmediate(sea.GetComponent<Collider>());

                var bark = Material("GeneratedTrunk", "HDRP/Lit", new Color(0.19f, 0.13f, 0.08f));
                var leaf = Material("GeneratedCanopy", "HDRP/Lit", new Color(0.11f, 0.22f, 0.075f));
                var building = Material("PopulationProxy", "HDRP/Lit", new Color(0.65f, 0.62f, 0.48f));
                var random = new System.Random(10415);
                var forest = new GameObject("640 generated canopy proxies - not production pine assets");
                int placed = 0;
                for (int attempt = 0; attempt < 20000 && placed < 640; attempt++)
                {
                    float x = 2000 + (float)random.NextDouble() * 3000;
                    float z = 1600 + (float)random.NextDouble() * 3000;
                    float h = data.GetInterpolatedHeight(x / 6000, z / 6000);
                    if (h < 4 || h > 60) continue;
                    float height = 7 + (float)random.NextDouble() * 5;
                    Primitive("Trunk " + placed, PrimitiveType.Cylinder, new Vector3(x, h + height / 2, z),
                        new Vector3(0.5f, height / 2, 0.5f), bark, forest.transform);
                    Primitive("Canopy " + placed, PrimitiveType.Sphere, new Vector3(x, h + height, z),
                        new Vector3(8, 4, 8), leaf, forest.transform);
                    placed++;
                }
                var population = new GameObject("24 static building proxies - no players or vehicles");
                for (int i = 0; i < 24; i++)
                {
                    float x = 3400 + i % 6 * 35, z = 2700 + i / 6 * 40;
                    float h = data.GetInterpolatedHeight(x / 6000, z / 6000);
                    Primitive("Building proxy " + i, PrimitiveType.Cube, new Vector3(x, h + 5, z),
                        new Vector3(16, 10, 24), building, population.transform);
                }
                var camera = Camera.main;
                camera.transform.SetPositionAndRotation(new Vector3(2680, 115, 2450), Quaternion.Euler(14, 55, 0));
                camera.farClipPlane = 12000;
                camera.fieldOfView = 60;
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log($"FIRELINE_COASTAL_CREATED trees={placed} buildings=24 source=CopernicusGLO90");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void ConfigureTextures()
        {
            foreach (string surface in Surfaces)
            foreach (string name in new[] { "albedo.jpg", "normal_dx.jpg", "terrain_mask.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Content + surface + "/" + name);
                if (importer == null) throw new FileNotFoundException("Missing audited texture: " + surface + "/" + name);
                importer.textureType = name == "normal_dx.jpg" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = name == "albedo.jpg";
                // Poly Haven supplies DirectX (-Y) tangent normals; Unity expects +Y.
                importer.flipGreenChannel = name == "normal_dx.jpg";
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static Material Material(string name, string shader, Color color)
        {
            var material = new Material(Shader.Find(shader)) { name = "M00 " + name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, Content + name + ".mat");
            return material;
        }

        private static void Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Material material, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(position, Quaternion.identity);
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        }
    }
}
