using System;
using System.Text;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Lobby.Views;
using Poker.Client.Features.Seating.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Table.Views.Seats;
using Poker.Client.Infrastructure.Network.Session;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode
{
	public static class PlayModeUi
	{
		public static string SeatLabel(TableView table, int seatNumber) => SeatText(SeatRow(table, seatNumber));

		public static Button SeatAction(TableView table, int seatNumber) => FindComponent<Button>(SeatRow(table, seatNumber), "Action");

		public static float SeatOpacity(TableView table, int seatNumber) => SeatRow(table, seatNumber).GetComponent<CanvasGroup>().alpha;

		public static T FindComponent<T>(Component root, string name) where T : Component =>
			FindComponentOrNull<T>(root, name) ?? throw new AssertionException("No " + typeof(T).Name + " named " + name);

		public static T FindComponentOrNull<T>(Component root, string name) where T : Component
		{
			foreach (T component in root.GetComponentsInChildren<T>(true))
			{
				if (component.name != name) continue;

				return component;
			}

			return null;
		}

		public static async UniTask<T> WaitForAsync<T>(Func<T> probe, string what, float seconds = 10f) where T : class
		{
			float deadline = Time.realtimeSinceStartup + seconds;
			while (Time.realtimeSinceStartup < deadline)
			{
				T result = probe();
				if (result != null) return result;

				await UniTask.Yield();
			}

			throw new AssertionException("Timed out waiting for: " + what);
		}

		public static async UniTask<(TableView Table, HandView Hand)> SitAtHeadsUpAsync()
		{
			(TableView table, HandView hand) = await OpenTableAsync("Table_heads-up");
			await WaitForAsync(() => SeatLabel(table, 1).Contains("Edsger") ? table : null, "seats shown");

			Vector2 tappedAnchor = SeatAnchor(table, 2);
			Vector2 otherAnchor = SeatAnchor(table, 1);
			SeatAction(table, 2).onClick.Invoke();
			BuyInPopup popup = await WaitForAsync(() => Object.FindAnyObjectByType<BuyInPopup>(), "buy-in popup");
			FindComponent<Button>(popup, "Min").onClick.Invoke();
			await WaitForAsync(() => SeatLabel(table, 2).Contains("YOU") ? table : null, "seated");
			Assert.AreEqual(tappedAnchor, SeatAnchor(table, 2), "I sit where I tapped");
			Assert.AreEqual(otherAnchor, SeatAnchor(table, 1), "the other player stays in place");
			return (table, hand);
		}

		public static UniTask StartAppAsNewPlayerAsync()
		{
			PlayerPrefs.DeleteKey(DeviceKey.PrefsKey);
			return StartAppAsync();
		}

		public static UniTask StartAppAsync() => SceneManager.LoadSceneAsync("Main").ToUniTask();

		public static async UniTask<(TableView Table, HandView Hand)> OpenTableAsync(string tableButton)
		{
			await StartAppAsNewPlayerAsync();
			LobbyView lobby = await WaitForAsync(() => Object.FindAnyObjectByType<LobbyView>(), "lobby view");
			Button button = await WaitForAsync(() => FindComponentOrNull<Button>(lobby, tableButton), tableButton + " listed (is the server running on localhost:5080?)");
			button.onClick.Invoke();
			TableView table = await WaitForAsync(() => Object.FindAnyObjectByType<TableView>(), "table screen loaded");
			HandView hand = await WaitForAsync(() => Object.FindAnyObjectByType<HandView>(), "hand layer loaded into the table screen");
			return (table, hand);
		}

		public static async UniTask LeaveAsync(TableView table, HandView hand)
		{
			SeatAction(table, 2).onClick.Invoke();
			await WaitForAsync(() => SeatLabel(table, 2).Contains("empty") ? table : null, "stood up (after the hand if I was in one)", 60f);
			await BackToLobbyAndCloseAsync(table, hand);
		}

		public static async UniTask BackToLobbyAndCloseAsync(TableView table, HandView hand)
		{
			FindComponent<Button>(table, "BackButton").onClick.Invoke();
			await WaitForAsync(() => table == null && hand == null ? new object() : null, "table screen and hand layer released");
			await WaitForAsync(() => Object.FindAnyObjectByType<LobbyView>(), "lobby loaded again");

			Object.Destroy(GameObject.Find("Root"));
			await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime);
		}

		public static RectTransform FindMySeat(TableView table)
		{
			foreach (SeatWidget seat in table.GetComponentsInChildren<SeatWidget>())
			{
				if (!SeatText(seat.transform).Contains("YOU")) continue;

				return (RectTransform)seat.transform;
			}

			return null;
		}

		public static CardView[] FindMyFaceUpCards(TableView table)
		{
			RectTransform seat = FindMySeat(table);
			if (seat == null) return null;

			CardView[] cards = FindComponent<RectTransform>(seat, "Cards").GetComponentsInChildren<CardView>();
			return cards.Length > 0 && cards[0].Code.Length == 2 ? cards : null;
		}

		private static string SeatText(Transform seat)
		{
			StringBuilder text = new();
			foreach (TMP_Text part in seat.GetComponentsInChildren<TMP_Text>())
			{
				text.Append(part.text).Append(' ');
			}

			return text.ToString();
		}

		private static Vector2 SeatAnchor(TableView table, int seatNumber) => ((RectTransform)SeatRow(table, seatNumber)).anchorMin;

		private static Transform SeatRow(TableView table, int seatNumber) => FindComponent<RectTransform>(table, "Seat_" + seatNumber);
	}
}