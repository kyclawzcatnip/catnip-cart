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
        static void CreateRaceScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create the master setup object
            var setupGO = new GameObject("_GameSetup");
            setupGO.AddComponent<Core.SceneSetup>();

            // Create floor for initial grounding (will be replaced by track)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TempFloor";
            floor.transform.localScale = new Vector3(50, 1, 50);
            floor.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floor.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 0.15f);

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart"))
                AssetDatabase.CreateFolder("Assets", "_CatnipCart");
            if (!AssetDatabase.IsValidFolder("Assets/_CatnipCart/Scenes"))
                AssetDatabase.CreateFolder("Assets/_CatnipCart", "Scenes");

            // Save scene
            string scenePath = "Assets/_CatnipCart/Scenes/CatnipGardens.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("🏁 Catnip Cart race scene created! Press Play to race! 🐱");
        }
    }
}
