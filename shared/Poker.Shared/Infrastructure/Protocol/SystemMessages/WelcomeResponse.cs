namespace Poker.Shared.Infrastructure.Protocol.SystemMessages
{
	public sealed class WelcomeResponse
	{
		public string SessionId { get; set; } = "";
		public string PlayerId { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public long ServerTimeUnixMs { get; set; }
		public string ResumeKey { get; set; } = "";
		public bool Resumed { get; set; }
		public int HeartbeatMs { get; set; }
		public int ZombieMs { get; set; }
		public int MaxUnacked { get; set; }
		public string SeatedTableId { get; set; } = "";
	}
}