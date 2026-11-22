namespace Poker.Client.Infrastructure.Network.DevTools
{
	public readonly struct TranscriptEntry
	{
		public TranscriptEntry(string type, ushort requestId, string payloadJson)
		{
			Type = type;
			RequestId = requestId;
			PayloadJson = payloadJson;
		}

		public string Type { get; }
		public ushort RequestId { get; }
		public string PayloadJson { get; }
	}
}