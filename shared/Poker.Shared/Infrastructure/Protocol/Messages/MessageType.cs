using System;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public sealed class MessageType
	{
		public MessageType(uint id, string name, Type clrType)
		{
			Id = id;
			Name = name;
			ClrType = clrType;
		}

		public uint Id { get; }
		public string Name { get; }
		public Type ClrType { get; }

		public override string ToString() => Name;
	}
}