using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Client.Infrastructure.Network.DevTools
{
	public sealed class TranscriptRecorder : IPacketTap
	{
		private readonly Queue<TranscriptEntry> _entries = new();
		private readonly string _baselineMessage;
		private readonly int _capacity;

		// A capacity of 0 turns recording off; release builds pass 0 so no packet is copied into a string.
		public TranscriptRecorder(string baselineMessage, int capacity = 10000)
		{
			_baselineMessage = baselineMessage ?? throw new ArgumentNullException(nameof(baselineMessage));
			if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

			_capacity = capacity;
		}

		public bool IsEnabled => _capacity > 0;

		public int Count => _entries.Count;

		public bool StartsAtBaseline { get; private set; }

		void IPacketTap.OnReceived(MessageType type, ushort requestId, ArraySegment<byte> payload) => Record(type, requestId, payload);

		public void Record(MessageType type, ushort requestId, ArraySegment<byte> payload)
		{
			if (!IsEnabled) return;
			if (ProtocolMessages.IsUnnumbered(type)) return;

			if (type.Name == _baselineMessage)
			{
				_entries.Clear();
				StartsAtBaseline = true;
			}

			if (_entries.Count >= _capacity)
			{
				_entries.Dequeue();
				StartsAtBaseline = false;
			}

			_entries.Enqueue(new TranscriptEntry(type.Name, requestId, Encoding.UTF8.GetString(payload.Array!, payload.Offset, payload.Count)));
		}

		public void WriteTo(TextWriter writer, string heroPlayerId)
		{
			using JsonTextWriter json = new(writer) { Formatting = Formatting.Indented, CloseOutput = false };
			json.WriteStartObject();
			json.WritePropertyName("heroPlayerId");
			json.WriteValue(heroPlayerId);
			json.WritePropertyName("startsAtBaseline");
			json.WriteValue(StartsAtBaseline);
			json.WritePropertyName("messages");
			json.WriteStartArray();
			foreach (TranscriptEntry entry in _entries)
			{
				json.WriteStartObject();
				json.WritePropertyName("type");
				json.WriteValue(entry.Type);
				json.WritePropertyName("requestId");
				json.WriteValue(entry.RequestId);
				json.WritePropertyName("payload");
				json.WriteRawValue(entry.PayloadJson);
				json.WriteEndObject();
			}

			json.WriteEndArray();
			json.WriteEndObject();
		}
	}
}