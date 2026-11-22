using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Network.Clock;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Messaging;
using Poker.Client.Infrastructure.Network.Reliability;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.Reliability;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Debug = UnityEngine.Debug;

namespace Poker.Client.Infrastructure.Network.Connection
{
	// Owns the socket and the order of writes. Numbering lives in ReliableLink, request matching in PendingRequests,
	// delivery to presenters in MessageRouter, latency and blackouts in LinkConditioner, debug capture in packet taps.
	public sealed class ServerConnection : IServerConnection, IDisposable
	{
		private readonly MessageCodec _codec;
		private readonly MessageRegistry _registry;
		private readonly NetworkSettings _settings;
		private readonly NetworkStats _stats;
		private readonly IReadOnlyList<IPacketTap> _taps;
		private readonly PendingRequests _pending = new();
		private readonly MessageRouter _router = new();
		private readonly ReliableLink _reliable = new();
		private readonly HashSet<ushort> _unnumberedRequests = new();
		private readonly SemaphoreSlim _sendGate = new(1, 1);
		private readonly List<byte[]> _resend = new();
		private readonly LinkConditioner _conditioner;
		private WebSocketConnection _socket;

		public ServerConnection(MessageCodec codec, MessageRegistry registry, NetworkSettings settings, NetworkStats stats, IReadOnlyList<IPacketTap> taps)
		{
			_codec = codec ?? throw new ArgumentNullException(nameof(codec));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_stats = stats ?? throw new ArgumentNullException(nameof(stats));
			_taps = taps ?? throw new ArgumentNullException(nameof(taps));
			_conditioner = new LinkConditioner(settings.SimulatedRoundTrip);
			_conditioner.Bind(OnPacketReceived);
		}

		public event Action<string> Closed;

		public bool IsOpen => _socket is { IsOpen: true };

		public bool IsEstablished => _reliable.IsEstablished;

		public string ResumeKey => _reliable.ResumeKey;

		public TimeSpan HeartbeatInterval => _reliable.HeartbeatInterval;

		public long LastPacketAtUnixMs { get; private set; }

		public int LastCloseCode { get; private set; }

		public async UniTask ConnectAsync(CancellationToken cancellationToken)
		{
			ReleaseSocket();
			if (_conditioner.IsBlackedOut) throw new WebSocketException("Simulated network blackout.");

			LastCloseCode = 0;
			_socket = new WebSocketConnection(_stats);
			_socket.PacketReceived += _conditioner.Receive;
			_socket.Closed += OnClosed;
			await _socket.ConnectAsync(_settings.ServerUri, cancellationToken);
			LastPacketAtUnixMs = ServerClock.LocalNowUnixMs;
		}

		public UniTask SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class
		{
			byte[] packet = Stamp(message, 0, out bool isNumbered);
			return SendPacketAsync(packet, isNumbered, cancellationToken);
		}

		public async UniTask<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
			where TRequest : class
			where TResponse : class
		{
			ushort requestId = _pending.Register();
			try
			{
				byte[] packet = Stamp(request, requestId, out bool isNumbered);
				if (!isNumbered) _unnumberedRequests.Add(requestId);

				try
				{
					await SendPacketAsync(packet, isNumbered, cancellationToken);
				}
				catch
				{
					_pending.Cancel(requestId);
					throw;
				}

				IncomingMessage response = await _pending.WaitAsync(requestId, _settings.RequestTimeout, cancellationToken);
				if (response.Payload is ErrorResponse error) throw new RequestRejectedException(error.Code, error.Message);
				if (response.Payload is TResponse typed) return typed;

				throw new InvalidOperationException("Expected " + typeof(TResponse).Name + " for request " + requestId + ", got " + response.Type + ".");
			}
			finally
			{
				_unnumberedRequests.Remove(requestId);
			}
		}

		public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class => _router.Subscribe(handler);

		public void Drop(string reason)
		{
			Debug.LogWarning("[NET] Dropping the socket: " + reason);
			ReleaseSocket();
			OnClosed(reason);
		}

		public void SimulateDrop(TimeSpan blackout)
		{
			_conditioner.BlackOut(blackout);
			Drop("simulated network drop for " + blackout.TotalSeconds + " s");
		}

		public void Dispose()
		{
			_pending.FailAll(new ConnectionClosedException("client shut down"));
			_conditioner.Dispose();
			if (_socket == null) return;

			_socket.PacketReceived -= _conditioner.Receive;
			_socket.Closed -= OnClosed;
			CloseAndDisposeAsync(_socket).Forget();
			_socket = null;
		}

		private byte[] Stamp<TMessage>(TMessage message, ushort requestId, out bool isNumbered) where TMessage : class
		{
			byte[] packet = _codec.Encode(message, requestId: requestId);
			isNumbered = _reliable.Stamp(packet, _registry.Get(message.GetType()));
			return packet;
		}

