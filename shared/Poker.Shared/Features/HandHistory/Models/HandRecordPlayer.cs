using System;

namespace Poker.Shared.Features.HandHistory.Models
{
	public sealed class HandRecordPlayer
	{
		public string Name { get; set; } = "";
		public bool IsMe { get; set; }
		public string[] Cards { get; set; } = Array.Empty<string>();
		public string Description { get; set; } = "";
		public long Won { get; set; }
	}
}