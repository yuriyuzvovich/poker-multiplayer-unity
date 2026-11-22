using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public sealed class MessageRegistry
	{
		private readonly Dictionary<uint, MessageType> _byId = new();
		private readonly Dictionary<Type, MessageType> _byClrType = new();

		public IReadOnlyCollection<MessageType> All => _byId.Values;

		public void Register<TMessage>(string name) where TMessage : class
		{
			MessageType messageType = new(MessageTypeId.Compute(name), name, typeof(TMessage));
			if (_byId.TryGetValue(messageType.Id, out MessageType? existing))
			{
				throw new InvalidOperationException($"Message type id collision: '{name}' and '{existing.Name}' both hash to {messageType.Id}.");
			}

			if (_byClrType.ContainsKey(messageType.ClrType))
			{
				throw new InvalidOperationException($"{messageType.ClrType.Name} is already registered.");
			}

			_byId.Add(messageType.Id, messageType);
			_byClrType.Add(messageType.ClrType, messageType);
		}

		public MessageType Get(Type clrType)
		{
			if (_byClrType.TryGetValue(clrType, out MessageType? messageType)) return messageType;

			throw new KeyNotFoundException($"{clrType.Name} is not a registered message.");
		}

		public bool TryGet(uint id, [NotNullWhen(true)] out MessageType? messageType) => _byId.TryGetValue(id, out messageType);
	}
}