using UnityEngine;

namespace Poker.Client.Features.Table.Views.Seats
{
	public static class SeatLayout
	{
		private static readonly Vector2[] Slots =
		{
			new Vector2(0.5f, 0.1f),
			new Vector2(0.15f, 0.34f),
			new Vector2(0.15f, 0.68f),
			new Vector2(0.5f, 0.9f),
			new Vector2(0.85f, 0.68f),
			new Vector2(0.85f, 0.34f),
		};

		private static readonly int[][] SlotsBySeatCount =
		{
			new[] { 0 },
			new[] { 0 },
			new[] { 0, 3 },
			new[] { 0, 2, 4 },
			new[] { 0, 1, 3, 5 },
			new[] { 0, 1, 2, 4, 5 },
			new[] { 0, 1, 2, 3, 4, 5 },
		};

		public static int MaxSeats => Slots.Length;

		public static Vector2 AnchorFor(int seatIndex, int seatCount) => Slots[SlotsBySeatCount[Mathf.Clamp(seatCount, 0, MaxSeats)][seatIndex]];

		public static Vector2 TowardCenter(Vector2 anchor) => (new Vector2(0.5f, 0.5f) - anchor).normalized;
	}
}