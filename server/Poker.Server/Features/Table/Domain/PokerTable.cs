using System;
using System.Collections.Generic;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Lobby.Models;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;

namespace Poker.Server.Features.Table.Domain
{
	public sealed class PokerTable
	{
		private readonly Seat[] _seats;
		private readonly List<TableEvent> _events = new();
		private readonly IChipBank _bank;

		public PokerTable(TableDefinition definition, IChipBank? bank = null)
		{
			Definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_bank = bank ?? new UnlimitedChipBank();
			_seats = new Seat[definition.SeatCount];
			for (int i = 0; i < _seats.Length; i++)
			{
				_seats[i] = new Seat(i);
			}
		}

		public TableDefinition Definition { get; }

		public IReadOnlyList<Seat> Seats => _seats;

		public SeatingResult Reserve(int seatIndex, PlayerIdentity player, DateTimeOffset now)
		{
			if (seatIndex < 0 || seatIndex >= _seats.Length) return SeatingResult.Failure(SeatErrorCodes.SeatInvalid);
			if (FindSeatOf(player.PlayerId) != null) return SeatingResult.Failure(SeatErrorCodes.AlreadySeated);

			Seat seat = _seats[seatIndex];
			if (seat.Status != SeatStatus.Empty) return SeatingResult.Failure(SeatErrorCodes.SeatTaken);

			seat.Reserve(player, now + Definition.ReservationTime);
			Publish(new SeatReservedEvent { TableId = Definition.TableId, Seat = seat.ToSnapshot() });
			return SeatingResult.Success(seat);
		}

		public SeatingResult BuyIn(string playerId, long amount, DateTimeOffset now)
		{
			Seat? seat = FindSeatOf(playerId);
			if (seat is not { Status: SeatStatus.Reserved }) return SeatingResult.Failure(SeatErrorCodes.NotReserved);

			if (seat.ReservedUntil <= now)
			{
				ReleaseSeat(seat, SeatReleaseReason.ReservationExpired);
				return SeatingResult.Failure(SeatErrorCodes.ReservationExpired);
			}

			if (amount < Definition.MinBuyIn || amount > Definition.MaxBuyIn) return SeatingResult.Failure(SeatErrorCodes.BuyInOutOfRange);
			if (!_bank.TryWithdraw(playerId, amount)) return SeatingResult.Failure(SeatErrorCodes.InsufficientFunds);

			seat.Sit(amount);
			Publish(new PlayerSeatedEvent { TableId = Definition.TableId, Seat = seat.ToSnapshot() });
			return SeatingResult.Success(seat);
		}

		public SeatingResult StandUp(string playerId) => ReleasePlayer(playerId, SeatReleaseReason.StoodUp);

		public SeatingResult ReleasePlayer(string playerId, SeatReleaseReason reason)
		{
			Seat? seat = FindSeatOf(playerId);
			if (seat == null) return SeatingResult.Failure(SeatErrorCodes.NotSeated);

			if (seat.IsInHand)
			{
				seat.MarkLeaving(reason);
				return SeatingResult.Success(seat);
			}

			ReleaseSeat(seat, reason);
			return SeatingResult.Success(seat);
		}

		public void ExpireReservations(DateTimeOffset now)
		{
			foreach (Seat seat in _seats)
			{
				if (seat.Status != SeatStatus.Reserved) continue;
				if (seat.ReservedUntil > now) continue;

				ReleaseSeat(seat, SeatReleaseReason.ReservationExpired);
			}
		}

		public SeatingResult SetSittingOut(string playerId, bool isSittingOut)
		{
			Seat? seat = FindSeatOf(playerId);
			if (seat is not { Status: SeatStatus.Seated }) return SeatingResult.Failure(SeatErrorCodes.NotSeated);

			seat.SetSittingOut(isSittingOut);
			return SeatingResult.Success(seat);
		}

		public void EnterHand(int seatIndex) => _seats[seatIndex].SetInHand(true);

		public void LeaveHand(int seatIndex)
		{
			Seat seat = _seats[seatIndex];
			seat.SetInHand(false);
			if (seat.PendingRelease is not { } reason) return;

			ReleaseSeat(seat, reason);
		}

		public void SeatBot(int seatIndex, PlayerIdentity bot, long stack) => _seats[seatIndex].SitDirectly(bot, stack);

		public bool TrySetStack(int seatIndex, string playerId, long stack)
		{
			if (seatIndex < 0 || seatIndex >= _seats.Length) return false;

			Seat seat = _seats[seatIndex];
			if (seat.Status != SeatStatus.Seated) return false;
			if (!seat.IsHeldBy(playerId)) return false;

			seat.SetStack(stack);
			return true;
		}

		public void Publish(object message) => _events.Add(new TableEvent(message, null));

		public void Publish(TableEvent tableEvent) => _events.Add(tableEvent);

		public void TakeEvents(List<TableEvent> destination)
		{
			destination.AddRange(_events);
			_events.Clear();
		}

		public TableSnapshot CreateSnapshot()
		{
			SeatSnapshot[] seats = new SeatSnapshot[_seats.Length];
			for (int i = 0; i < _seats.Length; i++)
			{
				seats[i] = _seats[i].ToSnapshot();
			}

			return new TableSnapshot
			{
				TableId = Definition.TableId,
				Name = Definition.Name,
				SmallBlind = Definition.SmallBlind,
				BigBlind = Definition.BigBlind,
				MinBuyIn = Definition.MinBuyIn,
				MaxBuyIn = Definition.MaxBuyIn,
				Seats = seats,
			};
		}

		public TableSummary CreateSummary() => new()
		{
			TableId = Definition.TableId,
			Name = Definition.Name,
			SmallBlind = Definition.SmallBlind,
			BigBlind = Definition.BigBlind,
			SeatCount = _seats.Length,
			OccupiedSeats = CountOccupiedSeats(),
		};

		public int CountOccupiedSeats()
		{
			int occupied = 0;
			foreach (Seat seat in _seats)
			{
				if (seat.Status == SeatStatus.Empty) continue;

				occupied++;
			}

			return occupied;
		}

		public bool HoldersMatch(string[] holderIds)
		{
			int index = 0;
			foreach (Seat seat in _seats)
			{
				if (seat.Player == null) continue;
				if (index >= holderIds.Length) return false;
				if (holderIds[index] != seat.Player.PlayerId) return false;

				index++;
			}

			return index == holderIds.Length;
		}

		public string[] CollectHolderIds()
		{
			List<string> holders = new();
			foreach (Seat seat in _seats)
			{
				if (seat.Player == null) continue;

				holders.Add(seat.Player.PlayerId);
			}

			return holders.ToArray();
		}

		public Seat? FindSeatOf(string playerId)
		{
			foreach (Seat seat in _seats)
			{
				if (!seat.IsHeldBy(playerId)) continue;

				return seat;
			}

			return null;
		}

		private void ReleaseSeat(Seat seat, SeatReleaseReason reason)
		{
			if (seat.Status == SeatStatus.Seated && seat.Stack > 0) _bank.Deposit(seat.Player!.PlayerId, seat.Stack);

			seat.Release();
			Publish(new SeatReleasedEvent { TableId = Definition.TableId, SeatIndex = seat.Index, Reason = reason });
		}
	}
}