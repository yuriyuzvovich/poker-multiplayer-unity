using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Network.Connection;

namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class FakeServerConnection : IServerConnection
	{
		private readonly Dictionary<Type, Action<object>> _handlers = new();

		public List<object> Requests { get; } = new();

		public Dictionary<Type, object> Responses { get; } = new();

		// Requests of these types fail with the given exception, keyed by the request type.
		public Dictionary<Type, Exception> Failures { get; } = new();

		public UniTask<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
			where TRequest : class
			where TResponse : class
		{
			Requests.Add(request);
			if (Failures.TryGetValue(typeof(TRequest), out Exception failure)) return UniTask.FromException<TResponse>(failure);

			if (Responses.TryGetValue(typeof(TResponse), out object response)) return UniTask.FromResult((TResponse)response);

			return UniTask.FromResult((TResponse)Activator.CreateInstance(typeof(TResponse)));
		}

		public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
		{
			Action<object> untyped = message => handler((TMessage)message);
			_handlers.TryGetValue(typeof(TMessage), out Action<object> existing);
			_handlers[typeof(TMessage)] = existing + untyped;
			return new Unsubscriber(this, typeof(TMessage), untyped);
		}

		public void Deliver(object message)
		{
			if (!_handlers.TryGetValue(message.GetType(), out Action<object> handlers)) return;

			handlers.Invoke(message);
		}

		private sealed class Unsubscriber : IDisposable
		{
			private readonly FakeServerConnection _owner;
			private readonly Type _type;
			private readonly Action<object> _handler;

			public Unsubscriber(FakeServerConnection owner, Type type, Action<object> handler)
			{
				_owner = owner;
				_type = type;
				_handler = handler;
			}

			public void Dispose()
			{
				if (!_owner._handlers.TryGetValue(_type, out Action<object> existing)) return;

				_owner._handlers[_type] = existing - _handler;
			}
		}
	}
}