using System;

namespace Poker.Server.Features.Hand.Runtime
{
	public sealed class HandOptions
	{
		public TimeSpan TurnTime { get; init; } = TimeSpan.FromSeconds(15);
		public TimeSpan TurnLeeway { get; init; } = TimeSpan.FromSeconds(2);
		public TimeSpan NextHandDelay { get; init; } = TimeSpan.FromSeconds(3);
		public TimeSpan BotThinkMin { get; init; } = TimeSpan.FromMilliseconds(600);
		public TimeSpan BotThinkMax { get; init; } = TimeSpan.FromMilliseconds(1500);
		public int? BotSeed { get; init; }
	}
}