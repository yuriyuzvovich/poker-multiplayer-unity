using System.Text;
using Poker.Client.Features.Table.Views.Formatting;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.HandHistory.Models;

namespace Poker.Client.Features.HandHistory.Presentation
{
	public static class HandHistoryText
	{
		public static string Format(HandRecord hand)
		{
			StringBuilder text = new();
			text.Append("Hand #").Append(hand.HandNumber).Append(", ").Append(hand.TableName).Append("   ").Append(Signed(hand.NetChips)).Append('\n');
			foreach (HandRecordPlayer player in hand.Players)
			{
				if (!player.IsMe) continue;

				text.Append("You: ").Append(Cards(player.Cards));
			}

			text.Append("   Board: ").Append(hand.Board.Length > 0 ? Cards(hand.Board) : "none").Append('\n');
			foreach (HandRecordPlayer player in hand.Players)
			{
				if (player.Cards.Length == 0 && player.Won == 0) continue;
				if (player.IsMe && player.Won == 0) continue;

				text.Append(player.IsMe ? "You" : player.Name);
				if (!player.IsMe && player.Cards.Length > 0) text.Append(" showed ").Append(Cards(player.Cards));
				if (player.Description.Length > 0) text.Append(" (").Append(player.Description).Append(')');
				if (player.Won > 0) text.Append(" won ").Append(ChipText.Format(player.Won));

				text.Append('\n');
			}

			Street street = Street.Showdown;
			for (int i = 0; i < hand.Actions.Length; i++)
			{
				HandRecordAction action = hand.Actions[i];
				if (action.Street != street)
				{
					street = action.Street;
					text.Append(i == 0 ? "" : "; ").Append(HandText.StreetName(street)).Append(": ");
				}
				else
				{
					text.Append(", ");
				}

				text.Append(action.Name).Append(' ').Append(HandText.Action(action.Action, action.Amount));
			}

			return text.ToString();
		}

		private static string Signed(long chips) => chips > 0 ? "+" + ChipText.Format(chips) : chips < 0 ? "-" + ChipText.Format(-chips) : "0";

		private static string Cards(string[] cards) => string.Join(" ", cards);
	}
}