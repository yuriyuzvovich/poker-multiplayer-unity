using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Layout;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Features.Wallet.Views
{
	public static class WalletViewFactory
	{
		public static WalletView Create(Transform parent)
		{
			GameObject canvasObject = UiBuilder.CreateCanvas("WalletCanvas", parent, 40);
			RectTransform bar = UiBuilder.CreateRow(canvasObject.transform, "WalletBar", 56f);
			bar.anchorMin = new Vector2(1f, 1f);
			bar.anchorMax = new Vector2(1f, 1f);
			bar.pivot = new Vector2(1f, 1f);
			bar.sizeDelta = new Vector2(400f, 56f);
			bar.anchoredPosition = new Vector2(-16f, -42f);
			SafeAreaInset.Attach(bar, true, false);
			TMP_Text balance = UiBuilder.CreateText(bar, "Balance", 26, 56f, 230f);
			balance.alignment = TextAlignmentOptions.MidlineLeft;
			Button freeChips = UiBuilder.CreateButton(bar, "FreeChips", "Free chips", 158f);
			TMP_Text message = UiBuilder.CreateText(canvasObject.transform, "Message", 22, 40f, 600f);
			message.alignment = TextAlignmentOptions.MidlineRight;
			UiBuilder.Place(message.rectTransform, new Vector2(1f, 1f), new Vector2(600f, 40f), new Vector2(-316f, -125f));
			SafeAreaInset.Attach(message.rectTransform, true, false);

			WalletView view = canvasObject.AddComponent<WalletView>();
			view.Bind(balance, message, freeChips);
			return view;
		}
	}
}