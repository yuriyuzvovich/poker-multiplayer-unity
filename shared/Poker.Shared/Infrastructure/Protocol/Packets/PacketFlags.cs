using System;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Shared.Infrastructure.Protocol.Packets
{
	[Flags]
	public enum PacketFlags : ushort
	{
		None = 0,
		BinaryPayload = 1,
		ResponseExpected = 2,
		ErrorResponse = 4,
	}
}