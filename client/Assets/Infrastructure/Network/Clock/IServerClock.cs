namespace Poker.Client.Infrastructure.Network.Clock
{
	public interface IServerClock
	{
		public long NowUnixMs { get; }
	}
}