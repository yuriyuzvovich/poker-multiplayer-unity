using System;
using System.Collections.Generic;
using Poker.Client.Infrastructure.Core.Messaging;
using UnityEngine;

namespace Poker.Client.Infrastructure.Network.Messaging
{
	// Delivers each server message to every subscriber of its type. A subscriber that throws is logged and skipped,
	// so one broken presenter can neither starve the others nor stop the receive loop.
	public sealed class MessageRouter
	{
		private readonly Dictionary<Type, Action<object>[]> _handlers = new();

		public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
		{
			if (handler == null) throw new ArgumentNullException(nameof(handler));

			Action<object> untyped = message => handler((TMessage)message);
			_handlers.TryGetValue(typeof(TMessage), out Action<object>[] existing);
			_handlers[typeof(TMessage)] = HandlerArrays.Add(existing, untyped);
			return new Subscription(this, typeof(TMessage), untyped);
		}

		public void Dispatch(Type messageType, object message)
		{
			if (!_handlers.TryGetValue(messageType, out Action<object>[] handlers)) return;

			foreach (Action<object> handler in handlers)
			{
				try { handler(message); }
				catch (Exception exception) { Debug.LogException(exception); }
			}
		}

		private void Unsubscribe(Type messageType, Action<object> handler)
		{
			if (!_handlers.TryGetValue(messageType, out Action<object>[] existing)) return;

			Action<object>[] remaining = HandlerArrays.Remove(existing, handler);
			if (remaining.Length == 0) _handlers.Remove(messageType); else _handlers[messageType] = remaining;
		}

		private sealed class Subscription : IDisposable
		{
			private readonly MessageRouter _owner;
			private readonly Type _messageType;
			private readonly Action<object> _handler;
			private bool _isDisposed;

			public Subscription(MessageRouter owner, Type messageType, Action<object> handler)
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