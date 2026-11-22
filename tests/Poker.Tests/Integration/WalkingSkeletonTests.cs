using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table;
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
	public sealed class WalkingSkeletonTests
	{
		[Fact]
		public async Task HelloListThenJoin_OverRealSocket_ResponsesCarryTheirRequestIds()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient client = await ConnectAsync(app, "Alice", timeout.Token);

			TableListResponse tables = Assert.IsType<TableListResponse>((await client.RequestAsync(new ListTablesRequest(), timeout.Token)).Payload);
			Assert.Equal(2, tables.Tables.Length);

			TableSnapshot snapshot = Assert.IsType<TableSnapshot>((await client.RequestAsync(new JoinTableRequest { TableId = tables.Tables[0].TableId }, timeout.Token)).Payload);
			Assert.Equal(6, snapshot.Seats.Length);
			Assert.Equal("Grace", snapshot.Seats[4].PlayerName);

			await client.CloseAsync(timeout.Token);
			await app.StopAsync(timeout.Token);
		}

		[Fact]
		public async Task RequestBeforeHello_IsRejectedWithSessionRequired()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient client = new();
			await client.ConnectAsync(TestServer.SocketUri(app), timeout.Token);

			IncomingMessage reply = await client.RequestAsync(new ListTablesRequest(), timeout.Token);

			Assert.Equal(ProtocolErrorCodes.SessionRequired, Assert.IsType<ErrorResponse>(reply.Payload).Code);
			await client.CloseAsync(timeout.Token);
			await app.StopAsync(timeout.Token);
		}

		[Fact]
		public async Task JoinUnknownTable_IsRejectedWithTableNotFound()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient client = await ConnectAsync(app, "Alice", timeout.Token);

			IncomingMessage reply = await client.RequestAsync(new JoinTableRequest { TableId = "missing" }, timeout.Token);

			Assert.Equal(TableErrorCodes.TableNotFound, Assert.IsType<ErrorResponse>(reply.Payload).Code);
			await client.CloseAsync(timeout.Token);
			await app.StopAsync(timeout.Token);
		}

		[Fact]
		public async Task TwoClients_SeeTheSameSeatTaken_AndADisconnectFreesIt()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using TestClient alice = await ConnectAsync(app, "Alice", timeout.Token);
			using TestClient bob = await ConnectAsync(app, "Bob", timeout.Token);
			JoinTableRequest join = new() { TableId = TableSeed.PracticeTableId };
			await alice.RequestAsync(join, timeout.Token);
			await bob.RequestAsync(join, timeout.Token);

			IncomingMessage reserved = await alice.RequestAsync(new ReserveSeatRequest { TableId = TableSeed.PracticeTableId, SeatIndex = 1 }, timeout.Token);
			Assert.Equal(1, Assert.IsType<SeatReservationResponse>(reserved.Payload).SeatIndex);
			SeatReservedEvent bobSees = await bob.ReceiveEventAsync<SeatReservedEvent>(timeout.Token);
			Assert.Equal("Alice", bobSees.Seat.PlayerName);

			IncomingMessage taken = await bob.RequestAsync(new ReserveSeatRequest { TableId = TableSeed.PracticeTableId, SeatIndex = 1 }, timeout.Token);
			Assert.Equal(SeatErrorCodes.SeatTaken, Assert.IsType<ErrorResponse>(taken.Payload).Code);

			IncomingMessage boughtIn = await alice.RequestAsync(new BuyInRequest { TableId = TableSeed.PracticeTableId, Amount = 1000 }, timeout.Token);
			Assert.Equal(1000, Assert.IsType<BuyInAcceptedResponse>(boughtIn.Payload).Stack);
			Assert.Equal(SeatStatus.Seated, (await bob.ReceiveEventAsync<PlayerSeatedEvent>(timeout.Token)).Seat.Status);

			await alice.CloseAsync(timeout.Token);
			SeatReleasedEvent released = await bob.ReceiveEventAsync<SeatReleasedEvent>(timeout.Token);
			Assert.Equal(1, released.SeatIndex);
			Assert.Equal(SeatReleaseReason.Disconnected, released.Reason);

			await bob.CloseAsync(timeout.Token);
			await app.StopAsync(timeout.Token);
		}

		[Fact]
		public async Task TextFrame_IsRejectedWithInvalidMessageType()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			using ClientWebSocket socket = new();
			await socket.ConnectAsync(TestServer.SocketUri(app), timeout.Token);

			await socket.SendAsync(new ArraySegment<byte>(new byte[] { (byte)'{', (byte)'}' }), WebSocketMessageType.Text, true, timeout.Token);
			WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(new byte[256]), timeout.Token);

			Assert.Equal(WebSocketMessageType.Close, result.MessageType);
			Assert.Equal(WebSocketCloseStatus.InvalidMessageType, socket.CloseStatus);
			await app.StopAsync(timeout.Token);
		}

		private static async Task<TestClient> ConnectAsync(WebApplication app, string name, CancellationToken cancellationToken)
		{
			TestClient client = new();
			await client.ConnectAsync(TestServer.SocketUri(app), cancellationToken);
			await client.HelloAsync(name, cancellationToken);
			return client;
		}
	}
}