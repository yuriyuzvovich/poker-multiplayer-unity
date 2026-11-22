using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared;
using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Infrastructure.Session
{
	public sealed class ClientSessionTests
	{
		private readonly MessageRegistry _registry = MessageCatalog.CreateRegistry();
		private readonly CountingSerializer _serializer = new();
		private readonly MessageCodec _codec;

		public ClientSessionTests() => _codec = new MessageCodec(_registry, _serializer);

		[Fact]
		public void SendAsync_WhileThePeerStoppedReading_ReturnsAtOnce()
		{
			StalledWebSocket socket = new();
			ClientSession session = Attach(socket);

			for (int i = 0; i < 50; i++)
			{
				Task sent = session.SendAsync(new BalanceChangedEvent { Balance = i }, 0, CancellationToken.None);

				Assert.True(sent.IsCompletedSuccessfully);
			}

			Assert.False(session.IsBroken);
			Assert.False(socket.WasAborted);
		}

		[Fact]
		public async Task CloseAsync_WritesTheQueuedPacketsFirst_InSequenceOrder()
		{
			StalledWebSocket socket = new() { IsStalled = false };
			ClientSession session = Attach(socket);
			await session.SendAsync(new BalanceChangedEvent { Balance = 1 }, 0, CancellationToken.None);
			await session.SendAsync(new BalanceChangedEvent { Balance = 2 }, 0, CancellationToken.None);

			await session.CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

			Assert.Equal(3, socket.Sent.Count);
			Assert.True(PacketCodec.TryDecode(new ArraySegment<byte>(socket.Sent[1]), out PacketHeader first, out _));
			Assert.True(PacketCodec.TryDecode(new ArraySegment<byte>(socket.Sent[2]), out PacketHeader second, out _));
			Assert.Equal(1, first.Seq);
			Assert.Equal(2, second.Seq);
			Assert.Equal(WebSocketState.CloseSent, socket.State);
		}

		[Fact]
		public void FullResendBuffer_BreaksTheSession_AndReleasesTheStalledSend()
		{
			StalledWebSocket socket = new();
			ClientSession session = Attach(socket, new ConnectionOptions { ResendCapacity = 4 });

			for (int i = 0; i < 5; i++)
			{
				session.SendAsync(new BalanceChangedEvent { Balance = i }, 0, CancellationToken.None);
			}

			Assert.True(session.IsBroken);
			Assert.True(socket.WasAborted);
		}

		[Fact]
		public async Task Send_OneBroadcast_SerializesThePayloadOnceForEveryReceiver()
		{
			StalledWebSocket aliceSocket = new() { IsStalled = false };
			StalledWebSocket bobSocket = new() { IsStalled = false };
			ClientSession alice = Attach(aliceSocket);
			ClientSession bob = Attach(bobSocket);
			OutgoingMessage broadcast = new(new BalanceChangedEvent { Balance = 7 });

			alice.Send(broadcast);
			bob.Send(broadcast);
			await alice.CloseAsync(aliceSocket, WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
			await bob.CloseAsync(bobSocket, WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

			Assert.Equal(1, _serializer.CountOf(typeof(BalanceChangedEvent)));
			Assert.Equal(2, aliceSocket.Sent.Count);
			Assert.Equal(2, bobSocket.Sent.Count);
		}

		private ClientSession Attach(WebSocket socket, ConnectionOptions? options = null)
		{
			ClientSession session = new("s-" + Guid.NewGuid().ToString("N"), new PlayerIdentity("p-1", "Alice"), "key", _codec, _registry, options ?? new ConnectionOptions());
			session.Attach(socket, new WelcomeResponse(), 1, 0);
			return session;
		}

		private sealed class CountingSerializer : IPayloadSerializer
		{
			private readonly JsonPayloadSerializer _inner = new();
			private readonly Dictionary<Type, int> _counts = new();

			public int CountOf(Type type)
			{
				lock (_counts) return _counts.TryGetValue(type, out int count) ? count : 0;
			}

			public byte[] Serialize(object payload, Type type)
			{
				lock (_counts) _counts[type] = CountOf(type) + 1;

				return _inner.Serialize(payload, type);
			}

			public object Deserialize(ArraySegment<byte> payload, Type type) => _inner.Deserialize(payload, type);
		}
	}
}