using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Layout;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Features.TurnTimer.Views
{
	public static class AwayViewFactory
	{
		public static AwayView Create(Transform parent)
		{
			GameObject canvasObject = UiBuilder.CreateCanvas("AwayCanvas", parent, 50);
			RectTransform banner = UiBuilder.CreateRow(canvasObject.transform, "Banner", 56f);
			banner.anchorMin = new Vector2(0.5f, 0f);
			banner.anchorMax = new Vector2(0.5f, 0f);
			banner.anchoredPosition = new Vector2(0f, 230f);
			SafeAreaInset.Attach(banner, false, false);
			TMP_Text message = UiBuilder.CreateText(banner, "Message", 22, 56f, 300f);
			message.text = "You are sitting out";
			message.alignment = TextAlignmentOptions.MidlineLeft;
			Button returnButton = UiBuilder.CreateButton(banner, "ReturnButton", "I am back", 220f);

			AwayView view = canvasObject.AddComponent<AwayView>();
			view.Bind(returnButton);
			return view;
		}
	}
}