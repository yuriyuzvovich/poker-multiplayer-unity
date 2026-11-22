using System;

namespace Poker.Client.Infrastructure.Network.Connection
{
	public sealed class ConnectionClosedException : Exception
	{
		public ConnectionClosedException(string reason) : base("Connection closed: " + reason) { }
	}
}