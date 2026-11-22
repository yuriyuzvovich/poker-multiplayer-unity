using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Xunit;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Tests.Integration
{
	[Collection(IntegrationCollection.Name)]
	public sealed class HandIntegrationTests
	{
		private const string TableId = TableSeed.HeadsUpTableId;

		[Fact]
		public async Task HoleCardsReachOnlyTheirOwner_AndAMidHandJoinerGetsAHandSnapshotWithoutCards()
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
			await using WebApplication app = await TestServer.StartAsync(timeout.Token);
			Uri uri = TestServer.SocketUri(app);

			using TestClient spectator = await JoinAsync(uri, "Spectator", timeout.Token);
			using TestClient alice = await JoinAsync(uri, "Alice", timeout.Token);
			await alice.RequestAsync(new ReserveSeatRequest { TableId = TableId, SeatIndex = 1 }, timeout.Token);
			await alice.RequestAsync(new BuyInRequest { TableId = TableId, Amount = 2000 }, timeout.Token);

			List<object> aliceLog = new();
			TurnStartedEvent aliceTurn = await alice.ReceiveUntilAsync<TurnStartedEvent>(aliceLog, turn => turn.SeatIndex == 1, timeout.Token);
			foreach (object message in aliceLog)
			{
				if (message is not HoleCardsEvent holeCards) continue;

				Assert.Equal(1, holeCards.SeatIndex);
				Assert.Equal(2, holeCards.Cards.Length);
			}

			Assert.Contains(aliceLog, message => message is HoleCardsEvent);

			using TestClient lateJoiner = new();
			await lateJoiner.ConnectAsync(uri, timeout.Token);
			await lateJoiner.HelloAsync("Late", timeout.Token);
			await lateJoiner.SendAsync(new JoinTableRequest { TableId = TableId }, 99, timeout.Token);
			HandSnapshotEvent snapshot = await lateJoiner.ReceiveUntilAsync<HandSnapshotEvent>(new List<object>(), _ => true, timeout.Token);
			Assert.Empty(snapshot.MyCards);
			Assert.Equal(aliceTurn.TurnId, snapshot.CurrentTurn!.TurnId);
			Assert.Equal(1, snapshot.CurrentTurn.SeatIndex);

			IncomingMessage acted = await alice.RequestAsync(new TurnActRequest { TableId = TableId, TurnId = aliceTurn.TurnId, Action = PlayerAction.Fold }, timeout.Token);
			Assert.Equal(aliceTurn.TurnId, Assert.IsType<TurnActAcceptedResponse>(acted.Payload).TurnId);

			List<object> spectatorLog = new();
			await spectator.ReceiveUntilAsync<TurnActedEvent>(spectatorLog, turn => turn.TurnId == aliceTurn.TurnId, timeout.Token);
			Assert.Contains(spectatorLog, message => message is HandStartedEvent);
			Assert.DoesNotContain(spectatorLog, message => message is HoleCardsEvent);

			await lateJoiner.CloseAsync(timeout.Token);
			await alice.CloseAsync(timeout.Token);
			await spectator.CloseAsync(timeout.Token);
			await app.StopAsync(timeout.Token);
		}

		private static async Task<TestClient> JoinAsync(Uri uri, string name, CancellationToken cancellationToken)
		{
			TestClient client = new();
			await client.ConnectAsync(uri, cancellationToken);
			await client.HelloAsync(name, cancellationToken);
			await client.RequestAsync(new JoinTableRequest { TableId = TableId }, cancellationToken);
			return client;
		}
	}
}