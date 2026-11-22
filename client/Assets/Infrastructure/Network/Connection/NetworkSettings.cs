using System;
using Poker.Client.Infrastructure.Network.Session;

namespace Poker.Client.Infrastructure.Network.Connection
{
	public sealed class NetworkSettings
	{
		public NetworkSettings(Uri serverUri, TimeSpan requestTimeout, string playerName, string deviceKey, TimeSpan simulatedRoundTrip)
		{
			ServerUri = serverUri ?? throw new ArgumentNullException(nameof(serverUri));
			RequestTimeout = requestTimeout;
			PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
			DeviceKey = deviceKey ?? throw new ArgumentNullException(nameof(deviceKey));
			SimulatedRoundTrip = simulatedRoundTrip;
		}

		public Uri ServerUri { get; }
		public TimeSpan RequestTimeout { get; }
		public string PlayerName { get; }
		public string DeviceKey { get; }
		public TimeSpan SimulatedRoundTrip { get; }
	}
}