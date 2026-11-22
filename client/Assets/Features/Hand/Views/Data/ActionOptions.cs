namespace Poker.Client.Features.Hand.Views.Data
{
	public readonly struct ActionOptions
	{
		public ActionOptions(long toCall, bool canCheck, bool canRaise, long minRaiseTo, long maxRaiseTo)
		{
			ToCall = toCall;
			CanCheck = canCheck;
			CanRaise = canRaise;
			MinRaiseTo = minRaiseTo;
			MaxRaiseTo = maxRaiseTo;
		}

		public long ToCall { get; }
		public bool CanCheck { get; }
		public bool CanRaise { get; }
		public long MinRaiseTo { get; }
		public long MaxRaiseTo { get; }
	}
}