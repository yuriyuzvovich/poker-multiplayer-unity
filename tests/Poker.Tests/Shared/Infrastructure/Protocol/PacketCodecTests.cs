using System;
using Xunit;
using Poker.Shared.Infrastructure.Protocol.Packets;

namespace Poker.Tests.Shared.Infrastructure.Protocol
{
	public sealed class PacketCodecTests
	{
		[Fact]
		public void Encode_WritesLittleEndianHeaderThenPayload()
		{
			PacketHeader header = new(0x0102, 0x0304, 0x05060708, 0x090A, 0x0B0C, PacketFlags.ResponseExpected);

			byte[] packet = PacketCodec.Encode(header, new byte[] { 0xAA, 0xBB });

			Assert.Equal(new byte[] { 0x02, 0x01, 0x04, 0x03, 0x08, 0x07, 0x06, 0x05, 0x0A, 0x09, 0x0C, 0x0B, 0x02, 0x00, 0xAA, 0xBB }, packet);
		}

		[Fact]
		public void TryDecode_RoundTripsHeaderAndPayload()
		{
			PacketHeader header = new(65535, 7, 0xDEADBEEF, 3, 2, PacketFlags.BinaryPayload | PacketFlags.ErrorResponse);
			byte[] packet = PacketCodec.Encode(header, new byte[] { 1, 2, 3 });

			Assert.True(PacketCodec.TryDecode(new ArraySegment<byte>(packet), out PacketHeader decoded, out ArraySegment<byte> payload));

			Assert.Equal(header.Seq, decoded.Seq);
			Assert.Equal(header.Ack, decoded.Ack);
			Assert.Equal(header.TypeId, decoded.TypeId);
			Assert.Equal(header.RequestId, decoded.RequestId);
			Assert.Equal(header.Channel, decoded.Channel);
			Assert.Equal(header.Flags, decoded.Flags);
			Assert.Equal(new byte[] { 1, 2, 3 }, payload.ToArray());
		}

		[Fact]
		public void TryDecode_EmptyPayload_IsValid()
		{
			byte[] packet = PacketCodec.Encode(new PacketHeader(1, 0, 42, 0, 0, PacketFlags.None), ReadOnlySpan<byte>.Empty);

			Assert.True(PacketCodec.TryDecode(new ArraySegment<byte>(packet), out PacketHeader decoded, out ArraySegment<byte> payload));
			Assert.Equal(42u, decoded.TypeId);
			Assert.Equal(0, payload.Count);
		}

		[Fact]
		public void TryDecode_ShorterThanHeader_IsRejected()
		{
			Assert.False(PacketCodec.TryDecode(new ArraySegment<byte>(new byte[PacketHeader.Size - 1]), out PacketHeader _, out ArraySegment<byte> _));
		}
	}
}