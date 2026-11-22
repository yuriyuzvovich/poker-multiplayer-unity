using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Poker.Client.Infrastructure.Network.Connection
{
	public interface IServerConnection
	{
		public UniTask<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
			where TRequest : class
			where TResponse : class;

		public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class;
	}
}