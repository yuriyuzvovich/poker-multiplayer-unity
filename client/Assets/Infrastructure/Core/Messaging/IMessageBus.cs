using System;

namespace Poker.Client.Infrastructure.Core.Messaging
{
	public interface IMessageBus
	{
		public void Publish<TMessage>(TMessage message);

		public IDisposable Subscribe<TMessage>(Action<TMessage> handler);
	}
}