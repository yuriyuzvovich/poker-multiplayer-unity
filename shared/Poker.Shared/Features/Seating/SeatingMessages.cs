using Poker.Shared.Features.Seating.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.Seating
{
	public static class SeatingMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<ReserveSeatRequest>(SeatingMessageNames.ReserveSeat);
			registry.Register<SeatReservationResponse>(SeatingMessageNames.SeatReservation);
			registry.Register<BuyInRequest>(SeatingMessageNames.BuyIn);
			registry.Register<BuyInAcceptedResponse>(SeatingMessageNames.BuyInAccepted);
			registry.Register<StandUpRequest>(SeatingMessageNames.StandUp);
			registry.Register<StandUpAcceptedResponse>(SeatingMessageNames.StandUpAccepted);
		}
	}
}