using System.Collections.Generic;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Rules.Cards;

namespace Poker.Server.Features.Hand.Domain
{
	public sealed class HandPlayer
	{
		private readonly List<Card> _holeCards = new(2);

		public HandPlayer(HandParticipant participant)
		{
			SeatIndex = participant.SeatIndex;
			PlayerId = participant.PlayerId;
			Stack = participant.Stack;
		}

		public int SeatIndex { get; }
		public string PlayerId { get; }
		public long Stack { get; private set; }
		public long Committed { get; private set; }
		public long TotalCommitted { get; private set; }
		public bool IsFolded { get; internal set; }
		public bool IsAllIn => Stack == 0 && !IsFolded;
		public bool HasActed { get; internal set; }

		public bool CanAct => !IsFolded && Stack > 0;

		public IReadOnlyList<Card> HoleCards => _holeCards;

		public HandPlayerInfo ToInfo() => new()
		{
			SeatIndex = SeatIndex,
			PlayerId = PlayerId,
			Stack = Stack,
			Committed = Committed,
			IsFolded = IsFolded,
			IsAllIn = IsAllIn,
		};

		internal void Deal(Card card) => _holeCards.Add(card);

		internal long Commit(long amount)
		{
			long paid = amount < Stack ? amount : Stack;
			Stack -= paid;
			Committed += paid;
			TotalCommitted += paid;
			return paid;
		}

		internal void EndStreet()
		{
			Committed = 0;
			HasActed = false;
		}

		internal void Win(long amount) => Stack += amount;
	}
}