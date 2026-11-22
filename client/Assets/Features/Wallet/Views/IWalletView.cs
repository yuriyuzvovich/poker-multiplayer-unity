using System;

namespace Poker.Client.Features.Wallet.Views
{
	public interface IWalletView
	{
		public event Action FreeChipsClicked;

		public void SetBalance(string balance);

		public void SetMessage(string message);
	}
}