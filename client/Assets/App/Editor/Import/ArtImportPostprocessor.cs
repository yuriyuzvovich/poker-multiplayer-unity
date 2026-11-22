using UnityEditor;
using UnityEngine;

namespace Poker.Client.App.Editor.Import
{
	public sealed class ArtImportPostprocessor : AssetPostprocessor
	{
		public const string Folder = "Assets/Content/Art";

		private void OnPreprocessTexture()
		{
			if (!assetPath.StartsWith(Folder + "/")) return;

			TextureImporter importer = (TextureImporter)assetImporter;
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.mipmapEnabled = false;
			importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.maxTextureSize = 2048;
			if (!assetPath.EndsWith(".jpg")) return;

			UseCoarseCompression(importer, "Android");
			UseCoarseCompression(importer, "iPhone");
		}

		private static void UseCoarseCompression(TextureImporter importer, string platform)
		{
			TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
			settings.overridden = true;
			settings.maxTextureSize = 2048;
			settings.format = TextureImporterFormat.ASTC_8x8;
			importer.SetPlatformTextureSettings(settings);
		}
	}
}