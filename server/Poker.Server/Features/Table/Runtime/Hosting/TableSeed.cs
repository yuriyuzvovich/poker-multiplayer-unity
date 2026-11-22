using System;
using System.Collections.Generic;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Infrastructure.Transport;

namespace Poker.Server.Features.Table.Runtime.Hosting
{
	public static class TableSeed
	{
		public const string PracticeTableId = "practice";
		public const string HeadsUpTableId = "heads-up";

		public static List<PokerTable> CreateTables(IChipBank bank)
		{
			TimeSpan reservationTime = TimeSpan.FromSeconds(20);

			PokerTable practice = new(new TableDefinition(PracticeTableId, "Practice table", 6, 10, 20, 400, 2000, reservationTime), bank);
			practice.SeatBot(0, new PlayerIdentity("bot-ada", "Ada"), 5000);
			practice.SeatBot(2, new PlayerIdentity("bot-linus", "Linus"), 3200);
			practice.SeatBot(4, new PlayerIdentity("bot-grace", "Grace"), 7450);

			PokerTable headsUp = new(new TableDefinition(HeadsUpTableId, "Heads-up table", 2, 50, 100, 2000, 10000, reservationTime), bank);
			headsUp.SeatBot(0, new PlayerIdentity("bot-edsger", "Edsger"), 2500);

			return new List<PokerTable> { practice, headsUp };
		}
	}
}