using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Poker.Server.Features.Seating;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Requests;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Table
{
	public sealed class TableActorTests
	{
		private readonly ManualClock _clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		private readonly TableActor _actor;

		public TableActorTests()
		{
			PokerTable table = new(new TableDefinition("t1", "Test table", 3, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)));
			table.SeatBot(0, new PlayerIdentity("bot-ada", "Ada"), 5000);
			_actor = new TableActor(table, _clock, NullLogger<TableActor>.Instance);
		}

		[Fact]
		public async Task Join_RespondsWithTheSnapshotUnderTheRequestId()
		{
			RecordingConnection viewer = new(new PlayerIdentity("p-1", "Viewer"));

			await _actor.ExecuteAsync(new JoinTableWork(new MessageContext(viewer, 4)), CancellationToken.None);

			SentMessage sent = Assert.Single(viewer.Sent);
			Assert.Equal(4, sent.RequestId);
			TableSnapshot snapshot = Assert.IsType<TableSnapshot>(sent.Message);
			Assert.Equal(3, snapshot.Seats.Length);
			Assert.Equal("Ada", snapshot.Seats[0].PlayerName);
		}

		[Fact]
		public async Task Summary_FollowsTheTableAfterEachWorkItem()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			Assert.Equal(1, _actor.Summary.OccupiedSeats);

			await _actor.ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 1), 1), CancellationToken.None);

			Assert.Equal(2, _actor.Summary.OccupiedSeats);
		}

		[Fact]
		public async Task FailingWorkItem_IsContained_AndTheActorKeepsServing()
		{
			RecordingConnection viewer = new(new PlayerIdentity("p-1", "Viewer"));

			await _actor.ExecuteAsync(new ThrowingWork(), CancellationToken.None);
			await _actor.ExecuteAsync(new JoinTableWork(new MessageContext(viewer, 1)), CancellationToken.None);

			Assert.IsType<TableSnapshot>(Assert.Single(viewer.Sent).Message);
		}

		[Fact]
		public async Task DeadWatcher_IsDropped_AndLiveWatchersStillReceiveEvents()
		{
			RecordingConnection alice = await JoinAsync("p-alice", "Alice");
			RecordingConnection dead = await JoinAsync("p-dead", "Dead");
			dead.FailSendsWith = new WebSocketException("gone");
			alice.Clear();

			await _actor.ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 2), 1), CancellationToken.None);
			dead.FailSendsWith = null;
			await _actor.ExecuteAsync(new StandUpWork(new MessageContext(alice, 3)), CancellationToken.None);

			Assert.Single(alice.SentOfType<SeatReservedEvent>());
			Assert.Single(alice.SentOfType<SeatReleasedEvent>());
			Assert.Empty(dead.SentOfType<SeatReleasedEvent>());
		}

		[Fact]
		public async Task ListenerReactions_AreBroadcastRightAfterTheEventThatCausedThem()
		{
			TableActor actor = CreateActorWith(new EchoListener());
			RecordingConnection alice = await JoinAsync(actor, "p-alice", "Alice");
			alice.Clear();

			await actor.ExecuteAsync(new PublishWork("first", "second"), CancellationToken.None);

			Assert.Equal(new object[] { "first", "echo of first", "second", "echo of second" }, EventsOf(alice));
		}

		[Fact]
		public async Task FailingListener_IsContained_AndTheEventIsStillBroadcast()
		{
			TableActor actor = CreateActorWith(new ThrowingListener());
			RecordingConnection alice = await JoinAsync(actor, "p-alice", "Alice");
			alice.Clear();

			await actor.ExecuteAsync(new ReserveSeatWork(new MessageContext(alice, 2), 1), CancellationToken.None);

			Assert.Single(alice.SentOfType<SeatReservedEvent>());
		}

		private TableActor CreateActorWith(ITableEventListener listener)
		{
			PokerTable table = new(new TableDefinition("t2", "Listener table", 3, 10, 20, 400, 2000, TimeSpan.FromSeconds(20)));
			return new TableActor(table, _clock, NullLogger<TableActor>.Instance, new[] { listener });
		}

		private static List<object> EventsOf(RecordingConnection connection)
		{
			List<object> events = new();
			foreach (SentMessage sent in connection.Sent)
			{
				if (sent.RequestId != 0) continue;

				events.Add(sent.Message);
			}

			return events;
		}

		private Task<RecordingConnection> JoinAsync(string playerId, string name) => JoinAsync(_actor, playerId, name);

		private static async Task<RecordingConnection> JoinAsync(TableActor actor, string playerId, string name)
		{
			RecordingConnection connection = new(new PlayerIdentity(playerId, name), "c-" + playerId);
			await actor.ExecuteAsync(new JoinTableWork(new MessageContext(connection, 1)), CancellationToken.None);
			return connection;
		}

		private sealed class EchoListener : ITableEventListener
		{
			public void OnEvent(TableContext context, TableEvent tableEvent)
			{
				if (tableEvent.Message is not string text) return;
				if (text.StartsWith("echo")) return;

				context.Table.Publish("echo of " + text);
			}
		}

		private sealed class PublishWork : ITableWork
		{
			private readonly string[] _messages;

			public PublishWork(params string[] messages) => _messages = messages;

			public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken)
			{
				foreach (string message in _messages)
				{
					context.Table.Publish(message);
				}

				return Task.CompletedTask;
			}
		}

		private sealed class ThrowingListener : ITableEventListener
		{
			public void OnEvent(TableContext context, TableEvent tableEvent) => throw new InvalidOperationException("listener boom");
		}

		private sealed class ThrowingWork : ITableWork
		{
			public Task ExecuteAsync(TableContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("boom");
		}
	}
}