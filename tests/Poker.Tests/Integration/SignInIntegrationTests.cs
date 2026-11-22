using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Xunit;

namespace Poker.Tests.Integration
{
	[Collection(IntegrationCollection.Name)]
	public sealed class SignInIntegrationTests
	{
		private const string TableId = TableSeed.PracticeTableId;
		private const string DeviceKey = "device-alice";

		[Fact]
		public async Task SecondSignIn_TakesOverThePlayerAndTheSeat_AndClosesTheOldSocket()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			Uri uri = TestServer.SocketUri(app);
			using TestClient first = new();
			await first.ConnectAsync(uri, timeout.Token);
			WelcomeResponse firstWelcome = await first.HelloAsync("Alice", DeviceKey, timeout.Token);
			await first.RequestAsync(new JoinTableRequest { TableId = TableId }, timeout.Token);
			await ReserveAsync(first, 1, timeout.Token);

			using TestClient second = new();
			await second.ConnectAsync(uri, timeout.Token);
			WelcomeResponse secondWelcome = await second.HelloAsync("Alice", DeviceKey, timeout.Token);

			Assert.Equal(firstWelcome.PlayerId, secondWelcome.PlayerId);
			Assert.False(secondWelcome.Resumed);
			Assert.Equal(TableId, secondWelcome.SeatedTableId);
			WebSocketReceiveResult closed = await ReceiveCloseAsync(first, timeout.Token);
			Assert.Equal(WebSocketMessageType.Close, closed.MessageType);
			Assert.Equal(CloseCodes.DuplicateLogin, (int)first.Socket.CloseStatus!.Value);
			await first.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
			// Proves a negative: the replaced socket's close must not free the seat. Nothing observable happens when it is
			// right, so give the server time to process the close before checking (the documented exception in tests.md).
			await Task.Delay(500, timeout.Token);

			IncomingMessage joined = await second.RequestAsync(new JoinTableRequest { TableId = TableId }, timeout.Token);
			SeatSnapshot seat = Assert.IsType<TableSnapshot>(joined.Payload).Seats[1];
			Assert.Equal(firstWelcome.PlayerId, seat.PlayerId);
			IncomingMessage standUp = await second.RequestAsync(new StandUpRequest { TableId = TableId }, timeout.Token);
			Assert.Equal(1, Assert.IsType<StandUpAcceptedResponse>(standUp.Payload).SeatIndex);
		}

		[Fact]
		public async Task SignInAfterARestart_FindsTheSeatHeldByTheWaitingSession()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			Uri uri = TestServer.SocketUri(app);
			using TestClient first = new();
			await first.ConnectAsync(uri, timeout.Token);
			WelcomeResponse firstWelcome = await first.HelloAsync("Alice", DeviceKey, timeout.Token);
			await first.RequestAsync(new JoinTableRequest { TableId = TableId }, timeout.Token);
			await ReserveAsync(first, 3, timeout.Token);
			first.Abort();

			using TestClient restarted = new();
			await restarted.ConnectAsync(uri, timeout.Token);
			WelcomeResponse welcome = await restarted.HelloAsync("Alice", DeviceKey, timeout.Token);

			Assert.Equal(firstWelcome.PlayerId, welcome.PlayerId);
			Assert.Equal(TableId, welcome.SeatedTableId);
			await restarted.RequestAsync(new JoinTableRequest { TableId = TableId }, timeout.Token);
			IncomingMessage standUp = await restarted.RequestAsync(new StandUpRequest { TableId = TableId }, timeout.Token);
			Assert.Equal(3, Assert.IsType<StandUpAcceptedResponse>(standUp.Payload).SeatIndex);
		}

		[Fact]
		public async Task SignInWithoutADeviceKey_IsANewPlayerWithNoSeat()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			Uri uri = TestServer.SocketUri(app);
			using TestClient first = new();
			await first.ConnectAsync(uri, timeout.Token);
			WelcomeResponse firstWelcome = await first.HelloAsync("Alice", timeout.Token);
			using TestClient second = new();
			await second.ConnectAsync(uri, timeout.Token);
			WelcomeResponse secondWelcome = await second.HelloAsync("Alice", timeout.Token);

			Assert.NotEqual(firstWelcome.PlayerId, secondWelcome.PlayerId);
			Assert.Equal("", secondWelcome.SeatedTableId);
		}

		private static async Task ReserveAsync(TestClient client, int seatIndex, CancellationToken cancellationToken)
		{
			IncomingMessage reserved = await client.RequestAsync(new ReserveSeatRequest { TableId = TableId, SeatIndex = seatIndex }, cancellationToken);
			Assert.IsType<SeatReservationResponse>(reserved.Payload);
			await client.RequestAsync(new JoinTableRequest { TableId = TableId }, cancellationToken);
		}

		private static async Task<WebSocketReceiveResult> ReceiveCloseAsync(TestClient client, CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[4096];
			while (true)
			{
				WebSocketReceiveResult result = await client.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
				if (result.MessageType != WebSocketMessageType.Close) continue;

				return result;
			}
		}
	}
}