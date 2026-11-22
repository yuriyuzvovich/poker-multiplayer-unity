using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Server.Infrastructure.Messaging
{
	public readonly struct MessageContext
	{
		public MessageContext(IClientConnection connection, ushort requestId)
		{
			Connection = connection;
			RequestId = requestId;
		}

		public IClientConnection Connection { get; }
		public ushort RequestId { get; }

		public Task RespondAsync<TResponse>(TResponse response, CancellationToken cancellationToken) where TResponse : class =>
			Connection.SendAsync(response, RequestId, cancellationToken);

		public Task RejectAsync(string code, string message, CancellationToken cancellationToken) =>
			RespondAsync(new ErrorResponse { Code = code, Message = message }, cancellationToken);
	}
}