namespace Poker.Client.Infrastructure.Network.DevTools
{
	public sealed class MessageTypeStats
	{
		public MessageTypeStats(string name) => Name = name;

		public string Name { get; }
		public long Count { get; private set; }
		public long Bytes { get; private set; }
		public double MaxHandleMs { get; private set; }

		public void Add(int bytes, double handleMs)
		{
			Count++;
			Bytes += bytes;
			if (handleMs > MaxHandleMs) MaxHandleMs = handleMs;
		}
	}
}