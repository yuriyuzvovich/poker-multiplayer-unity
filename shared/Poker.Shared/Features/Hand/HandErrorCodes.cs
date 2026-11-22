namespace Poker.Shared.Features.Hand
{
	public static class HandErrorCodes
	{
		public const string NoHand = "hand.none";
		public const string NotYourTurn = "turn.not_your_turn";
		public const string StaleTurn = "turn.stale";
		public const string InvalidAction = "turn.invalid_action";
		public const string InvalidAmount = "turn.invalid_amount";
	}
}