using System;

namespace Poker.Client.Infrastructure.Network.Session
{
	public sealed class PlayerSession
	{
		public event Action Established;

		public bool IsEstablished => PlayerId.Length > 0;

		public string SessionId { get; private set; } = "";
		public string PlayerId { get; private set; } = "";
		public string PlayerName { get; private set; } = "";

		public void Establish(string sessionId, string playerId, string playerName)
		{
			SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
			PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
			PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
			Established?.Invoke();
		}

		public bool IsMe(string playerId) => IsEstablished && playerId == PlayerId;
	}
}