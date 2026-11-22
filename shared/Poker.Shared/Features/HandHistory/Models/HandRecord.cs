using System;

namespace Poker.Shared.Features.HandHistory.Models
{
	public sealed class HandRecord
	{
		public string TableName { get; set; } = "";
		public long HandNumber { get; set; }
		public long EndedAtUnixMs { get; set; }
		public string[] Board { get; set; } = Array.Empty<string>();
		public long NetChips { get; set; }
		public HandRecordPlayer[] Players { get; set; } = Array.Empty<HandRecordPlayer>();
		public HandRecordAction[] Actions { get; set; } = Array.Empty<HandRecordAction>();
	}
}