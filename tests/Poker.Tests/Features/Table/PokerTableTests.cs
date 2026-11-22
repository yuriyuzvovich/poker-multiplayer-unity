using System;
using System.Collections.Generic;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Table
{
	public sealed class PokerTableTests
	{
		private static readonly TimeSpan ReservationTime = TimeSpan.FromSeconds(20);
		private static readonly PlayerIdentity Alice = new("p-alice", "Alice");
		private static readonly PlayerIdentity Bob = new("p-bob", "Bob");

		private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		private readonly PokerTable _table = new(new TableDefinition("t1", "Test table", 3, 10, 20, 400, 2000, ReservationTime));

		[Fact]
		public void Reserve_EmptySeat_HoldsItUntilTheDeadline_AndRaisesSeatReserved()
		{
			SeatingResult result = _table.Reserve(1, Alice, _clock.UtcNow);

			Assert.True(result.IsSuccess);
			Seat seat = _table.Seats[1];
			Assert.Equal(SeatStatus.Reserved, seat.Status);
			Assert.Equal(_clock.UtcNow + ReservationTime, seat.ReservedUntil);
			SeatReservedEvent reserved = Assert.IsType<SeatReservedEvent>(Assert.Single(TakeEvents()));
			Assert.Equal("t1", reserved.TableId);
			Assert.Equal("p-alice", reserved.Seat.PlayerId);
			Assert.Equal((_clock.UtcNow + ReservationTime).ToUnixTimeMilliseconds(), reserved.Seat.ReservedUntilUnixMs);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(3)]
		public void Reserve_IndexOutsideTheTable_IsInvalid(int seatIndex)
		{
			Assert.Equal(SeatErrorCodes.SeatInvalid, _table.Reserve(seatIndex, Alice, _clock.UtcNow).ErrorCode);
		}

		[Fact]
		public void Reserve_SeatHeldBySomeoneElse_IsTaken()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);

			Assert.Equal(SeatErrorCodes.SeatTaken, _table.Reserve(1, Bob, _clock.UtcNow).ErrorCode);
		}

		[Fact]
		public void Reserve_SecondSeatForTheSamePlayer_IsRejected()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);

			Assert.Equal(SeatErrorCodes.AlreadySeated, _table.Reserve(2, Alice, _clock.UtcNow).ErrorCode);
		}

		[Fact]
		public void BuyIn_WithinLimits_SeatsThePlayer_AndRaisesPlayerSeated()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);
			TakeEvents();

			SeatingResult result = _table.BuyIn("p-alice", 1000, _clock.UtcNow);

			Assert.True(result.IsSuccess);
			Assert.Equal(SeatStatus.Seated, _table.Seats[1].Status);
			Assert.Equal(1000, _table.Seats[1].Stack);
			Assert.Null(_table.Seats[1].ReservedUntil);
			Assert.Equal(1000, Assert.IsType<PlayerSeatedEvent>(Assert.Single(TakeEvents())).Seat.Stack);
		}

		[Theory]
		[InlineData(399)]
		[InlineData(2001)]
		public void BuyIn_OutsideLimits_IsRejected_AndKeepsTheReservation(long amount)
		{
			_table.Reserve(1, Alice, _clock.UtcNow);

			Assert.Equal(SeatErrorCodes.BuyInOutOfRange, _table.BuyIn("p-alice", amount, _clock.UtcNow).ErrorCode);
			Assert.Equal(SeatStatus.Reserved, _table.Seats[1].Status);
		}

		[Fact]
		public void BuyIn_WithoutAReservation_IsRejected()
		{
			Assert.Equal(SeatErrorCodes.NotReserved, _table.BuyIn("p-alice", 1000, _clock.UtcNow).ErrorCode);
		}

		[Fact]
		public void ExpireReservations_KeepsTheSeatUntilTheDeadline_ThenReleasesIt()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);
			TakeEvents();

			_clock.Advance(ReservationTime - TimeSpan.FromMilliseconds(1));
			_table.ExpireReservations(_clock.UtcNow);
			Assert.Equal(SeatStatus.Reserved, _table.Seats[1].Status);
			Assert.Empty(TakeEvents());

			_clock.Advance(TimeSpan.FromMilliseconds(1));
			_table.ExpireReservations(_clock.UtcNow);
			Assert.Equal(SeatStatus.Empty, _table.Seats[1].Status);
			SeatReleasedEvent released = Assert.IsType<SeatReleasedEvent>(Assert.Single(TakeEvents()));
			Assert.Equal(1, released.SeatIndex);
			Assert.Equal(SeatReleaseReason.ReservationExpired, released.Reason);
		}

		[Fact]
		public void BuyIn_AfterTheDeadline_ReleasesTheSeat_EvenBeforeTheTicker()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);
			TakeEvents();
			_clock.Advance(ReservationTime);

			Assert.Equal(SeatErrorCodes.ReservationExpired, _table.BuyIn("p-alice", 1000, _clock.UtcNow).ErrorCode);
			Assert.Equal(SeatStatus.Empty, _table.Seats[1].Status);
			Assert.Equal(SeatReleaseReason.ReservationExpired, Assert.IsType<SeatReleasedEvent>(Assert.Single(TakeEvents())).Reason);
		}

		[Fact]
		public void ExpireReservations_LeavesSeatedPlayersAlone()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);
			_table.BuyIn("p-alice", 1000, _clock.UtcNow);
			_clock.Advance(TimeSpan.FromHours(1));

			_table.ExpireReservations(_clock.UtcNow);

			Assert.Equal(SeatStatus.Seated, _table.Seats[1].Status);
		}

		[Fact]
		public void StandUp_FreesTheSeat_WithReasonStoodUp()
		{
			_table.Reserve(1, Alice, _clock.UtcNow);
			_table.BuyIn("p-alice", 1000, _clock.UtcNow);
			TakeEvents();

			Assert.True(_table.StandUp("p-alice").IsSuccess);

			Assert.Equal(SeatStatus.Empty, _table.Seats[1].Status);
			Assert.Equal(SeatReleaseReason.StoodUp, Assert.IsType<SeatReleasedEvent>(Assert.Single(TakeEvents())).Reason);
		}

		[Fact]
		public void StandUp_WhenNotSeated_IsRejected()
		{
			Assert.Equal(SeatErrorCodes.NotSeated, _table.StandUp("p-alice").ErrorCode);
		}

		[Fact]
		public void ReleasePlayer_OnDisconnect_UsesReasonDisconnected()
		{
			_table.Reserve(2, Bob, _clock.UtcNow);
			TakeEvents();

			_table.ReleasePlayer("p-bob", SeatReleaseReason.Disconnected);

			Assert.Equal(SeatReleaseReason.Disconnected, Assert.IsType<SeatReleasedEvent>(Assert.Single(TakeEvents())).Reason);
		}

		[Fact]
		public void SnapshotAndSummary_ReflectSeats()
		{
			_table.SeatBot(0, new PlayerIdentity("bot-ada", "Ada"), 5000);
			_table.Reserve(2, Alice, _clock.UtcNow);

			TableSnapshot snapshot = _table.CreateSnapshot();

			Assert.Equal(400, snapshot.MinBuyIn);
			Assert.Equal(2000, snapshot.MaxBuyIn);
			Assert.Equal(SeatStatus.Seated, snapshot.Seats[0].Status);
			Assert.Equal("Ada", snapshot.Seats[0].PlayerName);
			Assert.Equal(SeatStatus.Empty, snapshot.Seats[1].Status);
			Assert.Equal(SeatStatus.Reserved, snapshot.Seats[2].Status);
			Assert.Equal(2, _table.CreateSummary().OccupiedSeats);
		}

		private List<object> TakeEvents()
		{
			List<TableEvent> tableEvents = new();
			_table.TakeEvents(tableEvents);
			List<object> messages = new();
			foreach (TableEvent tableEvent in tableEvents)
			{
				Assert.True(tableEvent.IsPublic);
				messages.Add(tableEvent.Message);
			}

			return messages;
		}
	}
}