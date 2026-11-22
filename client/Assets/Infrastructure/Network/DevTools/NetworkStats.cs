using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Poker.Client.Infrastructure.Network.DevTools
{
	public sealed class NetworkStats
	{
		private readonly Dictionary<string, MessageTypeStats> _byType = new();
		private long _bytesOut;
		private long _packetsOut;

		public long BytesIn { get; private set; }
		public long PacketsIn { get; private set; }
		public long BytesOut => Interlocked.Read(ref _bytesOut);
		public long PacketsOut => Interlocked.Read(ref _packetsOut);
		public long Resent { get; private set; }
		public long HopCount { get; private set; }
		public double HopTotalMs { get; private set; }
		public double HopMaxMs { get; private set; }

		public static double MillisecondsSince(long timestamp) => (Stopwatch.GetTimestamp() - timestamp) * 1000.0 / Stopwatch.Frequency;

		public void AddSent(int bytes)
		{
			Interlocked.Add(ref _bytesOut, bytes);
			Interlocked.Increment(ref _packetsOut);
		}

		public void AddReceived(int bytes)
		{
			BytesIn += bytes;
			PacketsIn++;
		}

		public void AddResent(int packets) => Resent += packets;

		public void AddHop(double milliseconds)
		{
			HopCount++;
			HopTotalMs += milliseconds;
			if (milliseconds > HopMaxMs) HopMaxMs = milliseconds;
		}

		public void AddHandled(string type, int bytes, double handleMs)
		{
			if (!_byType.TryGetValue(type, out MessageTypeStats stats))
			{
				stats = new MessageTypeStats(type);
				_byType.Add(type, stats);
			}

			stats.Add(bytes, handleMs);
		}

		public int CopyTopByBytes(MessageTypeStats[] top)
		{
			int filled = 0;
			foreach (MessageTypeStats stats in _byType.Values)
			{
				int slot = filled;
				while (slot > 0 && top[slot - 1].Bytes < stats.Bytes) slot--;
				if (slot >= top.Length) continue;

				for (int i = Math.Min(filled, top.Length - 1); i > slot; i--)
				{
					top[i] = top[i - 1];
				}

				top[slot] = stats;
				if (filled < top.Length) filled++;
			}

			return filled;
		}

		public MessageTypeStats FindSlowest()
		{
			MessageTypeStats slowest = null;
			foreach (MessageTypeStats stats in _byType.Values)
			{
				if (slowest != null && stats.MaxHandleMs <= slowest.MaxHandleMs) continue;

				slowest = stats;
			}

			return slowest;
		}
	}
}