using System;
using System.Collections.Generic;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;

namespace Poker.Client.Features.Table.Domain
{
	public sealed class TableModel
	{
		private SeatSnapshot[] _seats = Array.Empty<SeatSnapshot>();

		public event Action Changed;

		public bool IsJoined => TableId.Length > 0;
		public string TableId { get; private set; } = "";
		public string Name { get; private set; } = "";
		public long MinBuyIn { get; private set; }
		public long MaxBuyIn { get; private set; }

		public IReadOnlyList<SeatSnapshot> Seats => _seats;

		public void Load(TableSnapshot snapshot)
		{
			TableId = snapshot.TableId;
			Name = snapshot.Name;
			MinBuyIn = snapshot.MinBuyIn;
			MaxBuyIn = snapshot.MaxBuyIn;
			_seats = snapshot.Seats;
			Changed?.Invoke();
		}

		public void Clear()
		{
			TableId = "";
			Name = "";
			_seats = Array.Empty<SeatSnapshot>();
			Changed?.Invoke();
		}

		public void ApplySeat(string tableId, SeatSnapshot seat)
		{
			if (!IsCurrent(tableId, seat.Index)) return;

			_seats[seat.Index] = seat;
			Changed?.Invoke();
		}

		public void ApplyStack(string tableId, int seatIndex, string playerId, long stack)
		{
			if (!IsCurrent(tableId, seatIndex)) return;

			SeatSnapshot seat = _seats[seatIndex];
			if (seat.Status != SeatStatus.Seated) return;
			if (seat.PlayerId != playerId) return;
			if (seat.Stack == stack) return;

			SeatSnapshot changed = Copy(seat);
			changed.Stack = stack;
			_seats[seatIndex] = changed;
			Changed?.Invoke();
		}

		public void SetSittingOut(string tableId, int seatIndex, bool isSittingOut)
		{
			if (!IsCurrent(tableId, seatIndex)) return;

			SeatSnapshot seat = _seats[seatIndex];
			if (seat.Status != SeatStatus.Seated) return;
			if (seat.IsSittingOut == isSittingOut) return;

			SeatSnapshot changed = Copy(seat);
			changed.IsSittingOut = isSittingOut;
			_seats[seatIndex] = changed;
			Changed?.Invoke();
		}

		public string NameAt(int seatIndex)
		{
			if (seatIndex < 0 || seatIndex >= _seats.Length) return "";

			return _seats[seatIndex].PlayerName;
		}

		public void ReleaseSeat(string tableId, int seatIndex)
		{
			if (!IsCurrent(tableId, seatIndex)) return;

			_seats[seatIndex] = new SeatSnapshot { Index = seatIndex, Status = SeatStatus.Empty };
			Changed?.Invoke();
		}

		public int FindSeatOf(string playerId)
		{
			foreach (SeatSnapshot seat in _seats)
			{
				if (seat.Status == SeatStatus.Empty) continue;
				if (seat.PlayerId != playerId) continue;

				return seat.Index;
			}

			return -1;
		}

		private static SeatSnapshot Copy(SeatSnapshot seat) => new SeatSnapshot
		{
			Index = seat.Index,
			Status = seat.Status,
			PlayerId = seat.PlayerId,
			PlayerName = seat.PlayerName,
			Stack = seat.Stack,
			ReservedUntilUnixMs = seat.ReservedUntilUnixMs,
			IsSittingOut = seat.IsSittingOut,
		};

		private bool IsCurrent(string tableId, int seatIndex)
		{
			if (tableId != TableId) return false;

			return seatIndex >= 0 && seatIndex < _seats.Length;
		}
	}
}