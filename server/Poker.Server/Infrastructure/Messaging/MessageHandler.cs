using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poker.Server.Infrastructure.Messaging
{
	public abstract class MessageHandler<TMessage> : IMessageHandler where TMessage : class
	{
		public Type MessageType => typeof(TMessage);

		Task IMessageHandler.HandleAsync(MessageContext context, object message, CancellationToken cancellationToken) =>
			HandleAsync(context, (TMessage)message, cancellationToken);

		protected abstract Task HandleAsync(MessageContext context, TMessage message, CancellationToken cancellationToken);
	}
}