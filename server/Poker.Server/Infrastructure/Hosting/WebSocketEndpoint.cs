using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Time;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.Reliability;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Server.Infrastructure.Hosting
{
	public sealed class WebSocketEndpoint
	{
		private readonly MessageDispatcher _dispatcher;
		private readonly MessageCodec _codec;
		private readonly MessageRegistry _registry;
		private readonly SessionManager _sessions;
		private readonly IPlayerLocator _locator;
		private readonly ConnectionOptions _options;
		private readonly IClock _clock;
		private readonly IConnectionListener[] _listeners;
		private readonly ILogger<WebSocketEndpoint> _logger;

		public WebSocketEndpoint(
			MessageDispatcher dispatcher,
			MessageCodec codec,
			MessageRegistry registry,
			SessionManager sessions,
			IPlayerLocator locator,
			ConnectionOptions options,
			IClock clock,
			IEnumerable<IConnectionListener> listeners,
			ILogger<WebSocketEndpoint> logger)
		{
			_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
			_codec = codec ?? throw new ArgumentNullException(nameof(codec));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
			_locator = locator ?? throw new ArgumentNullException(nameof(locator));
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_listeners = listeners?.ToArray() ?? throw new ArgumentNullException(nameof(listeners));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
		{
			if (!context.WebSockets.IsWebSocketRequest)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				return;
			}

			using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
			SocketRun run = new();
			using CancellationTokenSource idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			try
			{
				await ReceiveLoopAsync(socket, run, idle, cancellationToken);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				_logger.LogInformation("No traffic for {Seconds} s from {PlayerName}; dropping the socket", _options.DeadSocketAfter.TotalSeconds, run.Session?.Player.Name);
				socket.Abort();
			}
			catch (OperationCanceledException) { }
			catch (WebSocketException exception)
			{
				_logger.LogInformation("Socket of {PlayerName} lost: {Reason}", run.Session?.Player.Name, exception.Message);
			}

			await EndRunAsync(socket, run);
		}

		private async Task ReceiveLoopAsync(WebSocket socket, SocketRun run, CancellationTokenSource idle, CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[4096];
			using MemoryStream packet = new();
			idle.CancelAfter(_options.DeadSocketAfter);
			while (socket.State == WebSocketState.Open)
			{
				ValueWebSocketReceiveResult result = await socket.ReceiveAsync(buffer.AsMemory(), idle.Token);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					await CloseOutputAsync(socket, run, WebSocketCloseStatus.NormalClosure, "", cancellationToken);
					run.IsGraceful = true;
					return;
				}

				if (result.MessageType == WebSocketMessageType.Text)
				{
					_logger.LogWarning("Text frame received; the protocol is binary");
					await CloseOutputAsync(socket, run, WebSocketCloseStatus.InvalidMessageType, "Binary packets only.", cancellationToken);
					return;
				}

				packet.Write(buffer, 0, result.Count);
				if (!result.EndOfMessage) continue;

				idle.CancelAfter(_options.DeadSocketAfter);
				ArraySegment<byte> bytes = new(packet.GetBuffer(), 0, (int)packet.Length);
				bool keepGoing = await HandlePacketAsync(socket, run, bytes, cancellationToken);
				packet.SetLength(0);
				if (!keepGoing) return;
			}
		}

		private async Task<bool> HandlePacketAsync(WebSocket socket, SocketRun run, ArraySegment<byte> packet, CancellationToken cancellationToken)
		{
			if (!PacketCodec.TryDecode(packet, out PacketHeader header, out _))
			{
				_logger.LogWarning("Dropped a truncated packet");
				return true;
			}

			if (run.Session == null)
			{
				run.Session = await HandshakeAsync(socket, packet, header, cancellationToken);
				return true;
			}

			ReceiveResult received = run.Session.Receive(header);
			if (received == ReceiveResult.Gap)
			{
				_logger.LogWarning("Sequence gap from {PlayerName}: got {Seq}", run.Session.Player.Name, header.Seq);
				await CloseOutputAsync(socket, run, (WebSocketCloseStatus)CloseCodes.SequenceGap, "Sequence gap.", cancellationToken);
				return false;
			}

			if (received == ReceiveResult.Duplicate) return true;
			if (received == ReceiveResult.Unnumbered && !IsSystemMessage(header))
			{
				_logger.LogWarning("Unnumbered game message from {PlayerName} dropped; only sys.* travel without a seq", run.Session.Player.Name);
				return true;
			}

			await _dispatcher.DispatchAsync(run.Session, packet, cancellationToken);
			return true;
		}

		private async Task<ClientSession?> HandshakeAsync(WebSocket socket, ArraySegment<byte> packet, PacketHeader header, CancellationToken cancellationToken)
		{
			if (_codec.Decode(packet, out IncomingMessage message) != DecodeStatus.Ok) return null;

			if (message.Payload is not HelloRequest hello)
			{
				if (header.RequestId == 0) return null;

				await SendUnnumberedAsync(socket, new ErrorResponse { Code = ProtocolErrorCodes.SessionRequired, Message = "Send sys.hello first." }, header.RequestId, cancellationToken);
				return null;
			}

			if (!HelloValidator.TryValidate(hello, out string name, out ErrorResponse error))
			{
				await SendUnnumberedAsync(socket, error, header.RequestId, cancellationToken);
				return null;
			}

			ClientSession? resumed = null;
			bool isResumed = hello.ResumeKey.Length > 0 && _sessions.TryResume(hello.ResumeKey, _clock.UtcNow, out resumed);
			ClientSession session = resumed ?? await StartSessionAsync(hello.DeviceKey, name, cancellationToken);
			WelcomeResponse welcome = new()
			{
				SessionId = session.Id,
				PlayerId = session.Player.PlayerId,
				PlayerName = session.Player.Name,
				ServerTimeUnixMs = _clock.UtcNow.ToUnixTimeMilliseconds(),
				ResumeKey = session.ResumeKey,
				Resumed = isResumed,
				HeartbeatMs = (int)_options.HeartbeatInterval.TotalMilliseconds,
				ZombieMs = (int)_options.ZombieWindow.TotalMilliseconds,
				MaxUnacked = _options.MaxUnacked,
				SeatedTableId = isResumed ? "" : _locator.FindSeatedTableId(session.Player.PlayerId),
			};
			int resent = session.Attach(socket, welcome, header.RequestId, header.Ack);
			_logger.LogInformation("{PlayerName} ({PlayerId}) {Action} session {SessionId}, resent {Resent} packets", session.Player.Name, session.Player.PlayerId, isResumed ? "resumed" : "started", session.Id, resent);
			return session;
		}

		private async Task<ClientSession> StartSessionAsync(string deviceKey, string name, CancellationToken cancellationToken)
		{
			PlayerIdentity player = _sessions.IdentityFor(deviceKey, name);
			if (_sessions.TryTakeOver(player.PlayerId, out ClientSession? replaced))
			{
				await replaced.CloseAsReplacedAsync();
				_logger.LogInformation("Session {SessionId} of {PlayerName} replaced by a new sign-in", replaced.Id, player.Name);
				foreach (IConnectionListener listener in _listeners)
				{
					await listener.OnReplacedAsync(replaced);
				}
			}

			return _sessions.Create(player);
		}

		private async Task EndRunAsync(WebSocket socket, SocketRun run)
		{
			ClientSession? session = run.Session;
			if (session == null) return;

			if (!run.IsGraceful)
			{
				if (!session.Detach(socket, _clock.UtcNow)) return;

				_logger.LogInformation("Session {SessionId} of {PlayerName} waits {Seconds} s for a resume", session.Id, session.Player.Name, _options.ZombieWindow.TotalSeconds);
				return;
			}

			if (!session.IsAttachedTo(socket)) return;

			_sessions.End(session);
			_logger.LogInformation("Session {SessionId} of {PlayerName} closed by the client", session.Id, session.Player.Name);
			foreach (IConnectionListener listener in _listeners)
			{
				await listener.OnDisconnectedAsync(session);
			}
		}

		// Once a session owns the socket, its outbox serializes the close with any queued sends.
		private static Task CloseOutputAsync(WebSocket socket, SocketRun run, WebSocketCloseStatus status, string description, CancellationToken cancellationToken) =>
			run.Session != null ? run.Session.CloseAsync(socket, status, description, cancellationToken) : socket.CloseOutputAsync(status, description, cancellationToken);

		private bool IsSystemMessage(in PacketHeader header) => _registry.TryGet(header.TypeId, out MessageType? messageType) && ProtocolMessages.IsUnnumbered(messageType);

		private Task SendUnnumberedAsync<TMessage>(WebSocket socket, TMessage message, ushort requestId, CancellationToken cancellationToken) where TMessage : class =>
			socket.SendAsync(new ArraySegment<byte>(_codec.Encode(message, requestId: requestId)), WebSocketMessageType.Binary, true, cancellationToken);

		private sealed class SocketRun
		{
			public ClientSession? Session { get; set; }
			public bool IsGraceful { get; set; }
		}
	}
}