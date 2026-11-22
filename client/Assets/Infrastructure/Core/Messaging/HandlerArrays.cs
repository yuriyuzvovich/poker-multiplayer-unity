using System;

namespace Poker.Client.Infrastructure.Core.Messaging
{
	// Copy-on-write handler lists: dispatch walks an array that subscribing never changes in place.
	public static class HandlerArrays
	{
		public static T[] Add<T>(T[] existing, T handler) where T : class
		{
			if (existing == null) return new[] { handler };

			T[] grown = new T[existing.Length + 1];
			Array.Copy(existing, grown, existing.Length);
			grown[existing.Length] = handler;
			return grown;
		}

		public static T[] Remove<T>(T[] existing, T handler) where T : class
		{
			int index = Array.IndexOf(existing, handler);
			if (index < 0) return existing;

			T[] shrunk = new T[existing.Length - 1];
			Array.Copy(existing, 0, shrunk, 0, index);
			Array.Copy(existing, index + 1, shrunk, index, existing.Length - index - 1);
			return shrunk;
		}
	}
}