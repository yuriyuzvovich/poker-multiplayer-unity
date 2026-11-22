using Poker.Shared.Infrastructure.Protocol.Packets;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public readonly struct IncomingMessage
	{
		public IncomingMessage(PacketHeader header, MessageType type, object payload)
		{
			Header = header;
			Type = type;
			Payload = payload;
		}

		public PacketHeader Header { get; }
		public MessageType Type { get; }
		public object Payload { get; }
	}
}