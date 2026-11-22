using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Server.Infrastructure.Messaging
{
	public sealed class MessageDispatcher
	{
		private readonly Dictionary<Type, IMessageHandler> _handlers = new();
		private readonly MessageCodec _codec;
		private readonly ILogger<MessageDispatcher> _logger;

		public MessageDispatcher(IEnumerable<IMessageHandler> handlers, MessageCodec codec, ILogger<MessageDispatcher> logger)
		{
			if (handlers == null) throw new ArgumentNullException(nameof(handlers));

			_codec = codec ?? throw new ArgumentNullException(nameof(codec));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			foreach (IMessageHandler handler in handlers)
			{
				_handlers.Add(handler.MessageType, handler);
			}
		}

		public Task DispatchAsync(IClientConnection connection, ArraySegment<byte> packet, CancellationToken cancellationToken)
		{
			IncomingMessage message;
			DecodeStatus status;
			try
			{
				status = _codec.Decode(packet, out message);
			}
			catch (PayloadFormatException exception)
			{
				_logger.LogWarning(exception, "Invalid payload from {ConnectionId}", connection.Id);
				return Task.CompletedTask;
			}

			if (status != DecodeStatus.Ok)
			{
				_logger.LogWarning("Dropped packet from {ConnectionId}: {Status}", connection.Id, status);
				return Task.CompletedTask;
			}

			if (!_handlers.TryGetValue(message.Type.ClrType, out IMessageHandler? handler))
			{
				_logger.LogWarning("No handler for {MessageType} from {ConnectionId}", message.Type, connection.Id);
				return Task.CompletedTask;
			}

			MessageContext context = new(connection, message.Header.RequestId);
			if (connection.Player == null)
			{
				_logger.LogWarning("{MessageType} before sys.hello from {ConnectionId}", message.Type, connection.Id);
				if (context.RequestId == 0) return Task.CompletedTask;

				return context.RejectAsync(ProtocolErrorCodes.SessionRequired, "Send sys.hello first.", cancellationToken);
			}

			_logger.LogDebug("IN {MessageType} rid={RequestId} from {ConnectionId}", message.Type, message.Header.RequestId, connection.Id);
			return handler.HandleAsync(context, message.Payload, cancellationToken);
		}
	}
}