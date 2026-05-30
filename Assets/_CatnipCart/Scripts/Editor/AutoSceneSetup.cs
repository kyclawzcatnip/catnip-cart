using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CatnipCart.Editor
{
    /// <summary>
    /// Editor script that automatically sets up the Catnip Cart scene
    /// when the project is first opened. Creates a scene with the
    /// SceneSetup component ready to go.
    /// </summary>
    [InitializeOnLoad]
    public class AutoSceneSetup
    {
        static AutoSceneSetup()
        {
            EditorApplication.delayCall += OnFirstLoad;
        }

        static void OnFirstLoad()
        {
            // Always ensure build materials exist (even if scene already exists)
            EnsureBuildMaterials();

            // Check if our scene already exists
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            if (System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
            {
                return; // Already set up
            }

            CreateRaceScene();
        }

        /// <summary>
        /// Creates dummy materials that reference URP Lit (with texture) and Skybox/Panoramic
        /// shaders. Without these, Unity's shader stripping removes the texture-sampling
        /// variants and all procedural textures render as flat solid colors in builds.
        /// </summary>
        public static void EnsureBuildMaterials()
        {
            // Create folders if needed
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart"))
                AssetDatabase.CreateFolder("Assets", "_CatnipCart");
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart/Materials"))
                AssetDatabase.CreateFolder("Assets/_CatnipCart", "Materials");

            // 1. Dummy Lit material with a SAVED texture to force texture sampling variant
            string dummyLitPath = "Assets/_CatnipCart/Materials/DummyLitRef.mat";
            Material dummyLit = AssetDatabase.LoadAssetAtPath<Material>(dummyLitPath);
            bool litNeedsUpdate = dummyLit == null || dummyLit.GetTexture("_BaseMap") == null;
            if (litNeedsUpdate)
            {
                if (dummyLit == null)
                {
                    dummyLit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(dummyLit, dummyLitPath);
                }

                // Create a 2x2 texture and save it as a sub-asset of the material.
                // This is critical — without saving, Unity loses the texture reference
                // on reimport and the shader variant gets stripped.
                Texture2D dummyTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                dummyTex.name = "DummyLitTex";
                dummyTex.SetPixels(new[] { Color.white, Color.red, Color.green, Color.blue });
                dummyTex.Apply();
                AssetDatabase.AddObjectToAsset(dummyTex, dummyLitPath);

                dummyLit.SetTexture("_BaseMap", dummyTex);
                dummyLit.EnableKeyword("_EMISSION");
                dummyLit.SetColor("_EmissionColor", Color.white);

                // Also set metallic/smoothness map to prevent those variants being stripped
                dummyLit.SetFloat("_Metallic", 0.5f);
                dummyLit.SetFloat("_Smoothness", 0.5f);

                EditorUtility.SetDirty(dummyLit);
                AssetDatabase.SaveAssets();
                Debug.Log("✅ Created DummyLitRef.mat with embedded texture for shader preservation");
            }

            // 2. Dummy Skybox material with a SAVED texture
            string dummySkyPath = "Assets/_CatnipCart/Materials/DummySkyRef.mat";
            Material dummySky = AssetDatabase.LoadAssetAtPath<Material>(dummySkyPath);
            bool skyNeedsUpdate = dummySky == null || dummySky.GetTexture("_MainTex") == null;
            if (skyNeedsUpdate)
            {
                if (dummySky == null)
                {
                    dummySky = new Material(Shader.Find("Skybox/Panoramic"));
                    AssetDatabase.CreateAsset(dummySky, dummySkyPath);
                }

                Texture2D skyTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                skyTex.name = "DummySkyTex";
                skyTex.SetPixels(new[] { Color.cyan, Color.cyan, Color.blue, Color.blue });
                skyTex.Apply();
                AssetDatabase.AddObjectToAsset(skyTex, dummySkyPath);

                dummySky.SetTexture("_MainTex", skyTex);
                EditorUtility.SetDirty(dummySky);
                AssetDatabase.SaveAssets();
                Debug.Log("✅ Created DummySkyRef.mat with embedded texture for shader preservation");
            }
        }

        [MenuItem("Catnip Cart/Create Race Scene")]
        public static void CreateRaceScene()
        {
            // Ensure materials exist first
            EnsureBuildMaterials();

            // Create scene folder
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart"))
                AssetDatabase.CreateFolder("Assets", "_CatnipCart");
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart/Scenes"))
                AssetDatabase.CreateFolder("Assets/_CatnipCart", "Scenes");

            // Load the dummy materials
            Material dummyLit = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CatnipCart/Materials/DummyLitRef.mat");
            Material dummySky = AssetDatabase.LoadAssetAtPath<Material>("Assets/_CatnipCart/Materials/DummySkyRef.mat");

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create the master setup object
            var setupGO = new GameObject("_GameSetup");
            setupGO.AddComponent<Core.SceneSetup>();

            // Create floor and assign the dummy Lit material to ensure it is referenced in the scene
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TempFloor";
            floor.transform.localScale = new Vector3(50, 1, 50);
            if (dummyLit != null)
                floor.GetComponent<Renderer>().sharedMaterial = dummyLit;

            // Set skybox in RenderSettings to ensure the panoramic shader is referenced in the scene
            if (dummySky != null)
                RenderSettings.skybox = dummySky;

            // Save scene
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("🏁 Catnip Cart race scene created with build-safe shaders! Press Play to race! 🐱");
        }
    }
}
