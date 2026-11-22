using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Client.Infrastructure.Network.Messaging
{
	public sealed class NewtonsoftPayloadSerializer : IPayloadSerializer
	{
		private static readonly UTF8Encoding Utf8 = new(false);

		private readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
		});

		public byte[] Serialize(object payload, Type type)
		{
			using MemoryStream stream = new();
			using (StreamWriter writer = new(stream, Utf8))
			{
				_serializer.Serialize(writer, payload, type);
			}

			return stream.ToArray();
		}

		public object Deserialize(ArraySegment<byte> payload, Type type)
		{
			using MemoryStream stream = new(payload.Array, payload.Offset, payload.Count, false);
			using StreamReader reader = new(stream, Utf8);
			using JsonTextReader json = new(reader);
			object result;
			try
			{
				result = _serializer.Deserialize(json, type);
			}
			catch (JsonException exception)
			{
				throw new PayloadFormatException("Invalid " + type.Name + " payload.", exception);
			}

			return result ?? throw new PayloadFormatException("Null " + type.Name + " payload.", new ArgumentNullException(nameof(payload)));
		}
	}
}