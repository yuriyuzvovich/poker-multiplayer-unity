using System;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Server.Features.HandHistory
{
	public sealed class HandHistoryListener : ITableEventListener
	{
		private readonly HandHistoryBook _book;

		public HandHistoryListener(HandHistoryBook book) => _book = book ?? throw new ArgumentNullException(nameof(book));

		public void OnEvent(TableContext context, TableEvent tableEvent)
		{
			HandHistoryDraft draft = context.GetState<HandHistoryDraft>();
			switch (tableEvent.Message)
			{
				case HandStartedEvent started:
					draft.Start(started, context.Table);
					break;
				case HoleCardsEvent cards:
					draft.SetHoleCards(cards);
					break;
				case TurnActedEvent acted:
					draft.AddAction(acted);
					break;
				case RoundStartedEvent round:
					draft.StartRound(round);
					break;
				case ShowdownEvent showdown:
					draft.Reveal(showdown);
					break;
				case HandEndedEvent ended:
					draft.Finish(ended, context.Table.Definition.Name, context.Now, _book);
					break;
			}
		}
	}
}