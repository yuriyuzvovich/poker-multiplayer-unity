using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.TurnTimer.Views;
using Poker.Client.Infrastructure.Network.Connection;
using Poker.Shared.Features.Hand.Turns;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Game
{
	public sealed class HandPlayModeTests
	{
		[UnityTest]
		[Timeout(240000)]
		public IEnumerator HeadsUp_ActAtTheLastVisibleSecond_ThenPlayUntilAHandEnds() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			CardView[] myCards = await WaitForAsync(() => FindMyFaceUpCards(table), "my hole cards face up", 20f);
			Assert.AreEqual(2, myCards.Length, "two hole cards on my line");

			TMP_Text status = FindComponent<TMP_Text>(hand, "Status");
			TMP_Text timer = FindComponent<TMP_Text>(hand, "Timer");
			Button checkOrCall = FindComponent<Button>(hand, "CheckOrCall");
			await WaitForAsync(() => checkOrCall.gameObject.activeInHierarchy ? checkOrCall : null, "my turn", 60f);
			await WaitForAsync(() => timer.text == "1" ? timer : null, "the last visible second", 20f);
			IObjectResolver container = Object.FindAnyObjectByType<LifetimeScope>().Container;
			int mySeat = container.Resolve<HandModel>().MySeat;
			TurnActedEvent myAct = null;
			using (container.Resolve<ServerConnection>().Subscribe<TurnActedEvent>(acted =>
			{
				if (acted.SeatIndex != mySeat) return;

				myAct ??= acted;
			}))
			{
				checkOrCall.onClick.Invoke();
				await WaitForAsync(() => myAct, "the server's answer to my click", 10f);
			}

			Assert.AreEqual(ActionReason.Player, myAct.Reason, "a click in the last visible second reaches the server before its deadline");
			StringAssert.DoesNotContain("refused", FindComponent<TMP_Text>(table, "Toast").text);

			int clicks = 1;
			await WaitForAsync(() =>
			{
				if (clicks > 0 && status.text.Contains(" wins ")) return status;
				if (!checkOrCall.gameObject.activeInHierarchy) return null;

				checkOrCall.onClick.Invoke();
				clicks++;
				return null;
			}, "a hand in which I acted to end", 90f);

			await LeaveAsync(table, hand);
		});

		[UnityTest]
		[Timeout(240000)]
		public IEnumerator HeadsUp_StandUpThenFold_LeavesAnEmptySeatThatIsNotFaded() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			Button checkOrCall = FindComponent<Button>(hand, "CheckOrCall");
			await WaitForAsync(() => checkOrCall.gameObject.activeInHierarchy ? checkOrCall : null, "my turn", 60f);

			SeatAction(table, 2).onClick.Invoke();
			FindComponent<Button>(hand, "Fold").onClick.Invoke();
			await WaitForAsync(() => SeatLabel(table, 2).Contains("empty") ? table : null, "my seat released when the hand ends", 60f);
			await UniTask.Delay(TimeSpan.FromSeconds(3), DelayType.Realtime);

			Assert.AreEqual(1f, SeatOpacity(table, 2), 0.001f, "an empty seat keeps no fold dimming from the last hand");
			Button sit = SeatAction(table, 2);
			Assert.IsTrue(sit.gameObject.activeInHierarchy && sit.interactable, "Sit is offered and clickable");

			await BackToLobbyAndCloseAsync(table, hand);
		});

		[UnityTest]
		[Timeout(240000)]
		public IEnumerator HeadsUp_TwoMissedTurns_SitMeOut_AndIAmBackReturnsMe() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			AwayView away = Object.FindAnyObjectByType<AwayView>(FindObjectsInactive.Include);
			Assert.IsFalse(away.gameObject.activeSelf);

			await WaitForAsync(() => away.gameObject.activeSelf ? away : null, "sitting out after two missed turns", 120f);
			StringAssert.Contains("away", SeatLabel(table, 2));

			FindComponent<Button>(away, "ReturnButton").onClick.Invoke();
			await WaitForAsync(() => !away.gameObject.activeSelf && !SeatLabel(table, 2).Contains("away") ? away : null, "back in the game", 10f);

			await LeaveAsync(table, hand);
		});
	}
}