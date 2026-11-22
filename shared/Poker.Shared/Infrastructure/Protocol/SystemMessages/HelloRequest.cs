namespace Poker.Shared.Infrastructure.Protocol.SystemMessages
{
	public sealed class HelloRequest
	{
		public int ProtocolVersion { get; set; }
		public string PlayerName { get; set; } = "";
		public string ResumeKey { get; set; } = "";
		public string DeviceKey { get; set; } = "";
	}
}