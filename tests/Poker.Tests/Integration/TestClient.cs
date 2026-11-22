using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Reliability;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Xunit;

namespace Poker.Tests.Integration
{
	public sealed class TestClient : IDisposable
	{
		private readonly MessageRegistry _registry = MessageCatalog.CreateRegistry();
		private readonly MessageCodec _codec;
		private ReliableChannel _channel = new();
		private ClientWebSocket _socket = new();
		private bool _isEstablished;
		private ushort _lastRequestId;

		public TestClient() => _codec = new MessageCodec(_registry, new JsonPayloadSerializer());

		public ClientWebSocket Socket => _socket;

		public string ResumeKey { get; private set; } = "";

		public string Name { get; private set; } = "";

		public ushort LastReceived => _channel.LastReceived;

		public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => _socket.ConnectAsync(uri, cancellationToken);

		public Task<WelcomeResponse> HelloAsync(string name, CancellationToken cancellationToken) => HelloAsync(name, "", cancellationToken);

		public async Task<WelcomeResponse> HelloAsync(string name, string deviceKey, CancellationToken cancellationToken)
		{
			Name = name;
			IncomingMessage reply = await RequestAsync(new HelloRequest { ProtocolVersion = ProtocolInfo.Version, PlayerName = name, DeviceKey = deviceKey }, cancellationToken);
			WelcomeResponse welcome = Assert.IsType<WelcomeResponse>(reply.Payload);
			ResumeKey = welcome.ResumeKey;
			_isEstablished = true;
			return welcome;
		}

		public async Task<WelcomeResponse> ReconnectAsync(Uri uri, CancellationToken cancellationToken)
		{
			_socket.Dispose();
			_socket = new ClientWebSocket();
			await _socket.ConnectAsync(uri, cancellationToken);
			ushort requestId = ++_lastRequestId;
			await SendAsync(new HelloRequest { ProtocolVersion = ProtocolInfo.Version, PlayerName = Name, ResumeKey = ResumeKey }, requestId, cancellationToken);
			IncomingMessage reply = await ReceiveResponseAsync(requestId, cancellationToken);
			WelcomeResponse welcome = Assert.IsType<WelcomeResponse>(reply.Payload);
			ResumeKey = welcome.ResumeKey;
			if (!welcome.Resumed)
			{
				_channel = new ReliableChannel();
				return welcome;
			}

			List<byte[]> resend = new();
			_channel.CollectResend(reply.Header.Ack, resend);
			foreach (byte[] packet in resend)
			{
				await SendRawAsync(packet, cancellationToken);
			}

			return welcome;
		}

		public async Task<IncomingMessage> RequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken) where TRequest : class
		{
			ushort requestId = ++_lastRequestId;
			await SendAsync(request, requestId, cancellationToken);
			return await ReceiveResponseAsync(requestId, cancellationToken);
		}

		public async Task<IncomingMessage> ReceiveResponseAsync(ushort requestId, CancellationToken cancellationToken)
		{
			while (true)
			{
				IncomingMessage message = await ReceiveAsync(cancellationToken);
				if (message.Header.RequestId != requestId) continue;

				return message;
			}
		}

		public Task SendAsync<TMessage>(TMessage message, ushort requestId, CancellationToken cancellationToken) where TMessage : class =>
			SendRawAsync(Stamp(message, requestId), cancellationToken);

		public ushort StampWithoutSending<TMessage>(TMessage message) where TMessage : class
		{
			ushort requestId = ++_lastRequestId;
			Stamp(message, requestId);
			return requestId;
		}

		public byte[] Stamp<TMessage>(TMessage message, ushort requestId) where TMessage : class
		{
			byte[] packet = _codec.Encode(message, requestId: requestId);
			if (_isEstablished && !ProtocolMessages.IsUnnumbered(_registry.Get(message.GetType()))) _channel.TryStamp(packet); else _channel.StampUnnumbered(packet);

			return packet;
		}

		public Task SendRawAsync(byte[] packet, CancellationToken cancellationToken) =>
			_socket.SendAsync(new ArraySegment<byte>(packet), WebSocketMessageType.Binary, true, cancellationToken);

		public async Task<TEvent> ReceiveEventAsync<TEvent>(CancellationToken cancellationToken) where TEvent : class
		{
			while (true)
			{
				IncomingMessage message = await ReceiveAsync(cancellationToken);
				if (message.Header.RequestId != 0) continue;
				if (message.Payload is not TEvent typed) continue;

				return typed;
			}
		}

		public async Task<TEvent> ReceiveUntilAsync<TEvent>(List<object> log, Func<TEvent, bool> match, CancellationToken cancellationToken) where TEvent : class
		{
			while (true)
			{
				IncomingMessage message = await ReceiveAsync(cancellationToken);
				log.Add(message.Payload);
				if (message.Payload is not TEvent typed) continue;
				if (!match(typed)) continue;

				return typed;
			}
		}

		public async Task<IncomingMessage> ReceiveAsync(CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[4096];
			using MemoryStream packet = new();
			while (true)
			{
				WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
				Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
				packet.Write(buffer, 0, result.Count);
				if (!result.EndOfMessage) continue;

				Assert.Equal(DecodeStatus.Ok, _codec.Decode(new ArraySegment<byte>(packet.ToArray()), out IncomingMessage message));
				packet.SetLength(0);
				ReceiveResult received = _channel.Receive(message.Header);
				Assert.True(received != ReceiveResult.Gap, "Sequence gap: got " + message.Header.Seq + " after " + _channel.LastReceived);
				if (received == ReceiveResult.Duplicate) continue;

				return message;
			}
		}

		public void Abort() => _socket.Abort();

		public Task CloseAsync(CancellationToken cancellationToken) => _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);

		public void Dispose() => _socket.Dispose();
	}
}