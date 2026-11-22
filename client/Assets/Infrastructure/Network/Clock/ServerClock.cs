using System;

namespace Poker.Client.Infrastructure.Network.Clock
{
	public sealed class ServerClock : IServerClock
	{
		private const int WindowSize = 6;

		private readonly long[] _roundTrips = new long[WindowSize];
		private readonly long[] _offsets = new long[WindowSize];
		private int _count;
		private int _next;

		public long NowUnixMs => LocalNowUnixMs + OffsetMs;

		public long OffsetMs { get; private set; }

		public long BestRoundTripMs { get; private set; } = long.MaxValue;

		public static long LocalNowUnixMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		public bool AddSample(long sentAtUnixMs, long serverTimeUnixMs, long receivedAtUnixMs)
		{
			long roundTripMs = receivedAtUnixMs - sentAtUnixMs;
			if (roundTripMs < 0) return false;

			int index = _next;
			_roundTrips[index] = roundTripMs;
			_offsets[index] = serverTimeUnixMs + roundTripMs / 2 - receivedAtUnixMs;
			_next = (_next + 1) % WindowSize;
			_count = Math.Min(_count + 1, WindowSize);
			return SelectBest() == index;
		}

		private int SelectBest()
		{
			int best = _next == 0 ? WindowSize - 1 : _next - 1;
			for (int i = 0; i < _count; i++)
			{
				if (_roundTrips[i] >= _roundTrips[best]) continue;

				best = i;
			}

			BestRoundTripMs = _roundTrips[best];
			OffsetMs = _offsets[best];
			return best;
		}
	}
}