		private async UniTask SendPacketAsync(byte[] packet, bool isNumbered, CancellationToken cancellationToken)
		{
			WebSocketConnection socket = _socket;
			if (socket is not { IsOpen: true })
			{
				if (isNumbered) return;

				throw new ConnectionClosedException("not connected");
			}

			DateTime dueAt = _conditioner.OutgoingDueAt();
			await _sendGate.WaitAsync(cancellationToken);
			try
			{
				await _conditioner.WaitUntilAsync(dueAt, cancellationToken);
				if (socket != _socket || !socket.IsOpen)
				{
					if (isNumbered) return;

					throw new ConnectionClosedException("the connection was replaced");
				}

				_stats.AddSent(packet.Length);
				await socket.SendAsync(packet, cancellationToken);
			}
			catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException)
			{
				if (isNumbered) return;

				throw new ConnectionClosedException(exception.Message);
			}
			finally
			{
				_sendGate.Release();
			}
		}

		private void OnPacketReceived(ArraySegment<byte> packet)
		{
			LastPacketAtUnixMs = ServerClock.LocalNowUnixMs;
			_stats.AddReceived(packet.Count);
			IncomingMessage message;
			DecodeStatus status;
			try
			{
				status = _codec.Decode(packet, out message);
			}
			catch (PayloadFormatException exception)
			{
				Debug.LogWarning("[NET] Invalid payload: " + exception.Message);
				return;
			}

			if (status != DecodeStatus.Ok)
			{
				Debug.LogWarning("[NET] Dropped packet: " + status);
				return;
			}

			ReceiveResult received = _reliable.Receive(message.Header);
			if (received == ReceiveResult.Gap)
			{
				Drop("sequence gap: got " + message.Header.Seq + " after " + _reliable.LastReceived);
				return;
			}

			if (received == ReceiveResult.Duplicate) return;

			Tap(message, packet);
			if (message.Payload is WelcomeResponse welcome) ApplyWelcome(welcome, message.Header);

			long handlingStartedAt = Stopwatch.GetTimestamp();
			Deliver(message);
			_stats.AddHandled(message.Type.Name, packet.Count, NetworkStats.MillisecondsSince(handlingStartedAt));
			if (!_reliable.IsAckDue) return;

			SendAckAsync().Forget();
		}

		private void Tap(IncomingMessage message, ArraySegment<byte> packet)
		{
			if (_taps.Count == 0) return;
			if (!PacketCodec.TryDecode(packet, out _, out ArraySegment<byte> payload)) return;

			for (int i = 0; i < _taps.Count; i++)
			{
				IPacketTap tap = _taps[i];
				tap.OnReceived(message.Type, message.Header.RequestId, payload);
			}
		}

		private void Deliver(IncomingMessage message)
		{
			if (_pending.TryComplete(message)) return;

			_router.Dispatch(message.Type.ClrType, message.Payload);
		}

		private void ApplyWelcome(WelcomeResponse welcome, PacketHeader header)
		{
			_resend.Clear();
			WelcomeKind kind = _reliable.ApplyWelcome(welcome, header, _resend);
			if (kind == WelcomeKind.Resumed)
			{
				ResendAsync(_resend.ToArray()).Forget();
				_resend.Clear();
				return;
			}

			if (kind != WelcomeKind.Restarted) return;

			_pending.FailAllExcept(header.RequestId, new ConnectionClosedException("the server started a new session"));
		}

		private async UniTaskVoid ResendAsync(byte[][] packets)
		{
			Debug.Log("[NET] Resending " + packets.Length + " unacknowledged packets");
			_stats.AddResent(packets.Length);
			foreach (byte[] packet in packets)
			{
				await SendPacketAsync(packet, true, CancellationToken.None);
			}
		}

		private async UniTaskVoid SendAckAsync()
		{
			byte[] ack = Stamp(new AckMessage(), 0, out bool isNumbered);
			try
			{
				await SendPacketAsync(ack, isNumbered, CancellationToken.None);
			}
			catch (ConnectionClosedException) { }
		}

		private void ReleaseSocket()
		{
			_conditioner.Reset();
			if (_socket == null) return;

			_socket.PacketReceived -= _conditioner.Receive;
			_socket.Closed -= OnClosed;
			_socket.Dispose();
			_socket = null;
		}

		private static async UniTaskVoid CloseAndDisposeAsync(WebSocketConnection socket)
		{
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
			try
			{
				await socket.CloseAsync(timeout.Token);
			}
			catch (OperationCanceledException) { }
			catch (WebSocketException) { }

			socket.Dispose();
		}

		private void OnClosed(string reason)
		{
			LastCloseCode = _socket?.CloseCode ?? 0;
			if (_unnumberedRequests.Count > 0)
			{
				ushort[] requestIds = new ushort[_unnumberedRequests.Count];
				_unnumberedRequests.CopyTo(requestIds);
				foreach (ushort requestId in requestIds)
				{
					_pending.Fail(requestId, new ConnectionClosedException(reason));
				}
			}

			Closed?.Invoke(reason);
		}
	}
}