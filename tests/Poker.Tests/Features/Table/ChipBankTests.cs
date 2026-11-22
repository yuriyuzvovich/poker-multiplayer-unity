using System;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Models;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Table
{
	public sealed class ChipBankTests
	{
		private static readonly PlayerIdentity Alice = new("p-alice", "Alice");

		private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		private readonly RecordingChipBank _bank = new();
		private readonly PokerTable _table;

		public ChipBankTests()
		{
			_bank.Deposit(Alice.PlayerId, 1500);
			_table = new PokerTable(new TableDefinition("t1", "Test table", 3, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)), _bank);
			_table.Reserve(1, Alice, _clock.UtcNow);
		}

		[Fact]
		public void BuyIn_TakesTheChipsFromTheBank()
		{
			Assert.True(_table.BuyIn(Alice.PlayerId, 1000, _clock.UtcNow).IsSuccess);

			Assert.Equal(500, _bank.BalanceOf(Alice.PlayerId));
		}

		[Fact]
		public void BuyIn_AboveTheBalance_IsRefused_AndTheReservationStays()
		{
			SeatingResult result = _table.BuyIn(Alice.PlayerId, 2000, _clock.UtcNow);

			Assert.Equal(SeatErrorCodes.InsufficientFunds, result.ErrorCode);
			Assert.Equal(SeatStatus.Reserved, _table.Seats[1].Status);
			Assert.Equal(1500, _bank.BalanceOf(Alice.PlayerId));
		}

		[Fact]
		public void StandUp_OutsideAHand_PaysTheStackBack()
		{
			_table.BuyIn(Alice.PlayerId, 1000, _clock.UtcNow);
			_table.TrySetStack(1, Alice.PlayerId, 1240);

			_table.StandUp(Alice.PlayerId);

			Assert.Equal(1740, _bank.BalanceOf(Alice.PlayerId));
			Assert.Equal(SeatStatus.Empty, _table.Seats[1].Status);
		}

		[Fact]
		public void StandUp_InAHand_WaitsForTheHandToRelease_ThenPaysTheFinalStack()
		{
			_table.BuyIn(Alice.PlayerId, 1000, _clock.UtcNow);
			_table.EnterHand(1);

			_table.StandUp(Alice.PlayerId);
			Assert.Equal(500, _bank.BalanceOf(Alice.PlayerId));
			_table.TrySetStack(1, Alice.PlayerId, 600);
			_table.LeaveHand(1);

			Assert.Equal(1100, _bank.BalanceOf(Alice.PlayerId));
			Assert.Equal(SeatStatus.Empty, _table.Seats[1].Status);
		}

		[Fact]
		public void CancelledReservation_PaysNothing()
		{
			_table.StandUp(Alice.PlayerId);

			Assert.Equal(1500, _bank.BalanceOf(Alice.PlayerId));
		}
	}
}