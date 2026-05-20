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
            // Check if our scene already exists
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            if (System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
            {
                return; // Already set up
            }

            CreateRaceScene();
        }

        [MenuItem("Catnip Cart/Create Race Scene")]
        public static void CreateRaceScene()
        {
            // Create folders if needed
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart"))
                AssetDatabase.CreateFolder("Assets", "_CatnipCart");
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart/Scenes"))
                AssetDatabase.CreateFolder("Assets/_CatnipCart", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart/Materials"))
                AssetDatabase.CreateFolder("Assets/_CatnipCart", "Materials");

            // 1. Create a dummy Lit material with texture & emission to force variant compilation
            string dummyLitPath = "Assets/_CatnipCart/Materials/DummyLitRef.mat";
            Material dummyLit = AssetDatabase.LoadAssetAtPath<Material>(dummyLitPath);
            if (dummyLit == null)
            {
                dummyLit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                // Assign a dummy 2x2 texture to force texture sampling compilation
                Texture2D dummyTex = new Texture2D(2, 2);
                dummyTex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                dummyTex.Apply();
                
                // Add texture as a sub-asset or just assign it
                dummyLit.SetTexture("_BaseMap", dummyTex);
                dummyLit.EnableKeyword("_EMISSION");
                dummyLit.SetColor("_EmissionColor", Color.white);
                AssetDatabase.CreateAsset(dummyLit, dummyLitPath);
            }

            // 2. Create a dummy Skybox material to force Skybox/Panoramic compilation
            string dummySkyPath = "Assets/_CatnipCart/Materials/DummySkyRef.mat";
            Material dummySky = AssetDatabase.LoadAssetAtPath<Material>(dummySkyPath);
            if (dummySky == null)
            {
                dummySky = new Material(Shader.Find("Skybox/Panoramic"));
                Texture2D dummyTex = new Texture2D(2, 2);
                dummySky.SetTexture("_MainTex", dummyTex);
                AssetDatabase.CreateAsset(dummySky, dummySkyPath);
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create the master setup object
            var setupGO = new GameObject("_GameSetup");
            setupGO.AddComponent<Core.SceneSetup>();

            // Create floor and assign the dummy Lit material to ensure it is referenced in the scene
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TempFloor";
            floor.transform.localScale = new Vector3(50, 1, 50);
            floor.GetComponent<Renderer>().sharedMaterial = dummyLit;

            // Set skybox in RenderSettings to ensure the panoramic shader is referenced in the scene
            RenderSettings.skybox = dummySky;

            // Save scene
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("🏁 Catnip Cart race scene created with build-safe shaders! Press Play to race! 🐱");
        }
    }
}
