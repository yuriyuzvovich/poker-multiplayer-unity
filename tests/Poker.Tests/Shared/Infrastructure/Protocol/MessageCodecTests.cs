using System;
using System.Text;
using Xunit;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;

namespace Poker.Tests.Shared.Infrastructure.Protocol
{
	public sealed class MessageCodecTests
	{
		private const string MessageName = "test.note";

		[Fact]
		public void Encode_ThenDecode_ReturnsTypedPayloadAndHeader()
		{
			MessageCodec codec = CreateCodec();

			byte[] packet = codec.Encode(new NoteMessage { Text = "hello" }, channel: 2, requestId: 9);

			Assert.Equal(DecodeStatus.Ok, codec.Decode(new ArraySegment<byte>(packet), out IncomingMessage message));
			Assert.Equal(MessageName, message.Type.Name);
			Assert.Equal(MessageTypeId.Compute(MessageName), message.Header.TypeId);
			Assert.Equal(2, message.Header.Channel);
			Assert.Equal(9, message.Header.RequestId);
			Assert.Equal("hello", Assert.IsType<NoteMessage>(message.Payload).Text);
		}

		[Fact]
		public void Decode_UnknownTypeId_ReportsUnknownType()
		{
			byte[] packet = PacketCodec.Encode(new PacketHeader(0, 0, 12345, 0, 0, PacketFlags.None), ReadOnlySpan<byte>.Empty);

			Assert.Equal(DecodeStatus.UnknownType, CreateCodec().Decode(new ArraySegment<byte>(packet), out IncomingMessage _));
		}

		[Fact]
		public void Decode_TruncatedPacket_ReportsTruncated()
		{
			Assert.Equal(DecodeStatus.Truncated, CreateCodec().Decode(new ArraySegment<byte>(new byte[3]), out IncomingMessage _));
		}

		private static MessageCodec CreateCodec()
		{
			MessageRegistry registry = new();
			registry.Register<NoteMessage>(MessageName);
			return new MessageCodec(registry, new NoteSerializer());
		}

		private sealed class NoteMessage
		{
			public string Text { get; set; } = "";
		}

		private sealed class NoteSerializer : IPayloadSerializer
		{
			public byte[] Serialize(object payload, Type type) => Encoding.UTF8.GetBytes(((NoteMessage)payload).Text);

			public object Deserialize(ArraySegment<byte> payload, Type type) => new NoteMessage { Text = Encoding.UTF8.GetString(payload.AsSpan()) };
		}
	}
}