using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Poker.Client.App.Editor.Import
{
	public static class MobileTextureSettings
	{
		[MenuItem("mp-poker/Apply Mobile Texture Settings")]
		public static void Apply()
		{
			PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ASTC };
			AssetDatabase.SaveAssets();
			Debug.Log("[SETUP] Android textures now default to ASTC.");
		}
	}
}