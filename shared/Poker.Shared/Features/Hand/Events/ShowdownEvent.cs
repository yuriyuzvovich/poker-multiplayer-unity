using System;
using Poker.Shared.Features.Hand.Models;

namespace Poker.Shared.Features.Hand.Events
{
	public sealed class ShowdownEvent
	{
		public string TableId { get; set; } = "";
		public long HandNumber { get; set; }
		public string[] Board { get; set; } = Array.Empty<string>();
		public CardReveal[] Reveals { get; set; } = Array.Empty<CardReveal>();
	}
}