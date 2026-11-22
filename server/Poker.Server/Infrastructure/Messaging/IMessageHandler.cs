using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poker.Server.Infrastructure.Messaging
{
	public interface IMessageHandler
	{
		public Type MessageType { get; }

		public Task HandleAsync(MessageContext context, object message, CancellationToken cancellationToken);
	}
}