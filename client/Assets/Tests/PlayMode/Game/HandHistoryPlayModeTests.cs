using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.HandHistory.Views;
using Poker.Client.Features.Table.Views;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Game
{
	public sealed class HandHistoryPlayModeTests
	{
		[UnityTest]
		[Timeout(240000)]
		public IEnumerator History_ListsAHandIFolded_WithMyCards() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			Button history = FindComponent<Button>(table, "History");
			history.onClick.Invoke();
			HandHistoryPopup empty = await WaitForAsync(() => Object.FindAnyObjectByType<HandHistoryPopup>(), "the history popup");
			Assert.IsTrue(FindComponent<TMP_Text>(empty, "Empty").gameObject.activeSelf, "a new player has no finished hands");
			FindComponent<Button>(empty, "Close").onClick.Invoke();
			await WaitForAsync(() => empty == null ? new object() : null, "the popup closed");

			Button fold = FindComponent<Button>(hand, "Fold");
			await WaitForAsync(() => fold.gameObject.activeInHierarchy ? fold : null, "my turn", 60f);
			string first = FindMyFaceUpCards(table)[0].Code;
			TMP_Text title = FindComponent<TMP_Text>(hand, "Title");
			string handName = title.text.Substring(0, title.text.IndexOf(','));
			fold.onClick.Invoke();
			await WaitForAsync(() => HasEnded(title.text, handName) ? title : null, "the hand ended", 30f);

			history.onClick.Invoke();
			HandHistoryPopup popup = await WaitForAsync(() => Object.FindAnyObjectByType<HandHistoryPopup>(), "the history popup again");
			TMP_Text row = await WaitForAsync(() => FirstShownRow(popup), "a hand in the list");
			StringAssert.StartsWith("Hand #", row.text);
			StringAssert.Contains("You: " + first, row.text, "my own cards are in the record");
			StringAssert.Contains("fold", row.text);
			FindComponent<Button>(popup, "Close").onClick.Invoke();

			await LeaveAsync(table, hand);
		});

		private static bool HasEnded(string title, string handName)
		{
			if (!title.StartsWith(handName + ",")) return true;

			return title.EndsWith("finished");
		}

		private static TMP_Text FirstShownRow(HandHistoryPopup popup)
		{
			foreach (TMP_Text text in popup.GetComponentsInChildren<TMP_Text>())
			{
				if (text.name != "Row(Clone)") continue;

				return text;
			}

			return null;
		}
	}
}