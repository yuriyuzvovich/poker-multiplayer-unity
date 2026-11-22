using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Poker.Client.Infrastructure.Network.DevTools;
using Poker.Client.Infrastructure.Network.Messaging;
using Poker.Shared;
using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.Packets;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Client.Tests.EditMode.Diagnostics
{
	public sealed class TranscriptRecorderTests
	{
		private readonly MessageRegistry _registry = MessageCatalog.CreateRegistry();
		private MessageCodec _codec;
		private TranscriptRecorder _recorder;

		[SetUp]
		public void SetUp()
		{
			_codec = new MessageCodec(_registry, new NewtonsoftPayloadSerializer());
			_recorder = new TranscriptRecorder(TableMessageNames.Snapshot, capacity: 3);
		}

		[Test]
		public void ZeroCapacity_RecordsNothing()
		{
			_recorder = new TranscriptRecorder(TableMessageNames.Snapshot, capacity: 0);

			Record(new TableSnapshot { TableId = "heads-up" }, 5);

			Assert.IsFalse(_recorder.IsEnabled);
			Assert.AreEqual(0, _recorder.Count);
		}

		[Test]
		public void TableSnapshot_StartsANewTranscript_AndSystemMessagesAreLeftOut()
		{
			Record(new TableListResponse(), 4);
			Record(new TableSnapshot { TableId = "heads-up" }, 5);
			Record(new PongResponse(), 6);
			Record(new SeatReservedEvent { TableId = "heads-up" }, 0);

			JObject written = Write();

			Assert.IsTrue((bool)written["startsAtBaseline"]);
			JArray messages = (JArray)written["messages"];
			Assert.AreEqual(2, messages.Count);
			Assert.AreEqual("table.snapshot", (string)messages[0]["type"]);
			Assert.AreEqual(5, (int)messages[0]["requestId"]);
			Assert.AreEqual("heads-up", (string)messages[0]["payload"]["tableId"]);
			Assert.AreEqual("p-hero", (string)written["heroPlayerId"]);
		}

		[Test]
		public void Overflow_DropsTheOldest_AndMarksTheTranscriptAsNotReplayable()
		{
			Record(new TableSnapshot(), 1);
			for (int i = 0; i < 3; i++)
			{
				Record(new SeatReservedEvent(), 0);
			}

			Assert.AreEqual(3, _recorder.Count);
			Assert.IsFalse(_recorder.StartsAtBaseline);
		}

		private void Record<TMessage>(TMessage message, ushort requestId) where TMessage : class
		{
			PacketCodec.TryDecode(new ArraySegment<byte>(_codec.Encode(message, requestId: requestId)), out PacketHeader header, out ArraySegment<byte> payload);
			_registry.TryGet(header.TypeId, out MessageType type);
			_recorder.Record(type, header.RequestId, payload);
		}

		private JObject Write()
		{
			using StringWriter writer = new();
			_recorder.WriteTo(writer, "p-hero");
			return JObject.Parse(writer.ToString());
		}
	}
}