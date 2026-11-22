namespace Poker.Shared.Features.Table
{
	public static class SeatErrorCodes
	{
		public const string SeatInvalid = "seat.invalid";
		public const string SeatTaken = "seat.taken";
		public const string AlreadySeated = "seat.already_seated";
		public const string NotReserved = "seat.not_reserved";
		public const string ReservationExpired = "seat.reservation_expired";
		public const string NotSeated = "seat.not_seated";
		public const string BuyInOutOfRange = "buyin.out_of_range";
		public const string InsufficientFunds = "buyin.insufficient_funds";
	}
}