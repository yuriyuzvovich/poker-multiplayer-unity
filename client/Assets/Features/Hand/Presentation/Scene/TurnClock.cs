using System;
using System.Globalization;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Features.Hand.Presentation.Scene
{
	// The countdown of the turn being shown. Time runs against the server deadline minus the leeway the server adds
	// for latency, so the ring empties before the server acts; the label only changes when the whole second does.
	public sealed class TurnClock
	{
		private long _windowMs;
		private int _shownSeconds = -1;

		public TurnStartedEvent Turn { get; private set; }

		public bool IsMine { get; private set; }

		public float Remaining { get; private set; }

		public string SecondsText { get; private set; } = "";

		public bool IsRunning => Turn != null;

		public void Start(TurnStartedEvent turn, bool isMine)
		{
			Clear();
			Turn = turn;
			IsMine = isMine;
		}

		public void Clear()
		{
			Turn = null;
			IsMine = false;
			Remaining = 0f;
			SecondsText = "";
			_windowMs = 0;
			_shownSeconds = -1;
		}

		// Returns true on the tick the shown seconds reach zero.
		public bool Advance(long nowUnixMs)
		{
			long remainingMs = Math.Max(0L, Turn.DeadlineUnixMs - Turn.LeewayMs - nowUnixMs);
			if (_windowMs <= 0) _windowMs = Math.Max(1L, remainingMs);

			Remaining = (float)remainingMs / _windowMs;
			int seconds = (int)((remainingMs + 999) / 1000);
			if (seconds == _shownSeconds) return false;

			_shownSeconds = seconds;
			SecondsText = seconds > 0 ? seconds.ToString(CultureInfo.InvariantCulture) : "";
			return seconds == 0;
		}
	}
}