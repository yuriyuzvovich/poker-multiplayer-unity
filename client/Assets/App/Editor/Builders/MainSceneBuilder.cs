using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Poker.Client.App.Editor.Builders
{
	public static class MainSceneBuilder
	{
		private const string ScenePath = "Assets/Scenes/Main.unity";

		[MenuItem("mp-poker/Rebuild Main Scene")]
		public static void Build()
		{
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			GameObject cameraObject = new("Main Camera", typeof(Camera));
			cameraObject.tag = "MainCamera";
			Camera camera = cameraObject.GetComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.06f, 0.22f, 0.14f);

			new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
			new GameObject("Root", typeof(RootLifetimeScope));

			if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

			EditorSceneManager.SaveScene(scene, ScenePath);
			EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
			Debug.Log("[SETUP] Saved " + ScenePath + " and set it as the only build scene.");
		}
	}
}