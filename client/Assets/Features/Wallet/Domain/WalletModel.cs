using System;

namespace Poker.Client.Features.Wallet.Domain
{
	public sealed class WalletModel
	{
		public event Action Changed;

		public bool IsKnown { get; private set; }
		public long Balance { get; private set; }

		public void SetBalance(long balance)
		{
			IsKnown = true;
			Balance = balance;
			Changed?.Invoke();
		}

		public void Forget()
		{
			IsKnown = false;
			Balance = 0;
			Changed?.Invoke();
		}
	}
}