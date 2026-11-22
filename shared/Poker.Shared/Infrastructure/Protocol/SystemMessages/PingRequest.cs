namespace Poker.Shared.Infrastructure.Protocol.SystemMessages
{
	public sealed class PingRequest
	{
		public long ClientTimeUnixMs { get; set; }
	}
}