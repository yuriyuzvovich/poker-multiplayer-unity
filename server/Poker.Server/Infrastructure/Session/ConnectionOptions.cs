using System;

namespace Poker.Server.Infrastructure.Session
{
	public sealed class ConnectionOptions
	{
		public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(10);
		public TimeSpan ZombieWindow { get; init; } = TimeSpan.FromSeconds(30);
		public int MaxUnacked { get; init; } = 30;
		public int ResendCapacity { get; init; } = 4096;

		public TimeSpan DeadSocketAfter => HeartbeatInterval * 2;

		// Room for a full resend after a resume plus the unnumbered traffic around it.
		public int OutboxCapacity => ResendCapacity + 256;
	}
}