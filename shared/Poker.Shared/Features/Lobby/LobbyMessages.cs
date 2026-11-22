using Poker.Shared.Features.Lobby.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.Lobby
{
	public static class LobbyMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<ListTablesRequest>(LobbyMessageNames.ListTables);
			registry.Register<TableListResponse>(LobbyMessageNames.TableList);
		}
	}
}