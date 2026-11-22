using System;
using Poker.Shared.Features.HandHistory.Models;

namespace Poker.Shared.Features.HandHistory.Requests
{
	public sealed class HandHistoryResponse
	{
		public HandRecord[] Hands { get; set; } = Array.Empty<HandRecord>();
	}
}