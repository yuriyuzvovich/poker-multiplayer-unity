using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Seating.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Infrastructure.UI.Views;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Connection
{
	public sealed class WalkingSkeletonTests
	{
		[UnityTest]
		public IEnumerator Lobby_Join_SitBuyIn_StandUp_CancelledReservation_BackToLobby() => UniTask.ToCoroutine(async () =>
		{
			await StartAppAsNewPlayerAsync();
			LobbyView lobby = await WaitForAsync(() => Object.FindAnyObjectByType<LobbyView>(), "lobby view");
			ViewLoader views = Object.FindAnyObjectByType<LifetimeScope>().Container.Resolve<ViewLoader>();
			Assert.AreEqual(1, views.LoadedCount, "only the lobby is loaded at start");

			Button practice = await WaitForAsync(() => FindComponentOrNull<Button>(lobby, "Table_practice"), "practice table listed (is the server running on localhost:5080?)");
			Assert.IsNotNull(FindComponentOrNull<Button>(lobby, "Table_heads-up"), "heads-up table listed");

			practice.onClick.Invoke();
			TableView table = await WaitForAsync(() => Object.FindAnyObjectByType<TableView>(), "table screen loaded");
			await WaitForAsync(() => SeatLabel(table, 1).Contains("Ada") ? table : null, "practice seats shown");
			await WaitForAsync(() => lobby == null ? new object() : null, "lobby released while at the table");
			Assert.AreEqual("Sit", ActionLabel(table, 2));

			SeatAction(table, 2).onClick.Invoke();
			BuyInPopup popup = await WaitForAsync(() => Object.FindAnyObjectByType<BuyInPopup>(), "buy-in popup");
			FindComponent<Button>(popup, "Min").onClick.Invoke();
			await WaitForAsync(() => SeatLabel(table, 2).Contains("YOU") && SeatLabel(table, 2).Contains("400") ? table : null, "seated with 400");
			Assert.AreEqual("Stand up", ActionLabel(table, 2));
			Assert.IsFalse(SeatAction(table, 4).gameObject.activeSelf, "no Sit offered while seated");

			SeatAction(table, 2).onClick.Invoke();
			await WaitForAsync(() => SeatLabel(table, 2).Contains("empty") ? table : null, "stood up (after the hand if I was in one)", 60f);
			Assert.AreEqual("Sit", ActionLabel(table, 4));

			SeatAction(table, 2).onClick.Invoke();
			popup = await WaitForAsync(() => Object.FindAnyObjectByType<BuyInPopup>(), "second buy-in popup");
			await WaitForAsync(() => SeatLabel(table, 2).Contains("reserved") ? table : null, "seat shown as reserved");
			FindComponent<Button>(popup, "Cancel").onClick.Invoke();
			await WaitForAsync(() => SeatLabel(table, 2).Contains("empty") && Object.FindAnyObjectByType<BuyInPopup>() == null ? table : null, "reservation released on cancel");

			FindComponent<Button>(table, "BackButton").onClick.Invoke();
			await WaitForAsync(() => table == null ? new object() : null, "table screen released after leaving");
			await WaitForAsync(() => Object.FindAnyObjectByType<LobbyView>(), "lobby loaded again");
			Assert.AreEqual(1, views.LoadedCount, "after leaving only the lobby is loaded: table screen, hand layer and popups were released");

			Object.Destroy(GameObject.Find("Root"));
			await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime);
		});

		private static string ActionLabel(TableView table, int seatNumber)
		{
			Button action = SeatAction(table, seatNumber);
			return action.gameObject.activeSelf ? action.GetComponentInChildren<TMP_Text>(true).text : "";
		}
	}
}