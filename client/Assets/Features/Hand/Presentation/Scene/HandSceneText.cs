using System.Collections.Generic;
using System.Text;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Views.Formatting;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;

namespace Poker.Client.Features.Hand.Presentation.Scene
{
	// The words the hand layer shows. Callers pass their own StringBuilder so repeated formatting reuses one buffer.
	public static class HandSceneText
	{
		public static string Title(long handNumber, string stage) => "Hand #" + handNumber + ", " + stage;

		public static string Pots(PotInfo[] pots, StringBuilder text)
		{
			if (pots.Length == 0) return "";

			text.Clear();
			text.Append(pots.Length == 1 ? "Pot " : "Pots ");
			for (int i = 0; i < pots.Length; i++)
			{
				if (i > 0) text.Append(" + ");

				text.Append(ChipText.Format(pots[i].Amount));
			}

			return text.ToString();
		}

		public static string Winners(IReadOnlyList<HandSeatLine> lines, PotAward[] awards, StringBuilder text)
		{
			text.Clear();
			for (int i = 0; i < lines.Count; i++)
			{
				HandSeatLine line = lines[i];
				long won = 0;
				foreach (PotAward award in awards)
				{
					if (award.SeatIndex != line.SeatIndex) continue;

					won += award.Amount;
				}

				if (won == 0) continue;

				if (text.Length > 0) text.Append(", ");

				text.Append(line.Name).Append(" wins ").Append(ChipText.Format(won));
				if (line.Description.Length > 0) text.Append(" with ").Append(line.Description);
			}

			return text.ToString();
		}

		public static string Action(TurnActedEvent acted)
		{
			string action = HandText.Action(acted.Action, acted.Action == PlayerAction.Raise ? acted.Committed : acted.Amount);
			if (acted.IsAllIn) action += ", all-in";

			return acted.Reason switch
			{
				ActionReason.Timeout => action + " (timeout)",
				ActionReason.LeftTable => action + " (left)",
				_ => action,
			};
		}
	}
}