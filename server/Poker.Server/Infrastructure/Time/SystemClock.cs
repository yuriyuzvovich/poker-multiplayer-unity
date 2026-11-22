using System;

namespace Poker.Server.Infrastructure.Time
{
	public sealed class SystemClock : IClock
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	}
}