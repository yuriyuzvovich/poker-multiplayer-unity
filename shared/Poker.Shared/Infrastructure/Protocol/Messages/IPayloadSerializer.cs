using System;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public interface IPayloadSerializer
	{
		public byte[] Serialize(object payload, Type type);

		public object Deserialize(ArraySegment<byte> payload, Type type);
	}
}