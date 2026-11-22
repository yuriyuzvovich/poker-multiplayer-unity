using System;
using Poker.Server.Infrastructure.Time;

namespace Poker.Tests.TestDoubles
{
	public sealed class ManualClock : IClock
	{
		public ManualClock(DateTimeOffset start) => UtcNow = start;

		public DateTimeOffset UtcNow { get; private set; }

		public void Advance(TimeSpan delta) => UtcNow += delta;
	}
}