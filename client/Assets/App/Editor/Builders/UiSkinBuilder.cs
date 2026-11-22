using System.IO;
using UnityEditor;
using UnityEngine;
using Poker.Client.App.Editor.Import;

namespace Poker.Client.App.Editor.Builders
{
	public static class UiSkinBuilder
	{
		public const string Folder = ArtImportPostprocessor.Folder + "/Skin";

		[MenuItem("mp-poker/Rebuild UI Skin")]
		public static void Build()
		{
			if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(ArtImportPostprocessor.Folder, "Skin");

			Write("Pill.png", RoundedRect(128, 128, 56f, true), 60);
			Write("Panel.png", RoundedRect(96, 96, 28f, false), 32);
			Write("Ring.png", Ring(256, 14f), 0);
			Write("Glow.png", Glow(128), 0);
			Write("Spark.png", Spark(64), 0);
			Write("ChipBody.png", Chip(96, false), 0);
			Write("ChipSpots.png", Chip(96, true), 0);
			Write("Frame.png", Frame(96, 26f, 6f), 34);
			Write("DashedRing.png", DashedRing(192, 6f, 18), 0);
			Debug.Log("[SETUP] Rebuilt the UI skin sprites in " + Folder + ".");
		}

		private static Color32[] RoundedRect(int width, int height, float radius, bool isShaded)
		{
			Color32[] pixels = new Color32[width * height];
			for (int y = 0; y < height; y++)
			{
				float shade = isShaded ? Mathf.Lerp(0.78f, 1f, (float)y / (height - 1)) : 1f;
				for (int x = 0; x < width; x++)
				{
					float dx = Mathf.Max(radius - (x + 0.5f), x + 0.5f - (width - radius), 0f);
					float dy = Mathf.Max(radius - (y + 0.5f), y + 0.5f - (height - radius), 0f);
					float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
					byte alpha = (byte)(Mathf.Clamp01(0.5f - distance) * 255f);
					byte value = (byte)(shade * 255f);
					pixels[y * width + x] = new Color32(value, value, value, alpha);
				}
			}

			return pixels;
		}

		private static Color32[] Ring(int size, float thickness)
		{
			Color32[] pixels = new Color32[size * size];
			float center = size / 2f;
			float radius = center - thickness / 2f - 1f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x + 0.5f - center;
					float dy = y + 0.5f - center;
					float distance = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius) - thickness / 2f;
					byte alpha = (byte)(Mathf.Clamp01(0.5f - distance) * 255f);
					pixels[y * size + x] = new Color32(255, 255, 255, alpha);
				}
			}

			return pixels;
		}

		private static Color32[] Glow(int size)
		{
			Color32[] pixels = new Color32[size * size];
			float center = size / 2f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = (x + 0.5f - center) / center;
					float dy = (y + 0.5f - center) / center;
					float ring = (Mathf.Sqrt(dx * dx + dy * dy) - 0.62f) / 0.2f;
					byte alpha = (byte)(Mathf.Exp(-ring * ring) * 255f);
					pixels[y * size + x] = new Color32(255, 255, 255, alpha);
				}
			}

			return pixels;
		}

		private static Color32[] Spark(int size)
		{
			Color32[] pixels = new Color32[size * size];
			float center = size / 2f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = Mathf.Abs(x + 0.5f - center) / center;
					float dy = Mathf.Abs(y + 0.5f - center) / center;
					float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.5f);
					float horizontal = Mathf.Clamp01(1f - dx) * Mathf.Clamp01(1f - dy * 9f);
					float vertical = Mathf.Clamp01(1f - dy) * Mathf.Clamp01(1f - dx * 9f);
					float value = Mathf.Clamp01(core + horizontal * horizontal + vertical * vertical);
					pixels[y * size + x] = new Color32(255, 255, 255, (byte)(value * 255f));
				}
			}

			return pixels;
		}

		private static Color32[] Chip(int size, bool isSpots)
		{
			Color32[] pixels = new Color32[size * size];
			float center = size / 2f;
			float radius = center - 1f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x + 0.5f - center;
					float dy = y + 0.5f - center;
					float distance = Mathf.Sqrt(dx * dx + dy * dy);
					float edge = distance / radius;
					float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f;
					float coverage = Mathf.Clamp01(radius + 0.5f - distance);
					if (isSpots)
					{
						bool isSpot = edge > 0.74f && edge < 0.95f && (int)(angle / 22.5f) % 2 == 0;
						pixels[y * size + x] = new Color32(255, 255, 255, isSpot ? (byte)(coverage * 255f) : (byte)0);
						continue;
					}

					float value = 0.95f;
					if (edge > 0.56f && edge < 0.64f) value = 0.62f;
					if (edge > 0.96f) value = 0.7f;

					byte shade = (byte)(value * 255f);
					pixels[y * size + x] = new Color32(shade, shade, shade, (byte)(coverage * 255f));
				}
			}

			return pixels;
		}

		private static Color32[] Frame(int size, float radius, float thickness)
		{
			Color32[] outer = RoundedRect(size, size, radius, false);
			float inner = radius - thickness;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = Mathf.Max(radius - (x + 0.5f), x + 0.5f - (size - radius), 0f);
					float dy = Mathf.Max(radius - (y + 0.5f), y + 0.5f - (size - radius), 0f);
					float distance = Mathf.Sqrt(dx * dx + dy * dy) - inner;
					float hole = Mathf.Clamp01(0.5f - distance);
					Color32 pixel = outer[y * size + x];
					pixel.a = (byte)(pixel.a * (1f - hole));
					outer[y * size + x] = pixel;
				}
			}

			return outer;
		}

		private static Color32[] DashedRing(int size, float thickness, int dashes)
		{
			Color32[] pixels = Ring(size, thickness);
			float center = size / 2f;
			float arm = size * 0.16f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x + 0.5f - center;
					float dy = y + 0.5f - center;
					float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f;
					bool isGap = (int)(angle / (360f / dashes / 2f)) % 2 == 1;
					Color32 pixel = pixels[y * size + x];
					if (isGap) pixel.a = 0;

					bool isPlus = (Mathf.Abs(dx) < thickness / 2f && Mathf.Abs(dy) < arm) || (Mathf.Abs(dy) < thickness / 2f && Mathf.Abs(dx) < arm);
					if (isPlus) pixel = new Color32(255, 255, 255, 255);

					pixels[y * size + x] = pixel;
				}
			}

			return pixels;
		}

		private static void Write(string fileName, Color32[] pixels, int border)
		{
			int size = (int)Mathf.Sqrt(pixels.Length);
			Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
			texture.SetPixels32(pixels);
			texture.Apply();
			string path = Folder + "/" + fileName;
			File.WriteAllBytes(path, texture.EncodeToPNG());
			Object.DestroyImmediate(texture);
			AssetDatabase.ImportAsset(path);

			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.spriteBorder = new Vector4(border, border, border, border);
			importer.SaveAndReimport();
		}
	}
}