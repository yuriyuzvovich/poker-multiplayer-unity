namespace Poker.Client.Tests.EditMode.Hand
{
	public readonly struct RecordedMessage
	{
		public RecordedMessage(string type, ushort requestId, object payload)
		{
			Type = type;
			RequestId = requestId;
			Payload = payload;
		}

		public string Type { get; }
		public ushort RequestId { get; }
		public object Payload { get; }
	}
}