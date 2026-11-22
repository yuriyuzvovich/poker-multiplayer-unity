namespace Poker.Shared.Infrastructure.Protocol.Packets
{
	public readonly struct PacketHeader
	{
		public const int Size = 14;

		public PacketHeader(ushort seq, ushort ack, uint typeId, ushort requestId, ushort channel, PacketFlags flags)
		{
			Seq = seq;
			Ack = ack;
			TypeId = typeId;
			RequestId = requestId;
			Channel = channel;
			Flags = flags;
		}

		public ushort Seq { get; }
		public ushort Ack { get; }
		public uint TypeId { get; }
		public ushort RequestId { get; }
		public ushort Channel { get; }
		public PacketFlags Flags { get; }
	}
}