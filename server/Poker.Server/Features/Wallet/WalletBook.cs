using System;
using System.Collections.Generic;
using System.Threading;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Session;
using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Features.Wallet.Models;

namespace Poker.Server.Features.Wallet
{
	public sealed class WalletBook : IChipBank
	{
		private readonly object _lock = new object();
		private readonly Dictionary<string, long> _balances = new();
		private readonly WalletOptions _options;
		private readonly SessionManager _sessions;

		public WalletBook(WalletOptions options, SessionManager sessions)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
		}

		public long GetBalance(string playerId)
		{
			lock (_lock) return BalanceOf(playerId);
		}

		public bool TryWithdraw(string playerId, long amount)
		{
			long balance;
			lock (_lock)
			{
				balance = BalanceOf(playerId);
				if (amount > balance) return false;

				balance -= amount;
				_balances[playerId] = balance;
			}

			Notify(playerId, balance, -amount, BalanceChangeReason.BuyIn);
			return true;
		}

		public void Deposit(string playerId, long amount)
		{
			long balance;
			lock (_lock)
			{
				balance = BalanceOf(playerId) + amount;
				_balances[playerId] = balance;
			}

			Notify(playerId, balance, amount, BalanceChangeReason.CashOut);
		}

		public bool TryClaimFreeChips(string playerId, out long balance)
		{
			long delta;
			lock (_lock)
			{
				balance = BalanceOf(playerId);
				if (balance >= _options.FreeChipsBelow) return false;

				delta = _options.FreeChipsUpTo - balance;
				balance = _options.FreeChipsUpTo;
				_balances[playerId] = balance;
			}

			Notify(playerId, balance, delta, BalanceChangeReason.FreeChips);
			return true;
		}

		private long BalanceOf(string playerId)
		{
			if (_balances.TryGetValue(playerId, out long balance)) return balance;

			_balances.Add(playerId, _options.StartingBalance);
			return _options.StartingBalance;
		}

		private void Notify(string playerId, long balance, long delta, BalanceChangeReason reason)
		{
			if (!_sessions.TryFindByPlayerId(playerId, out ClientSession? session)) return;

			_ = session.SendAsync(new BalanceChangedEvent { Balance = balance, Delta = delta, Reason = reason }, 0, CancellationToken.None);
		}
	}
}