using System.Collections.Generic;

namespace Poker.Server.Features.TurnTimer
{
	public sealed class TurnTimerTableState
	{
		private readonly Dictionary<int, int> _strikes = new();

		public int AddStrike(int seatIndex)
		{
			_strikes.TryGetValue(seatIndex, out int strikes);
			strikes++;
			_strikes[seatIndex] = strikes;
			return strikes;
		}

		public void Clear(int seatIndex) => _strikes.Remove(seatIndex);
	}
}