using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poker.Client.Infrastructure.Network.Messaging;
using Poker.Shared;
using Poker.Shared.Infrastructure.Protocol.Messages;
using UnityEngine;

namespace Poker.Client.Tests.EditMode.Hand
{
	public sealed class RecordedHand
	{
		private const string FixturePath = "Tests/EditMode/Fixtures/heads-up-hand.json";

		private RecordedHand(string heroPlayerId, List<RecordedMessage> messages)
		{
			HeroPlayerId = heroPlayerId;
			Messages = messages;
		}

		public string HeroPlayerId { get; }
		public IReadOnlyList<RecordedMessage> Messages { get; }

		public static RecordedHand Load() => LoadFrom(Path.Combine(Application.dataPath, FixturePath));

		public static RecordedHand LoadFrom(string path)
		{
			JObject root = JObject.Parse(File.ReadAllText(path));
			MessageRegistry registry = MessageCatalog.CreateRegistry();
			NewtonsoftPayloadSerializer serializer = new();
			List<RecordedMessage> messages = new();
			foreach (JToken token in (JArray)root["messages"]!)
			{
				string name = (string)token["type"]!;
				if (!registry.TryGet(MessageTypeId.Compute(name), out MessageType type)) throw new InvalidDataException("Unknown message " + name);

				byte[] payload = Encoding.UTF8.GetBytes(token["payload"]!.ToString(Formatting.None));
				object message = serializer.Deserialize(new ArraySegment<byte>(payload), type.ClrType);
				messages.Add(new RecordedMessage(name, (ushort)token["requestId"]!, message));
			}

			return new RecordedHand((string)root["heroPlayerId"]!, messages);
		}

		public int IndexOf(Func<object, bool> predicate, int occurrence = 1)
		{
			int seen = 0;
			for (int i = 0; i < Messages.Count; i++)
			{
				if (!predicate(Messages[i].Payload)) continue;

				seen++;
				if (seen == occurrence) return i;
			}

			throw new InvalidDataException("The recorded hand has no such message.");
		}

		public T Last<T>() where T : class
		{
			for (int i = Messages.Count - 1; i >= 0; i--)
			{
				if (Messages[i].Payload is not T typed) continue;

				return typed;
			}

			throw new InvalidDataException("The recorded hand has no " + typeof(T).Name + ".");
		}
	}
}