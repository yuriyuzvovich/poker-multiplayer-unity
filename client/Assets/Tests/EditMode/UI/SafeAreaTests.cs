using NUnit.Framework;
using UnityEngine;
using Poker.Client.Infrastructure.UI.Layout;

namespace Poker.Client.Tests.EditMode.UI
{
	public sealed class SafeAreaTests
	{
		[Test]
		public void Insets_AreTheScreenStripsOutsideTheSafeArea()
		{
			Rect safeArea = new(0f, 102f, 1170f, 2532f - 102f - 141f);
			Vector2 screen = new(1170f, 2532f);

			Assert.AreEqual(141f, SafeArea.TopInset(safeArea, screen), 0.001f, "the notch strip at the top");
			Assert.AreEqual(102f, SafeArea.BottomInset(safeArea), 0.001f, "the home indicator strip at the bottom");
		}

		[Test]
		public void Insets_AreZero_OnAScreenWithoutCutouts()
		{
			Rect safeArea = new(0f, 0f, 1080f, 1920f);

			Assert.AreEqual(0f, SafeArea.TopInset(safeArea, new Vector2(1080f, 1920f)), 0.001f);
			Assert.AreEqual(0f, SafeArea.BottomInset(safeArea), 0.001f);
		}
	}
}