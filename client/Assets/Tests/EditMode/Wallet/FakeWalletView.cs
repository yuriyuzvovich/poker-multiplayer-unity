using System;
using Poker.Client.Features.Wallet.Views;

namespace Poker.Client.Tests.EditMode.Wallet
{
	public sealed class FakeWalletView : IWalletView
	{
		public event Action FreeChipsClicked;

		public string Balance { get; private set; } = "";
		public string Message { get; private set; } = "";

		public void SetBalance(string balance) => Balance = balance;

		public void SetMessage(string message) => Message = message;

		public void ClickFreeChips() => FreeChipsClicked?.Invoke();
	}
}