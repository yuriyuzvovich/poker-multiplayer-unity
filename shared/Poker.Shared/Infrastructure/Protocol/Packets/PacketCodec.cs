using System;
using System.Buffers.Binary;

namespace Poker.Shared.Infrastructure.Protocol.Packets
{
	public static class PacketCodec
	{
		private const int SeqOffset = 0;
		private const int AckOffset = 2;
		private const int TypeIdOffset = 4;
		private const int RequestIdOffset = 8;
		private const int ChannelOffset = 10;
		private const int FlagsOffset = 12;

		public static byte[] Encode(in PacketHeader header, ReadOnlySpan<byte> payload)
		{
			byte[] packet = new byte[PacketHeader.Size + payload.Length];
			Span<byte> span = packet;
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(SeqOffset), header.Seq);
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(AckOffset), header.Ack);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(TypeIdOffset), header.TypeId);
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(RequestIdOffset), header.RequestId);
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(ChannelOffset), header.Channel);
			BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(FlagsOffset), (ushort)header.Flags);
			payload.CopyTo(span.Slice(PacketHeader.Size));
			return packet;
		}

		public static void WriteSeqAck(Span<byte> packet, ushort seq, ushort ack)
		{
			BinaryPrimitives.WriteUInt16LittleEndian(packet.Slice(SeqOffset), seq);
			WriteAck(packet, ack);
		}

		public static void WriteAck(Span<byte> packet, ushort ack) => BinaryPrimitives.WriteUInt16LittleEndian(packet.Slice(AckOffset), ack);

		public static bool TryDecode(ArraySegment<byte> packet, out PacketHeader header, out ArraySegment<byte> payload)
		{
			header = default;
			payload = default;
			if (packet.Count < PacketHeader.Size) return false;

			ReadOnlySpan<byte> span = packet.AsSpan();
			header = new PacketHeader(
				BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(SeqOffset)),
				BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(AckOffset)),
				BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(TypeIdOffset)),
				BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(RequestIdOffset)),
				BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(ChannelOffset)),
				(PacketFlags)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(FlagsOffset)));
			payload = packet.Slice(PacketHeader.Size);
			return true;
		}
	}
}