using System;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public static class MessageTypeId
	{
		public static uint Compute(string name)
		{
			uint hash = 2166136261;
			foreach (char character in name)
			{
				if (character > 127) throw new ArgumentException("Message names must be ASCII: " + name, nameof(name));

				hash ^= character;
				hash *= 16777619;
			}

			return hash;
		}
	}
}