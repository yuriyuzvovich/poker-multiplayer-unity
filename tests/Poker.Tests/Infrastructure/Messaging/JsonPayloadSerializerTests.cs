using System;
using System.Text;
using Poker.Server.Infrastructure.Messaging;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Xunit;

namespace Poker.Tests.Infrastructure.Messaging
{
	public sealed class JsonPayloadSerializerTests
	{
		[Fact]
		public void Serialize_UsesCamelCaseWireNames()
		{
			byte[] bytes = new JsonPayloadSerializer().Serialize(new JoinTableRequest { TableId = "practice" }, typeof(JoinTableRequest));

			Assert.Equal("{\"tableId\":\"practice\"}", Encoding.UTF8.GetString(bytes));
		}

		[Fact]
		public void Deserialize_ReadsWhatSerializeWrote()
		{
			JsonPayloadSerializer serializer = new();
			byte[] bytes = serializer.Serialize(new SeatSnapshot { Index = 4, PlayerName = "Grace", Stack = 7450 }, typeof(SeatSnapshot));

			SeatSnapshot seat = Assert.IsType<SeatSnapshot>(serializer.Deserialize(new ArraySegment<byte>(bytes), typeof(SeatSnapshot)));

			Assert.Equal(4, seat.Index);
			Assert.Equal("Grace", seat.PlayerName);
			Assert.Equal(7450, seat.Stack);
		}

		[Theory]
		[InlineData("not json")]
		[InlineData("null")]
		[InlineData("{\"tableId\":42}")]
		public void Deserialize_InvalidPayload_ThrowsPayloadFormatException(string json)
		{
			ArraySegment<byte> payload = new(Encoding.UTF8.GetBytes(json));

			Assert.Throws<PayloadFormatException>(() => new JsonPayloadSerializer().Deserialize(payload, typeof(JoinTableRequest)));
		}
	}
}