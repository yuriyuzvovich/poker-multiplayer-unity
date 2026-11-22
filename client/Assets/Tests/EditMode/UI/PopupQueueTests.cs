using NUnit.Framework;
using UnityEngine;
using Poker.Client.Infrastructure.UI.Popups;

namespace Poker.Client.Tests.EditMode.UI
{
	public sealed class PopupQueueTests
	{
		private GameObject _layer;

		[SetUp]
		public void SetUp() => _layer = new GameObject("PopupLayer");

		[TearDown]
		public void TearDown() => Object.DestroyImmediate(_layer);

		[Test]
		public void SecondPopup_WaitsHidden_UntilTheFirstCloses()
		{
			PopupQueue queue = new();
			GameObject first = CreatePopup("First");
			GameObject second = CreatePopup("Second");

			queue.Open(first, PopupPriority.Normal);
			queue.Open(second, PopupPriority.Normal);

			Assert.IsTrue(first.activeSelf);
			Assert.IsFalse(second.activeSelf, "one popup at a time");

			queue.Close(first);

			Assert.IsTrue(second.activeSelf, "the next one opens when the first closes");
		}

		[Test]
		public void CriticalPopup_OpensAtOnceOnTop_AndTheQueueWaitsForBoth()
		{
			PopupQueue queue = new();
			GameObject history = CreatePopup("History");
			GameObject buyIn = CreatePopup("BuyIn");
			GameObject notice = CreatePopup("Notice");

			queue.Open(history, PopupPriority.Normal);
			queue.Open(buyIn, PopupPriority.Normal);
			queue.Open(notice, PopupPriority.Critical);

			Assert.IsTrue(notice.activeSelf, "a critical popup does not wait");
			Assert.AreEqual(notice.transform, _layer.transform.GetChild(_layer.transform.childCount - 1), "it is drawn on top");
			Assert.IsTrue(history.activeSelf);
			Assert.IsFalse(buyIn.activeSelf);

			queue.Close(notice);
			Assert.IsFalse(buyIn.activeSelf, "the history popup is still open");

			queue.Close(history);
			Assert.IsTrue(buyIn.activeSelf);
		}

		[Test]
		public void WaitingPopup_ClosedBeforeItsTurn_NeverOpens()
		{
			PopupQueue queue = new();
			GameObject first = CreatePopup("First");
			GameObject cancelled = CreatePopup("Cancelled");
			GameObject third = CreatePopup("Third");
			queue.Open(first, PopupPriority.Normal);
			queue.Open(cancelled, PopupPriority.Normal);
			queue.Open(third, PopupPriority.Normal);

			queue.Close(cancelled);
			queue.Close(first);

			Assert.IsFalse(cancelled.activeSelf);
			Assert.IsTrue(third.activeSelf);
		}

		private GameObject CreatePopup(string name)
		{
			GameObject popup = new(name);
			popup.transform.SetParent(_layer.transform);
			return popup;
		}
	}
}