using System;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Features.Table.Models;

namespace Poker.Server.Features.Table.Domain
{
	public sealed class Seat
	{
		public Seat(int index) => Index = index;

		public int Index { get; }
		public SeatStatus Status { get; private set; }
		public PlayerIdentity? Player { get; private set; }
		public long Stack { get; private set; }
		public DateTimeOffset? ReservedUntil { get; private set; }
		public bool IsSittingOut { get; private set; }
		public bool IsInHand { get; private set; }
		public SeatReleaseReason? PendingRelease { get; private set; }
		public bool IsLeaving => PendingRelease != null;

		public bool IsHeldBy(string playerId) => Player != null && Player.PlayerId == playerId;

		public SeatSnapshot ToSnapshot() => new()
		{
			Index = Index,
			Status = Status,
			PlayerId = Player?.PlayerId ?? "",
			PlayerName = Player?.Name ?? "",
			Stack = Stack,
			ReservedUntilUnixMs = ReservedUntil?.ToUnixTimeMilliseconds() ?? 0,
			IsSittingOut = IsSittingOut,
		};

		internal void Reserve(PlayerIdentity player, DateTimeOffset until)
		{
			Status = SeatStatus.Reserved;
			Player = player;
			Stack = 0;
			ReservedUntil = until;
		}

		internal void Sit(long stack)
		{
			Status = SeatStatus.Seated;
			Stack = stack;
			ReservedUntil = null;
			IsSittingOut = false;
		}

		internal void SetStack(long stack) => Stack = stack;

		internal void SetSittingOut(bool isSittingOut) => IsSittingOut = isSittingOut;

		internal void SetInHand(bool isInHand) => IsInHand = isInHand;

		internal void MarkLeaving(SeatReleaseReason reason) => PendingRelease = reason;

		internal void SitDirectly(PlayerIdentity player, long stack)
		{
			Player = player;
			Sit(stack);
		}

		internal void Release()
		{
			Status = SeatStatus.Empty;
			Player = null;
			Stack = 0;
			ReservedUntil = null;
			IsSittingOut = false;
			IsInHand = false;
			PendingRelease = null;
		}
	}
}