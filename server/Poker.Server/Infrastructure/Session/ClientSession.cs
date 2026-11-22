using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.Reliability;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Server.Infrastructure.Session
{
	public sealed class ClientSession : IClientConnection
	{
		private readonly MessageCodec _codec;
		private readonly MessageRegistry _registry;
		private readonly ReliableChannel _channel;
		private readonly int _maxUnacked;
		private readonly int _outboxCapacity;
		private readonly object _lock = new object();
		private readonly List<byte[]> _resend = new();
		private SocketOutbox? _outbox;
		private volatile bool _isBroken;

		public ClientSession(string id, PlayerIdentity player, string resumeKey, MessageCodec codec, MessageRegistry registry, ConnectionOptions options)
		{
			Id = id;
			Player = player ?? throw new ArgumentNullException(nameof(player));
			ResumeKey = resumeKey;
			_codec = codec ?? throw new ArgumentNullException(nameof(codec));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_channel = new ReliableChannel(options.ResendCapacity);
			_maxUnacked = options.MaxUnacked;
			_outboxCapacity = options.OutboxCapacity;
		}

		public string Id { get; }
		public PlayerIdentity Player { get; }
		public string ResumeKey { get; private set; }
		public DateTimeOffset? DetachedAt { get; private set; }
		public bool IsBroken => _isBroken;

		PlayerIdentity? IClientConnection.Player => Player;

		public Task SendAsync<TMessage>(TMessage message, ushort requestId, CancellationToken cancellationToken) where TMessage : class
		{
			Enqueue(_codec.Encode(message, requestId: requestId), message.GetType());
			return Task.CompletedTask;
		}

		public void Send(OutgoingMessage message) => Enqueue(message.CopyPacket(_codec), message.Message.GetType());

		public ReceiveResult Receive(in PacketHeader header)
		{
			lock (_lock)
			{
				ReceiveResult result = _channel.Receive(header);
				if (result != ReceiveResult.Accepted) return result;
				if (_channel.ReceivedSinceAck < _maxUnacked) return result;

				byte[] ack = _codec.Encode(new AckMessage());
				_channel.StampUnnumbered(ack);
				EnqueueLocked(ack);
				return result;
			}
		}

		public int Attach(WebSocket socket, WelcomeResponse welcome, ushort requestId, ushort peerAck)
		{
			lock (_lock)
			{
				SocketOutbox? previous = _outbox;
				_outbox = new SocketOutbox(socket, _outboxCapacity);
				DetachedAt = null;
				if (previous != null)
				{
					previous.Complete();
					if (previous.Socket != socket) previous.Socket.Abort();
				}

				byte[] packet = _codec.Encode(welcome, requestId: requestId);
				_channel.StampUnnumbered(packet);
				EnqueueLocked(packet);
				if (!welcome.Resumed) return 0;

				_resend.Clear();
				_channel.CollectResend(peerAck, _resend);
				foreach (byte[] resend in _resend)
				{
					EnqueueLocked(resend);
				}

				int resent = _resend.Count;
				_resend.Clear();
				return resent;
			}
		}

		public bool Detach(WebSocket socket, DateTimeOffset now)
		{
			lock (_lock)
			{
				if (_outbox == null || _outbox.Socket != socket) return false;

				_outbox.Complete();
				_outbox = null;
				DetachedAt = now;
				return true;
			}
		}

		public bool IsAttachedTo(WebSocket socket)
		{
			lock (_lock) return _outbox != null && _outbox.Socket == socket;
		}

		public Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string description, CancellationToken cancellationToken)
		{
			SocketOutbox? outbox;
			lock (_lock) outbox = _outbox != null && _outbox.Socket == socket ? _outbox : null;

			return outbox != null ? outbox.CloseAsync(status, description) : socket.CloseOutputAsync(status, description, cancellationToken);
		}

		public Task CloseAsReplacedAsync()
		{
			SocketOutbox? outbox;
			lock (_lock)
			{
				outbox = _outbox;
				_outbox = null;
			}

			return outbox != null ? outbox.CloseAsync((WebSocketCloseStatus)CloseCodes.DuplicateLogin, "Signed in on another device.") : Task.CompletedTask;
		}

		internal void RotateResumeKey(string resumeKey) => ResumeKey = resumeKey;

		private void Enqueue(byte[] packet, Type messageType)
		{
			bool isNumbered = !ProtocolMessages.IsUnnumbered(_registry.Get(messageType));
			lock (_lock)
			{
				if (!isNumbered)
				{
					_channel.StampUnnumbered(packet);
				}
				else if (!_channel.TryStamp(packet))
				{
					_isBroken = true;
					_outbox?.Socket.Abort();
					return;
				}

				EnqueueLocked(packet);
			}
		}

		private void EnqueueLocked(byte[] packet)
		{
			// Detached or closing: numbered packets wait in the resend buffer for a resume.
			if (_outbox == null || _outbox.IsClosed) return;
			if (_outbox.TryEnqueue(packet)) return;

			// The client stopped reading. Drop the socket; it resumes and gets the backlog from the resend buffer.
			_outbox.Complete();
			_outbox.Socket.Abort();
		}
	}
}