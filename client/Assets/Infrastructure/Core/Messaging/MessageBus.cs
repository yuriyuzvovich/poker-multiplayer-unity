using System;
using System.Collections.Generic;
using UnityEngine;

namespace Poker.Client.Infrastructure.Core.Messaging
{
	public sealed class MessageBus : IMessageBus
	{
		private readonly Dictionary<Type, Delegate[]> _handlers = new();

		public void Publish<TMessage>(TMessage message)
		{
			if (!_handlers.TryGetValue(typeof(TMessage), out Delegate[] handlers)) return;

			// Subscribing replaces the array instead of changing it, so a handler may unsubscribe while this runs.
			foreach (Delegate handler in handlers)
			{
				// One broken subscriber must not keep the message from the others.
				try { ((Action<TMessage>)handler).Invoke(message); }
				catch (Exception exception) { Debug.LogException(exception); }
			}
		}

		public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
		{
			if (handler == null) throw new ArgumentNullException(nameof(handler));

			_handlers.TryGetValue(typeof(TMessage), out Delegate[] existing);
			_handlers[typeof(TMessage)] = HandlerArrays.Add(existing, handler);
			return new Subscription(this, typeof(TMessage), handler);
		}

		private void Unsubscribe(Type messageType, Delegate handler)
		{
			if (!_handlers.TryGetValue(messageType, out Delegate[] existing)) return;

			Delegate[] remaining = HandlerArrays.Remove(existing, handler);
			if (remaining.Length == 0) _handlers.Remove(messageType); else _handlers[messageType] = remaining;
		}

		private sealed class Subscription : IDisposable
		{
			private readonly MessageBus _owner;
			private readonly Type _messageType;
			private readonly Delegate _handler;
			private bool _isDisposed;

			public Subscription(MessageBus owner, Type messageType, Delegate handler)
			{
				_owner = owner;
				_messageType = messageType;
				_handler = handler;
			}

			public void Dispose()
			{
				if (_isDisposed) return;

				_isDisposed = true;
				_owner.Unsubscribe(_messageType, _handler);
			}
		}
	}
}