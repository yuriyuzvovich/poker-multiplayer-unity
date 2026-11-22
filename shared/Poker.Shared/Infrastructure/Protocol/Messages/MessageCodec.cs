using System;
using Poker.Shared.Infrastructure.Protocol.Packets;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public sealed class MessageCodec
	{
		private readonly MessageRegistry _registry;
		private readonly IPayloadSerializer _serializer;

		public MessageCodec(MessageRegistry registry, IPayloadSerializer serializer)
		{
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public byte[] Encode<TMessage>(TMessage message, ushort channel = 0, ushort requestId = 0) where TMessage : class
		{
			MessageType messageType = _registry.Get(message.GetType());
			PacketHeader header = new(0, 0, messageType.Id, requestId, channel, PacketFlags.None);
			return PacketCodec.Encode(header, _serializer.Serialize(message, messageType.ClrType));
		}

		public DecodeStatus Decode(ArraySegment<byte> packet, out IncomingMessage message)
		{
			message = default;
			if (!PacketCodec.TryDecode(packet, out PacketHeader header, out ArraySegment<byte> payload)) return DecodeStatus.Truncated;
			if (!_registry.TryGet(header.TypeId, out MessageType? messageType)) return DecodeStatus.UnknownType;

			message = new IncomingMessage(header, messageType, _serializer.Deserialize(payload, messageType.ClrType));
			return DecodeStatus.Ok;
		}
	}
}