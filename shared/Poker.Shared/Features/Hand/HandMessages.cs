using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.Hand
{
	public static class HandMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<HandStartedEvent>(HandMessageNames.HandStarted);
			registry.Register<HoleCardsEvent>(HandMessageNames.HoleCards);
			registry.Register<HandSnapshotEvent>(HandMessageNames.HandSnapshot);
			registry.Register<TurnStartedEvent>(HandMessageNames.TurnStarted);
			registry.Register<TurnActRequest>(HandMessageNames.TurnAct);
			registry.Register<TurnActAcceptedResponse>(HandMessageNames.TurnActAccepted);
			registry.Register<TurnActedEvent>(HandMessageNames.TurnActed);
			registry.Register<RoundEndedEvent>(HandMessageNames.RoundEnded);
			registry.Register<RoundStartedEvent>(HandMessageNames.RoundStarted);
			registry.Register<ShowdownEvent>(HandMessageNames.Showdown);
			registry.Register<HandEndedEvent>(HandMessageNames.HandEnded);
		}
	}
}