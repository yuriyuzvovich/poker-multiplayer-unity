using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Views
{
	public sealed class UiLayers
	{
		private UiLayers(Transform screens, Transform popups)
		{
			Screens = screens;
			Popups = popups;
		}

		public Transform Screens { get; }
		public Transform Popups { get; }

		public static UiLayers Create(Transform parent)
		{
			Transform screens = UiBuilder.CreateCanvas("ScreenLayer", parent, 10).transform;
			Transform popups = UiBuilder.CreateCanvas("PopupLayer", parent, 100).transform;
			return new UiLayers(screens, popups);
		}
	}
}