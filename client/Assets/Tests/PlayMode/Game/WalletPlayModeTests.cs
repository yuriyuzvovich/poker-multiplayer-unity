using System.Collections;
using System.Globalization;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Table.Views;
using Poker.Client.Features.Wallet.Views;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using static Poker.Client.Tests.PlayMode.PlayModeUi;
using Object = UnityEngine.Object;

namespace Poker.Client.Tests.PlayMode.Game
{
	public sealed class WalletPlayModeTests
	{
		[UnityTest]
		[Timeout(240000)]
		public IEnumerator BuyIn_TakesFromTheWallet_AndStandingUpPaysTheFinalStackBack() => UniTask.ToCoroutine(async () =>
		{
			(TableView table, HandView hand) = await SitAtHeadsUpAsync();
			TMP_Text balance = FindComponent<TMP_Text>(Object.FindAnyObjectByType<WalletView>(), "Balance");
			await WaitForAsync(() => balance.text == "Balance 3,000" ? balance : null, "5,000 minus the 2,000 buy-in");
			await WaitForAsync(() => FindMyFaceUpCards(table), "a hand dealt to me", 20f);

			SeatAction(table, 2).onClick.Invoke();
			TMP_Text tableStatus = FindComponent<TMP_Text>(table, "Toast");
			await WaitForAsync(() => tableStatus.text.Contains("leave the seat when this hand ends") ? tableStatus : null, "the stand-up waits for the hand");
			Assert.AreEqual("Balance 3,000", balance.text, "nothing is paid back while the hand runs");

			await WaitForAsync(() => SeatLabel(table, 2).Contains("empty") ? table : null, "the seat released after the hand", 60f);
			TMP_Text handStatus = FindComponent<TMP_Text>(hand, "Status");
			await WaitForAsync(() => handStatus.text.Contains(" wins ") ? handStatus : null, "hand.ended shown", 20f);
			Assert.GreaterOrEqual(ReadBalance(balance), 3000, "the final stack is paid back once the hand ends");

			await BackToLobbyAndCloseAsync(table, hand);
		});

		private static long ReadBalance(TMP_Text balance) =>
			long.TryParse(balance.text.Replace("Balance ", ""), NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long value) ? value : -1;
	}
}