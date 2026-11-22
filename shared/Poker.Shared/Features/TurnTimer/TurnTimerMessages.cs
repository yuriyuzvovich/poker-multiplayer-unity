using Poker.Shared.Features.TurnTimer.Events;
using Poker.Shared.Features.TurnTimer.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.TurnTimer
{
	public static class TurnTimerMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<PlayerSatOutEvent>(TurnTimerMessageNames.SatOut);
			registry.Register<PlayerReturnRequest>(TurnTimerMessageNames.Return);
			registry.Register<PlayerReturnAcceptedResponse>(TurnTimerMessageNames.ReturnAccepted);
			registry.Register<PlayerReturnedEvent>(TurnTimerMessageNames.Returned);
		}
	}
}