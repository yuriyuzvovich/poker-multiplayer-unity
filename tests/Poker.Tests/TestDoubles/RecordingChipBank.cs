using System.Collections.Generic;
using Poker.Server.Features.Table.Domain;

namespace Poker.Tests.TestDoubles
{
	public sealed class RecordingChipBank : IChipBank
	{
		public Dictionary<string, long> Balances { get; } = new();

		public bool TryWithdraw(string playerId, long amount)
		{
			long balance = BalanceOf(playerId);
			if (balance < amount) return false;

			Balances[playerId] = balance - amount;
			return true;
		}

		public void Deposit(string playerId, long amount) => Balances[playerId] = BalanceOf(playerId) + amount;

		public long BalanceOf(string playerId) => Balances.TryGetValue(playerId, out long balance) ? balance : 0;
	}
}