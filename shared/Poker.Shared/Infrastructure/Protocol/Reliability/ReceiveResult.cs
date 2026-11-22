namespace Poker.Shared.Infrastructure.Protocol.Reliability
{
	public enum ReceiveResult
	{
		Accepted = 0,
		Unnumbered = 1,
		Duplicate = 2,
		Gap = 3,
	}
}