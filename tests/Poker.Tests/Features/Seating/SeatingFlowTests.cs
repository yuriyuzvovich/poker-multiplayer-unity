using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Poker.Server.Features.Seating;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Seating
{
	public sealed class SeatingFlowTests
	{
		private static readonly TimeSpan ReservationTime = TimeSpan.FromSeconds(20);

		private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		private readonly TableActor _actor;

		public SeatingFlowTests()
		{
			PokerTable table = new(new TableDefinition("t1", "Test table", 3, 10, 20, 400, 2000, ReservationTime));
			_actor = new TableActor(table, _clock, NullLogger<TableActor>.Instance);
		}

		[Fact]
		public async Task Reserve_IsAnsweredToTheRequester_AndBroadcastToEveryWatcher_ThenTheSeatIsTakenForOthers()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			RecordingConnection bob = await JoinAsync("p-bob", "Bob");

			await ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 7), 1));

			SentMessage answer = alice.Sent[0];
			Assert.Equal(7, answer.RequestId);
			SeatReservationResponse reservation = Assert.IsType<SeatReservationResponse>(answer.Message);
			Assert.Equal((_clock.UtcNow + ReservationTime).ToUnixTimeMilliseconds(), reservation.ExpiresAtUnixMs);
			Assert.Equal(400, reservation.MinBuyIn);
			Assert.Equal("p-alice", Assert.Single(alice.SentOfType<SeatReservedEvent>()).Seat.PlayerId);
			Assert.Equal("p-alice", Assert.Single(bob.SentOfType<SeatReservedEvent>()).Seat.PlayerId);
			Assert.Equal(0, Assert.Single(bob.Sent).RequestId);

			await ExecuteAsync(new ReserveSeatWork(new MessageContext(bob, 8), 1));

			Assert.Equal(SeatErrorCodes.SeatTaken, Assert.Single(bob.SentOfType<ErrorResponse>()).Code);
		}

		[Fact]
		public async Task Reserve_WithoutJoining_IsRejected()
		{
			RecordingConnection stranger = new(new PlayerIdentity("p-x", "Stranger"));

			await ExecuteAsync(new ReserveSeatWork(new MessageContext(stranger, 1), 1));

			Assert.Equal(TableErrorCodes.NotJoined, Assert.Single(stranger.SentOfType<ErrorResponse>()).Code);
		}

		[Fact]
		public async Task BuyIn_SeatsThePlayer_AndEveryWatcherSeesIt()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			RecordingConnection bob = await JoinAsync("p-bob", "Bob");
			await ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 1), 2));

			await ExecuteAsync(new BuyInWork(new MessageContext(alice, 2), 800));

			Assert.Equal(800, Assert.Single(alice.SentOfType<BuyInAcceptedResponse>()).Stack);
			PlayerSeatedEvent seated = Assert.Single(bob.SentOfType<PlayerSeatedEvent>());
			Assert.Equal(2, seated.Seat.Index);
			Assert.Equal(SeatStatus.Seated, seated.Seat.Status);
		}

		[Fact]
		public async Task Ticker_AfterTheDeadline_ReleasesTheReservation_ForEveryWatcher()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			RecordingConnection bob = await JoinAsync("p-bob", "Bob");
			await ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 1), 1));

			_clock.Advance(ReservationTime - TimeSpan.FromSeconds(1));
			await ExecuteAsync(ExpireReservationsWork.Instance);
			Assert.Empty(bob.SentOfType<SeatReleasedEvent>());

			_clock.Advance(TimeSpan.FromSeconds(1));
			await ExecuteAsync(ExpireReservationsWork.Instance);

			Assert.Equal(SeatReleaseReason.ReservationExpired, Assert.Single(alice.SentOfType<SeatReleasedEvent>()).Reason);
			Assert.Equal(SeatReleaseReason.ReservationExpired, Assert.Single(bob.SentOfType<SeatReleasedEvent>()).Reason);
		}

		[Fact]
		public async Task StandUp_FreesTheSeat_AndAnswersWithItsIndex()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			await ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 1), 1));
			await ExecuteAsync(new BuyInWork(new MessageContext(alice, 2), 400));

			await ExecuteAsync(new StandUpWork(new MessageContext(alice, 3)));

			Assert.Equal(1, Assert.Single(alice.SentOfType<StandUpAcceptedResponse>()).SeatIndex);
			Assert.Equal(SeatReleaseReason.StoodUp, Assert.Single(alice.SentOfType<SeatReleasedEvent>()).Reason);
		}

		[Fact]
		public async Task Disconnect_ReleasesTheSeat_AndStopsWatching()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			RecordingConnection bob = await JoinAsync("p-bob", "Bob");
			await ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 1), 1));
			alice.Clear();

			await ExecuteAsync(new DisconnectWork(alice));

			Assert.Equal(SeatReleaseReason.Disconnected, Assert.Single(bob.SentOfType<SeatReleasedEvent>()).Reason);
			Assert.Empty(alice.Sent);
		}

		private Task ExecuteAsync(ITableWork work) => _actor.ExecuteAsync(work, CancellationToken.None);

		private async Task<RecordingConnection> JoinAsync(string playerId, string name)
		{
			RecordingConnection connection = new(new PlayerIdentity(playerId, name), "c-" + playerId);
			await ExecuteAsync(new JoinTableWork(new MessageContext(connection, 1)));
			connection.Clear();
			return connection;
		}
	}
}