using System;
using System.Collections.Generic;
using Xunit;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.Reliability;

namespace Poker.Tests.Shared.Infrastructure.Protocol
{
	public sealed class ReliableChannelTests
	{
		[Fact]
		public void Stamp_NumbersPacketsFromOne_AndCarriesTheLastReceivedSeq()
		{
			ReliableChannel channel = new();
			channel.Receive(Header(seq: 1));
			channel.Receive(Header(seq: 2));

			byte[] first = Packet();
			byte[] second = Packet();
			channel.TryStamp(first);
			channel.TryStamp(second);

			Assert.Equal((1, 2), SeqAck(first));
			Assert.Equal((2, 2), SeqAck(second));
			Assert.Equal(2, channel.UnackedCount);
		}

		[Fact]
		public void Receive_InOrder_IsAccepted_ARepeat_IsADuplicate_ASkip_IsAGap()
		{
			ReliableChannel channel = new();

			Assert.Equal(ReceiveResult.Accepted, channel.Receive(Header(seq: 1)));
			Assert.Equal(ReceiveResult.Accepted, channel.Receive(Header(seq: 2)));
			Assert.Equal(ReceiveResult.Duplicate, channel.Receive(Header(seq: 1)));
			Assert.Equal(ReceiveResult.Duplicate, channel.Receive(Header(seq: 2)));
			Assert.Equal(ReceiveResult.Gap, channel.Receive(Header(seq: 4)));
			Assert.Equal(ReceiveResult.Unnumbered, channel.Receive(Header(seq: 0)));
			Assert.Equal(2, channel.LastReceived);
		}

		[Fact]
		public void PeerAck_DropsEverythingUpToIt()
		{
			ReliableChannel channel = new();
			for (int i = 0; i < 5; i++)
			{
				channel.TryStamp(Packet());
			}

			channel.Receive(Header(seq: 0, ack: 3));

			Assert.Equal(2, channel.UnackedCount);
		}

		[Fact]
		public void CollectResend_ReturnsUnackedPacketsInOrder_WithTheCurrentAck()
		{
			ReliableChannel channel = new();
			List<byte[]> stamped = new();
			for (int i = 0; i < 4; i++)
			{
				byte[] packet = Packet();
				channel.TryStamp(packet);
				stamped.Add(packet);
			}

			channel.Receive(Header(seq: 1));
			channel.Receive(Header(seq: 2));
			List<byte[]> resend = new();
			channel.CollectResend(peerAck: 2, resend);

			Assert.Equal(new[] { stamped[2], stamped[3] }, resend);
			Assert.Equal((3, 2), SeqAck(resend[0]));
			Assert.Equal((4, 2), SeqAck(resend[1]));
		}

		[Fact]
		public void Numbering_WrapsPastZero_AndOrderStillHolds()
		{
			ReliableChannel sender = new(capacity: 100);
			ReliableChannel receiver = new();
			for (int i = 0; i < 65540; i++)
			{
				byte[] packet = Packet();
				Assert.True(sender.TryStamp(packet));
				PacketCodec.TryDecode(new ArraySegment<byte>(packet), out PacketHeader header, out _);
				Assert.NotEqual(0, header.Seq);
				Assert.Equal(ReceiveResult.Accepted, receiver.Receive(header));
				if (i % 50 == 0) sender.Acknowledge(receiver.LastReceived);
			}

			Assert.Equal(ReceiveResult.Duplicate, receiver.Receive(Header(seq: receiver.LastReceived)));
			sender.Acknowledge(receiver.LastReceived);
			Assert.Equal(0, sender.UnackedCount);
		}

		[Fact]
		public void Capacity_AtOrAboveHalfTheSequenceSpace_IsRefused() => Assert.Throws<ArgumentOutOfRangeException>(() => new ReliableChannel(capacity: 32768));

		[Fact]
		public void FullResendBuffer_RefusesMorePackets()
		{
			ReliableChannel channel = new(capacity: 2);

			Assert.True(channel.TryStamp(Packet()));
			Assert.True(channel.TryStamp(Packet()));
			Assert.False(channel.TryStamp(Packet()));
		}

		[Fact]
		public void ReceivedSinceAck_CountsUntilSomethingCarriesTheAck()
		{
			ReliableChannel channel = new();
			channel.Receive(Header(seq: 1));
			channel.Receive(Header(seq: 2));
			Assert.Equal(2, channel.ReceivedSinceAck);

			channel.StampUnnumbered(Packet());

			Assert.Equal(0, channel.ReceivedSinceAck);
		}

		private static PacketHeader Header(ushort seq, ushort ack = 0) => new(seq, ack, 1, 0, 0, PacketFlags.None);

		private static byte[] Packet() => PacketCodec.Encode(new PacketHeader(0, 0, 1, 0, 0, PacketFlags.None), new byte[] { 42 });

		private static (int Seq, int Ack) SeqAck(byte[] packet)
		{
			PacketCodec.TryDecode(new ArraySegment<byte>(packet), out PacketHeader header, out _);
			return (header.Seq, header.Ack);
		}
	}
}