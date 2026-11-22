using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Poker.Server.Features.Lobby;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Lobby.Models;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Lobby
{
	public sealed class ListTablesHandlerTests
	{
		[Fact]
		public async Task HandleAsync_RespondsWithEveryTableAndItsOccupancy()
		{
			RecordingConnection connection = new(new PlayerIdentity("p-1", "Viewer"));
			TableDirectory directory = new(new ManualClock(DateTimeOffset.UnixEpoch), NullLoggerFactory.Instance, Array.Empty<ITableEventListener>(), new UnlimitedChipBank());
			IMessageHandler handler = new ListTablesHandler(directory);

			await handler.HandleAsync(new MessageContext(connection, 3), new ListTablesRequest(), CancellationToken.None);

			SentMessage sent = Assert.Single(connection.Sent);
			Assert.Equal(3, sent.RequestId);
			TableListResponse response = Assert.IsType<TableListResponse>(sent.Message);
			Assert.Equal(2, response.Tables.Length);

			TableSummary practice = response.Tables[0];
			Assert.Equal(TableSeed.PracticeTableId, practice.TableId);
			Assert.Equal(6, practice.SeatCount);
			Assert.Equal(3, practice.OccupiedSeats);
			Assert.Equal(10, practice.SmallBlind);
			Assert.Equal(20, practice.BigBlind);

			TableSummary headsUp = response.Tables[1];
			Assert.Equal(TableSeed.HeadsUpTableId, headsUp.TableId);
			Assert.Equal(2, headsUp.SeatCount);
			Assert.Equal(1, headsUp.OccupiedSeats);
		}
	}
}