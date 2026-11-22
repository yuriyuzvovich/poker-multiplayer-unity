using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Layout
{
	public static class SafeArea
	{
		public static float TopInset(Rect safeArea, Vector2 screenSize) => Mathf.Max(0f, screenSize.y - safeArea.yMax);

		public static float BottomInset(Rect safeArea) => Mathf.Max(0f, safeArea.yMin);
	}
}