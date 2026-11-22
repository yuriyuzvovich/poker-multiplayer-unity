using System;
using System.Text.Json;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Server.Infrastructure.Messaging
{
	public sealed class JsonPayloadSerializer : IPayloadSerializer
	{
		private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

		public byte[] Serialize(object payload, Type type) => JsonSerializer.SerializeToUtf8Bytes(payload, type, Options);

		public object Deserialize(ArraySegment<byte> payload, Type type)
		{
			object? result;
			try
			{
				result = JsonSerializer.Deserialize(payload.AsSpan(), type, Options);
			}
			catch (JsonException exception)
			{
				throw new PayloadFormatException($"Invalid {type.Name} payload.", exception);
			}

			return result ?? throw new PayloadFormatException($"Null {type.Name} payload.", new ArgumentNullException(nameof(payload)));
		}
	}
}