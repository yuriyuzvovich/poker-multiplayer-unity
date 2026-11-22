using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace Poker.Client.App.Editor.Builders
{
	public static class FontSetup
	{
		private const string Folder = "Assets/Content/Fonts";
		private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
		private const string Characters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u00a0\u00b7\u00d7\u2018\u2019\u201c\u201d\u2022\u2026\u2039\u203a\u20ac";
		private const int AtlasSize = 512;

		private static readonly int[] SamplingSizes = { 64, 60, 56, 52, 48, 44 };

		[MenuItem("mp-poker/Rebuild Fonts")]
		public static void Build()
		{
			TMP_FontAsset bold = CreateFontAsset("Montserrat-Bold");
			TMP_FontAsset regular = CreateFontAsset("Montserrat-SemiBold");
			TMP_FontWeightPair[] weights = regular.fontWeightTable;
			TMP_FontWeightPair boldWeight = weights[7];
			boldWeight.regularTypeface = bold;
			weights[7] = boldWeight;
			EditorUtility.SetDirty(regular);

			TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
			if (settings == null) throw new InvalidOperationException("No TextMesh Pro settings at " + SettingsPath + "; import the TMP Essential Resources first");

			SerializedObject serialized = new(settings);
			serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
			serialized.ApplyModifiedPropertiesWithoutUndo();
			AssetDatabase.SaveAssets();
			Debug.Log("[SETUP] Built the Montserrat font assets and made SemiBold the TextMesh Pro default, with Bold as its bold weight.");
		}

		private static TMP_FontAsset CreateFontAsset(string name)
		{
			string fontPath = Folder + "/" + name + ".ttf";
			Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
			if (font == null) throw new InvalidOperationException("No font at " + fontPath);

			string assetPath = Folder + "/" + name + " SDF.asset";
			AssetDatabase.DeleteAsset(assetPath);
			TMP_FontAsset asset = BakeLargestFitting(font, name);
			asset.atlasPopulationMode = AtlasPopulationMode.Static;
			asset.name = name + " SDF";
			AssetDatabase.CreateAsset(asset, assetPath);
			asset.atlasTexture.name = name + " Atlas";
			AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
			Material material = asset.material;
			material.name = name + " Material";
			material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
			material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
			material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.55f));
			AssetDatabase.AddObjectToAsset(material, asset);
			EditorUtility.SetDirty(asset);
			return asset;
		}

		private static TMP_FontAsset BakeLargestFitting(Font font, string name)
		{
			foreach (int size in SamplingSizes)
			{
				TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, size, size / 9, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
				if (asset.TryAddCharacters(Characters, out string _))
				{
					Debug.Log("[SETUP] " + name + " fits a " + AtlasSize + " atlas at " + size + " pt.");
					return asset;
				}

				Object.DestroyImmediate(asset.atlasTexture);
				Object.DestroyImmediate(asset.material);
				Object.DestroyImmediate(asset);
			}

			throw new InvalidOperationException(name + " does not fit a " + AtlasSize + " atlas at any sampling size");
		}
	}
}