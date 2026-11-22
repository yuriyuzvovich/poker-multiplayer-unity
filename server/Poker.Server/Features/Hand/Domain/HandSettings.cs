using System;

namespace Poker.Server.Features.Hand.Domain
{
	public sealed record HandSettings(long SmallBlind, long BigBlind, TimeSpan TurnTime, TimeSpan TurnLeeway, TimeSpan NextHandDelay);
}