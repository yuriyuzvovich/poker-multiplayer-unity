using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Poker.Server.Features.Lobby;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Hosting;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared;
using Poker.Shared.Features.Lobby;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Infrastructure.Messaging
{
	public sealed class MessageDispatcherTests
	{
		private readonly MessageCodec _codec = new(MessageCatalog.CreateRegistry(), new JsonPayloadSerializer());
		private readonly ManualClock _clock = new(DateTimeOffset.UnixEpoch);

		[Fact]
		public async Task DispatchAsync_KnownMessage_RoutesToItsHandlerWithTheRequestId()
		{
			RecordingConnection connection = new(new PlayerIdentity("p-1", "Alice"));

			await DispatchAsync(connection, _codec.Encode(new ListTablesRequest(), requestId: 41));

			SentMessage sent = Assert.Single(connection.Sent);
			Assert.IsType<TableListResponse>(sent.Message);
			Assert.Equal(41, sent.RequestId);
		}

		[Fact]
		public async Task DispatchAsync_RequestBeforeHello_IsRejectedWithSessionRequired()
		{
			RecordingConnection connection = new();

			await DispatchAsync(connection, _codec.Encode(new ListTablesRequest(), requestId: 5));

			SentMessage sent = Assert.Single(connection.Sent);
			Assert.Equal(5, sent.RequestId);
			Assert.Equal(ProtocolErrorCodes.SessionRequired, Assert.IsType<ErrorResponse>(sent.Message).Code);
		}

		[Fact]
		public async Task DispatchAsync_UnknownTypeId_IsDropped()
		{
			RecordingConnection connection = new(new PlayerIdentity("p-1", "Alice"));

			await DispatchAsync(connection, PacketCodec.Encode(new PacketHeader(0, 0, 12345, 0, 0, PacketFlags.None), ReadOnlySpan<byte>.Empty));

			Assert.Empty(connection.Sent);
		}

		[Fact]
		public async Task DispatchAsync_InvalidPayload_IsDropped()
		{
			RecordingConnection connection = new(new PlayerIdentity("p-1", "Alice"));
			uint typeId = MessageTypeId.Compute(LobbyMessageNames.ListTables);

			await DispatchAsync(connection, PacketCodec.Encode(new PacketHeader(0, 0, typeId, 0, 0, PacketFlags.None), Encoding.UTF8.GetBytes("{broken")));

			Assert.Empty(connection.Sent);
		}

		[Fact]
		public async Task DispatchAsync_TruncatedPacket_IsDropped()
		{
			RecordingConnection connection = new(new PlayerIdentity("p-1", "Alice"));

			await DispatchAsync(connection, new byte[5]);

			Assert.Empty(connection.Sent);
		}

		private Task DispatchAsync(RecordingConnection connection, byte[] packet)
		{
			TableDirectory directory = new(_clock, NullLoggerFactory.Instance, Array.Empty<ITableEventListener>(), new UnlimitedChipBank());
			IMessageHandler[] handlers =
			{
				new ListTablesHandler(directory),
			};
			MessageDispatcher dispatcher = new(handlers, _codec, NullLogger<MessageDispatcher>.Instance);
			return dispatcher.DispatchAsync(connection, new ArraySegment<byte>(packet), CancellationToken.None);
		}
	}
}