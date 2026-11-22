namespace Poker.Client.Infrastructure.Network.Session
{
	public sealed class SessionReplaced
	{
		public SessionReplaced(string reason) => Reason = reason;

		public string Reason { get; }
	}
}