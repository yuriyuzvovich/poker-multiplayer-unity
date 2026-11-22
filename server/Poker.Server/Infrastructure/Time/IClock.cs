using System;

namespace Poker.Server.Infrastructure.Time
{
	public interface IClock
	{
		public DateTimeOffset UtcNow { get; }
	}
}