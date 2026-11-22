using System;
using System.Collections.Generic;
using Poker.Shared.Infrastructure.Protocol.Packets;

namespace Poker.Shared.Infrastructure.Protocol.Reliability
{
	public sealed class ReliableChannel
	{
		private readonly Queue<SentPacket> _sent = new();
		private readonly int _capacity;
		private ushort _lastSent;

		public ReliableChannel(int capacity = 4096)
		{
			if (capacity <= 0 || capacity > short.MaxValue) throw new ArgumentOutOfRangeException(nameof(capacity), "Must stay below half the sequence space so wrap-around comparisons hold.");

			_capacity = capacity;
		}

		public ushort LastReceived { get; private set; }
		public int UnackedCount => _sent.Count;
		public int ReceivedSinceAck { get; private set; }

		public bool TryStamp(byte[] packet)
		{
			if (_sent.Count >= _capacity) return false;

			_lastSent = Next(_lastSent);
			PacketCodec.WriteSeqAck(packet, _lastSent, LastReceived);
			_sent.Enqueue(new SentPacket(_lastSent, packet));
			ReceivedSinceAck = 0;
			return true;
		}

		public void StampUnnumbered(byte[] packet)
		{
			PacketCodec.WriteSeqAck(packet, 0, LastReceived);
			ReceivedSinceAck = 0;
		}

		public ReceiveResult Receive(in PacketHeader header)
		{
			Acknowledge(header.Ack);
			if (header.Seq == 0) return ReceiveResult.Unnumbered;

			ushort expected = Next(LastReceived);
			if (header.Seq == expected)
			{
				LastReceived = expected;
				ReceivedSinceAck++;
				return ReceiveResult.Accepted;
			}

			return IsNewer(header.Seq, LastReceived) ? ReceiveResult.Gap : ReceiveResult.Duplicate;
		}

		public void Acknowledge(ushort ack)
		{
			while (_sent.Count > 0 && !IsNewer(_sent.Peek().Seq, ack)) _sent.Dequeue();
		}

		public void CollectResend(ushort peerAck, List<byte[]> destination)
		{
			Acknowledge(peerAck);
			foreach (SentPacket sent in _sent)
			{
				PacketCodec.WriteAck(sent.Packet, LastReceived);
				destination.Add(sent.Packet);
			}

			ReceivedSinceAck = 0;
		}

		public static bool IsNewer(ushort seq, ushort than) => (short)(seq - than) > 0;

		private static ushort Next(ushort seq) => seq == ushort.MaxValue ? (ushort)1 : (ushort)(seq + 1);

		private readonly struct SentPacket
		{
			public SentPacket(ushort seq, byte[] packet)
			{
				Seq = seq;
				Packet = packet;
			}

			public ushort Seq { get; }
			public byte[] Packet { get; }
		}
	}
}