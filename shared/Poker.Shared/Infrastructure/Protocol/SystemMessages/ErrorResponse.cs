namespace Poker.Shared.Infrastructure.Protocol.SystemMessages
{
	public sealed class ErrorResponse
	{
		public string Code { get; set; } = "";
		public string Message { get; set; } = "";
	}
}