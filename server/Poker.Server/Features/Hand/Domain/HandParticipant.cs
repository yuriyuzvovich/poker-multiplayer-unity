namespace Poker.Server.Features.Hand.Domain
{
	public sealed record HandParticipant(int SeatIndex, string PlayerId, long Stack);
}