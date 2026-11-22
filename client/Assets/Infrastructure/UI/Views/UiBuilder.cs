using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Infrastructure.UI.Views
{
	public static class UiBuilder
	{
		private const float PanelWidth = 560f;

		public static GameObject CreateCanvas(string name, Transform parent, int sortingOrder = 0)
		{
			GameObject canvasObject = new(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(parent, false);
			Canvas canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = sortingOrder;
			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0f;
			return canvasObject;
		}

		public static RectTransform CreatePanel(Transform parent, string name, float height)
		{
			GameObject panel = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
			panel.transform.SetParent(parent, false);
			RectTransform rect = panel.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(PanelWidth, height);
			VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
			layout.spacing = 12f;
			layout.childControlHeight = false;
			layout.childForceExpandHeight = false;
			return rect;
		}

		public static RectTransform CreateRow(Transform parent, string name, float height)
		{
			GameObject row = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
			row.transform.SetParent(parent, false);
			RectTransform rect = row.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(PanelWidth, height);
			HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
			layout.spacing = 12f;
			layout.childControlWidth = false;
			layout.childForceExpandWidth = false;
			layout.childAlignment = TextAnchor.MiddleLeft;
			return rect;
		}

		public static TextMeshProUGUI CreateText(Transform parent, string name, int fontSize, float height, float width = PanelWidth)
		{
			GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
			textObject.transform.SetParent(parent, false);
			textObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
			TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
			text.fontSize = fontSize;
			text.color = Color.white;
			text.alignment = TextAlignmentOptions.TopLeft;
			text.raycastTarget = false;
			return text;
		}

		public static Button CreateButton(Transform parent, string name, string label, float width = PanelWidth)
		{
			GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
			buttonObject.transform.SetParent(parent, false);
			buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 56f);
			buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.45f, 0.29f);

			TextMeshProUGUI labelText = CreateText(buttonObject.transform, "Label", 24, 56f, width);
			labelText.text = label;
			labelText.alignment = TextAlignmentOptions.Center;
			RectTransform labelRect = labelText.GetComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.sizeDelta = Vector2.zero;
			return buttonObject.GetComponent<Button>();
		}

		public static RectTransform CreateRect(Transform parent, string name)
		{
			GameObject rectObject = new(name, typeof(RectTransform));
			rectObject.transform.SetParent(parent, false);
			return rectObject.GetComponent<RectTransform>();
		}

		public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
		{
			GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
			imageObject.transform.SetParent(parent, false);
			Image image = imageObject.GetComponent<Image>();
			image.color = color;
			image.sprite = sprite;
			image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
			return image;
		}

		public static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
		{
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = size;
			rect.anchoredPosition = offset;
		}

		public static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(left, bottom);
			rect.offsetMax = new Vector2(-right, -top);
		}
	}
}