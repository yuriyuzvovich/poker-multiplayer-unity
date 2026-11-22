using Poker.Shared.Features.Hand;
using Poker.Shared.Features.HandHistory;
using Poker.Shared.Features.Lobby;
using Poker.Shared.Features.Seating;
using Poker.Shared.Features.Table;
using Poker.Shared.Features.TurnTimer;
using Poker.Shared.Features.Wallet;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Shared
{
	public static class MessageCatalog
	{
		public static MessageRegistry CreateRegistry()
		{
			MessageRegistry registry = new();
			ProtocolMessages.Register(registry);
			LobbyMessages.Register(registry);
			TableMessages.Register(registry);
			SeatingMessages.Register(registry);
			HandMessages.Register(registry);
			HandHistoryMessages.Register(registry);
			TurnTimerMessages.Register(registry);
			WalletMessages.Register(registry);
			return registry;
		}
	}
}