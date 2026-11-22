using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Layout;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public static class DevPanelViewFactory
	{
		private const float PanelHeight = 560f;

		public static DevPanelView Create(Transform parent)
		{
			GameObject canvasObject = UiBuilder.CreateCanvas("DevCanvas", parent, 200);
			Button toggle = UiBuilder.CreateButton(canvasObject.transform, "DevToggle", "Dev", 80f);
			PinToTopLeft(toggle.GetComponent<RectTransform>(), new Vector2(12f, -150f));
			SafeAreaInset.Attach(toggle.GetComponent<RectTransform>(), true, false);

			RectTransform panel = UiBuilder.CreatePanel(canvasObject.transform, "DevPanel", PanelHeight);
			PinToTopLeft(panel, new Vector2(12f, -220f));
			SafeAreaInset.Attach(panel, true, false);
			TMP_Text info = UiBuilder.CreateText(panel, "Info", 16, 210f);
			Button bugReport = UiBuilder.CreateButton(panel, "BugReport", "Write bug report");
			RectTransform drops = UiBuilder.CreateRow(panel, "Drops", 56f);
			Button shortDrop = UiBuilder.CreateButton(drops, "ShortDrop", "Drop 3 s", 270f);
			Button longDrop = UiBuilder.CreateButton(drops, "LongDrop", "Drop 40 s", 270f);
			RectTransform actions = UiBuilder.CreateRect(panel, "Actions");
			actions.sizeDelta = new Vector2(560f, 190f);
			GridLayoutGroup grid = actions.gameObject.AddComponent<GridLayoutGroup>();
			grid.cellSize = new Vector2(180f, 56f);
			grid.spacing = new Vector2(10f, 10f);
			panel.gameObject.SetActive(false);

			DevPanelView view = canvasObject.AddComponent<DevPanelView>();
			view.Bind(toggle, panel.gameObject, info, bugReport, shortDrop, longDrop, actions);
			return view;
		}

		private static void PinToTopLeft(RectTransform rect, Vector2 offset)
		{
			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(0f, 1f);
			rect.pivot = new Vector2(0f, 1f);
			rect.anchoredPosition = offset;
		}
	}
}