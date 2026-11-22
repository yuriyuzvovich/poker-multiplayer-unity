using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using Poker.Client.App.Editor.Import;

namespace Poker.Client.App.Editor.Build
{
	public static class AddressablesReport
	{
		private static string Output => Environment.GetEnvironmentVariable("POKER_REPORT_FILE") ?? "Library/AddressablesReport.txt";

		[MenuItem("mp-poker/Addressables Report")]
		public static void Build()
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetSettings.CleanPlayerContent();
			AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
			if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException("Addressables build failed: " + result.Error);

			StringBuilder report = new();
			report.Append("Target: ").Append(EditorUserBuildSettings.activeBuildTarget).AppendLine();
			report.AppendLine("Bundles (KB):");
			long total = 0;
			string[] bundles = Directory.GetFiles(Addressables.BuildPath, "*.bundle", SearchOption.AllDirectories);
			Array.Sort(bundles, StringComparer.Ordinal);
			foreach (string bundle in bundles)
			{
				long size = new FileInfo(bundle).Length;
				total += size;
				report.Append("  ").Append((size / 1024).ToString().PadLeft(6)).Append("  ").Append(Path.GetFileName(bundle)).AppendLine();
			}

			report.Append("  ").Append((total / 1024).ToString().PadLeft(6)).Append("  total in ").Append(bundles.Length).Append(" bundles").AppendLine();
			report.AppendLine("Assets duplicated across bundles:");
			List<AnalyzeRule.AnalyzeResult> duplicates = new CheckBundleDupeDependencies().RefreshAnalysis(settings);
			int count = 0;
			foreach (AnalyzeRule.AnalyzeResult duplicate in duplicates)
			{
				if (duplicate.resultName == "No issues found") continue;

				count++;
				report.Append("  ").Append(duplicate.resultName).AppendLine();
			}

			report.Append("  ").Append(count).Append(" entries").AppendLine();
			report.AppendLine("Standalone art textures on Android:");
			foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtImportPostprocessor.Folder }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (texture.width < 512 && texture.height < 512) continue;

				TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
				TextureImporterFormat format = android.overridden ? android.format : importer.GetAutomaticFormat("Android");
				report.Append("  ").Append(texture.width).Append("x").Append(texture.height).Append("  ").Append(format).Append("  ").Append(path).AppendLine();
			}

			AppendAtlases(report);
			AppendFonts(report);
			File.WriteAllText(Output, report.ToString());
			Debug.Log("[REPORT] Wrote " + Path.GetFullPath(Output) + "\n" + report);
		}

		private static void AppendAtlases(StringBuilder report)
		{
			SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
			report.AppendLine("Sprite atlases (page size, sprites, filled):");
			foreach (string guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
				Sprite[] sprites = new Sprite[atlas.spriteCount];
				atlas.GetSprites(sprites);
				float used = 0f;
				Texture2D page = null;
				foreach (Sprite sprite in sprites)
				{
					used += sprite.textureRect.width * sprite.textureRect.height;
					page = sprite.texture;
				}

				if (page == null) continue;

				float filled = used / (page.width * page.height);
				report.Append("  ").Append(page.width).Append("x").Append(page.height).Append("  ").Append(sprites.Length).Append(" sprites  ").Append((filled * 100f).ToString("F0")).Append("%  ").Append(path).AppendLine();
			}
		}

		private static void AppendFonts(StringBuilder report)
		{
			report.AppendLine("Font atlases:");
			foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
				Texture2D atlas = font.atlasTexture;
				if (atlas == null) continue;

				report.Append("  ").Append(atlas.width).Append("x").Append(atlas.height).Append("  ").Append(atlas.format).Append("  ").Append(font.characterTable.Count).Append(" glyphs  ").Append(font.faceInfo.pointSize).Append(" pt  ").Append(path).AppendLine();
			}
		}
	}
}