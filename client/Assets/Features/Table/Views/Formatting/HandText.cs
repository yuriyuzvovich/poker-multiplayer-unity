using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;


namespace Poker.Client.Features.Table.Views.Formatting
{
	public static class HandText
	{
		public static string Action(PlayerAction action, long amount) => action switch
		{
			PlayerAction.Fold => "fold",
			PlayerAction.Check => "check",
			PlayerAction.Call => "call " + amount,
			_ => "raise to " + amount,
		};

		public static string StreetName(Street street) => street switch
		{
			Street.Preflop => "preflop",
			Street.Flop => "flop",
			Street.Turn => "turn",
			Street.River => "river",
			_ => "showdown",
		};
	}
}