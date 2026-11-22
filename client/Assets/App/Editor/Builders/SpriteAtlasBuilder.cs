using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Poker.Client.App.Editor.Import;

namespace Poker.Client.App.Editor.Builders
{
	public static class SpriteAtlasBuilder
	{
		private const string Folder = "Assets/Content/Atlases";
		private const string GroupName = "Atlases";
		private const string Art = ArtImportPostprocessor.Folder + "/";

		private static readonly string[] TableSprites =
		{
			"Avatar1.png", "Avatar2.png", "Avatar3.png", "Avatar4.png", "Avatar5.png", "Avatar6.png",
			"AvatarFrame.png", "Chip.png", "DealerButton.png", "FeltEmblem.png",
		};

		private static readonly string[] CardSprites =
		{
			"CardFace.png", "CardBack.png", "SuitSpade.png", "SuitHeart.png", "SuitDiamond.png", "SuitClub.png", "ChipPile.png",
		};

		[MenuItem("mp-poker/Rebuild Sprite Atlases")]
		public static void Build()
		{
			EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
			if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Content", "Atlases");

			Save("Skin", new[] { AssetDatabase.LoadAssetAtPath<Object>(UiSkinBuilder.Folder) });
			Save("Table", Load(TableSprites));
			Save("Cards", Load(CardSprites));
			AssetDatabase.SaveAssets();
			Debug.Log("[SETUP] Rebuilt the sprite atlases in " + Folder + " and their Addressables entries in group " + GroupName + ".");
		}

		private static Object[] Load(string[] fileNames)
		{
			Object[] textures = new Object[fileNames.Length];
			for (int i = 0; i < fileNames.Length; i++)
			{
				textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + fileNames[i]);
				if (textures[i] == null) throw new System.InvalidOperationException("No texture at " + Art + fileNames[i]);
			}

			return textures;
		}

		private static void Save(string name, Object[] packables)
		{
			string path = Folder + "/" + name + ".spriteatlasv2";
			SpriteAtlasAsset atlas = new();
			atlas.Add(packables);
			SpriteAtlasAsset.Save(atlas, path);
			AssetDatabase.ImportAsset(path);

			SpriteAtlasImporter importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
			importer.includeInBuild = true;
			importer.packingSettings = new SpriteAtlasPackingSettings
			{
				enableRotation = false,
				enableTightPacking = false,
				padding = 4,
				blockOffset = 1,
			};
			importer.textureSettings = new SpriteAtlasTextureSettings
			{
				readable = false,
				generateMipMaps = false,
				sRGB = true,
				filterMode = FilterMode.Bilinear,
			};
			importer.SaveAndReimport();

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
			AddressableAssetGroup group = settings.FindGroup(GroupName);
			if (group == null) group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
			AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, false, false);
			entry.address = "atlas/" + name.ToLowerInvariant();
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
		}
	}
}