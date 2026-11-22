namespace Poker.Server.Features.Table.Domain
{
	public readonly struct SeatingResult
	{
		private SeatingResult(Seat? seat, string errorCode)
		{
			Seat = seat;
			ErrorCode = errorCode;
		}

		public Seat? Seat { get; }
		public string ErrorCode { get; }
		public bool IsSuccess => ErrorCode.Length == 0;

		public static SeatingResult Success(Seat seat) => new(seat, "");

		public static SeatingResult Failure(string errorCode) => new(null, errorCode);
	}
}