using System;
using Poker.Client.Infrastructure.Network.Connection;

namespace Poker.Client.Infrastructure.Network.Requests
{
	public static class RequestErrors
	{
		public static bool IsExpected(Exception exception) =>
			exception is TimeoutException or RequestRejectedException or ConnectionClosedException;
	}
}