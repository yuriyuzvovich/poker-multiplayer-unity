namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public enum DecodeStatus
	{
		Ok = 0,
		Truncated = 1,
		UnknownType = 2,
	}
}