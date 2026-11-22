namespace Poker.Shared.Infrastructure.Protocol.SystemMessages
{
	public sealed class PongResponse
	{
		public long ClientTimeUnixMs { get; set; }
		public long ServerTimeUnixMs { get; set; }
	}
}