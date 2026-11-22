using Poker.Client.Infrastructure.Network.Clock;


namespace Poker.Client.Tests.EditMode.Network
{
	public sealed class FakeServerClock : IServerClock
	{
		public long NowUnixMs { get; set; }
	}
}