using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Xunit;

namespace Poker.Tests.Integration
{
	[Collection(IntegrationCollection.Name)]
	public sealed class ReliabilityIntegrationTests
	{
		private const string TableId = TableSeed.PracticeTableId;

		[Fact]
		public async Task ShortDrop_ResumesTheSession_AndResendsWhatWasMissedInOrder()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			Uri uri = TestServer.SocketUri(app);
			using TestClient alice = await JoinAsync(uri, "Alice", timeout.Token);
			using TestClient bob = await JoinAsync(uri, "Bob", timeout.Token);
			await alice.RequestAsync(new ReserveSeatRequest { TableId = TableId, SeatIndex = 1 }, timeout.Token);
			string firstKey = alice.ResumeKey;

			alice.Abort();
			await bob.RequestAsync(new ReserveSeatRequest { TableId = TableId, SeatIndex = 3 }, timeout.Token);
			WelcomeResponse welcome = await alice.ReconnectAsync(uri, timeout.Token);

			Assert.True(welcome.Resumed);
			Assert.NotEqual(firstKey, welcome.ResumeKey);
			List<object> missed = new();
			SeatReservedEvent bobsSeat = await alice.ReceiveUntilAsync<SeatReservedEvent>(missed, e => e.Seat.Index == 3, timeout.Token);
			Assert.Equal("Bob", bobsSeat.Seat.PlayerName);
			IncomingMessage standUp = await alice.RequestAsync(new StandUpRequest { TableId = TableId }, timeout.Token);
			Assert.Equal(1, Assert.IsType<StandUpAcceptedResponse>(standUp.Payload).SeatIndex);
		}

		[Fact]
		public async Task DropLongerThanTheZombieWindow_ReleasesTheSeat_AndTheNextHelloStartsANewSession()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token, "--Connection:ZombieWindow=00:00:01");
			Uri uri = TestServer.SocketUri(app);
			using TestClient alice = await JoinAsync(uri, "Alice", timeout.Token);
			using TestClient bob = await JoinAsync(uri, "Bob", timeout.Token);
			await alice.RequestAsync(new ReserveSeatRequest { TableId = TableId, SeatIndex = 1 }, timeout.Token);
			await bob.ReceiveEventAsync<SeatReservedEvent>(timeout.Token);

			alice.Abort();
			SeatReleasedEvent released = await bob.ReceiveEventAsync<SeatReleasedEvent>(timeout.Token);
			WelcomeResponse welcome = await alice.ReconnectAsync(uri, timeout.Token);

			Assert.Equal(1, released.SeatIndex);
			Assert.Equal(SeatReleaseReason.Disconnected, released.Reason);
			Assert.False(welcome.Resumed);
		}

		[Fact]
		public async Task RequestLostWithTheSocket_IsResentOnResume_AndAnswered()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			Uri uri = TestServer.SocketUri(app);
			using TestClient alice = await ConnectAsync(uri, "Alice", timeout.Token);

			ushort lostRequestId = alice.StampWithoutSending(new ListTablesRequest());
			alice.Abort();
			await alice.ReconnectAsync(uri, timeout.Token);
			IncomingMessage answer = await alice.ReceiveResponseAsync(lostRequestId, timeout.Token);
			IncomingMessage next = await alice.RequestAsync(new ListTablesRequest(), timeout.Token);

			Assert.IsType<TableListResponse>(answer.Payload);
			Assert.IsType<TableListResponse>(next.Payload);
		}

		[Fact]
		public async Task DuplicatePacket_IsAnsweredOnce()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient alice = await ConnectAsync(TestServer.SocketUri(app), "Alice", timeout.Token);

			byte[] packet = alice.Stamp(new ListTablesRequest(), 60);
			await alice.SendRawAsync(packet, timeout.Token);
			await alice.SendRawAsync(packet, timeout.Token);
			await alice.SendAsync(new ListTablesRequest(), 61, timeout.Token);
			List<ushort> answered = new();
			while (!answered.Contains(61)) answered.Add((await alice.ReceiveAsync(timeout.Token)).Header.RequestId);

			Assert.Equal(new ushort[] { 60, 61 }, answered);
		}

		[Fact]
		public async Task SequenceGap_ClosesTheSocketWithTheGapCode()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient alice = await ConnectAsync(TestServer.SocketUri(app), "Alice", timeout.Token);

			alice.StampWithoutSending(new ListTablesRequest());
			await alice.SendAsync(new ListTablesRequest(), 50, timeout.Token);
			WebSocketReceiveResult result = await alice.Socket.ReceiveAsync(new ArraySegment<byte>(new byte[256]), timeout.Token);

			Assert.Equal(WebSocketMessageType.Close, result.MessageType);
			Assert.Equal(CloseCodes.SequenceGap, (int)alice.Socket.CloseStatus!.Value);
		}

		[Fact]
		public async Task UnnumberedGameMessage_AfterHello_IsDropped()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient alice = await ConnectAsync(TestServer.SocketUri(app), "Alice", timeout.Token);
			MessageCodec codec = new(MessageCatalog.CreateRegistry(), new JsonPayloadSerializer());

			await alice.SendRawAsync(codec.Encode(new ListTablesRequest(), requestId: 77), timeout.Token);
			await alice.SendAsync(new ListTablesRequest(), 78, timeout.Token);
			IncomingMessage first = await alice.ReceiveAsync(timeout.Token);

			Assert.Equal(78, first.Header.RequestId);
		}

		[Fact]
		public async Task SilentSocket_IsDroppedAfterTwoHeartbeats_AndTheSessionCanResume()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token, "--Connection:HeartbeatInterval=00:00:00.300");
			Uri uri = TestServer.SocketUri(app);
			using TestClient alice = await ConnectAsync(uri, "Alice", timeout.Token);

			await Assert.ThrowsAsync<WebSocketException>(() => alice.Socket.ReceiveAsync(new ArraySegment<byte>(new byte[256]), timeout.Token));
			WelcomeResponse welcome = await alice.ReconnectAsync(uri, timeout.Token);

			Assert.True(welcome.Resumed);
			Assert.Equal(300, welcome.HeartbeatMs);
		}

		private static async Task<TestClient> ConnectAsync(Uri uri, string name, CancellationToken cancellationToken)
		{
			TestClient client = new();
			await client.ConnectAsync(uri, cancellationToken);
			await client.HelloAsync(name, cancellationToken);
			return client;
		}

		private static async Task<TestClient> JoinAsync(Uri uri, string name, CancellationToken cancellationToken)
		{
			TestClient client = await ConnectAsync(uri, name, cancellationToken);
			await client.RequestAsync(new JoinTableRequest { TableId = TableId }, cancellationToken);
			return client;
		}
	}
}