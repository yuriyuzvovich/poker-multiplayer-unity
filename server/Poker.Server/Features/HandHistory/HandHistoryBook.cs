using System;
using System.Collections.Generic;
using Poker.Shared.Features.HandHistory.Models;

namespace Poker.Server.Features.HandHistory
{
	public sealed class HandHistoryBook
	{
		private readonly object _lock = new object();
		private readonly Dictionary<string, List<HandRecord>> _byPlayer = new();

		public void Add(string playerId, HandRecord record)
		{
			lock (_lock)
			{
				if (!_byPlayer.TryGetValue(playerId, out List<HandRecord>? hands))
				{
					hands = new List<HandRecord>();
					_byPlayer.Add(playerId, hands);
				}

				hands.Insert(0, record);
				if (hands.Count > 20) hands.RemoveAt(hands.Count - 1);
			}
		}

		public HandRecord[] Get(string playerId)
		{
			lock (_lock) return _byPlayer.TryGetValue(playerId, out List<HandRecord>? hands) ? hands.ToArray() : Array.Empty<HandRecord>();
		}
	}
}