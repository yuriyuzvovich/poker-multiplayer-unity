using Poker.Shared.Features.Table.Events;
using Poker.Shared.Features.Table.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;


namespace Poker.Shared.Features.Table
{
	public static class TableMessages
	{
		public static void Register(MessageRegistry registry)
		{
			registry.Register<JoinTableRequest>(TableMessageNames.JoinTable);
			registry.Register<TableSnapshot>(TableMessageNames.Snapshot);
			registry.Register<SeatReservedEvent>(TableMessageNames.SeatReserved);
			registry.Register<PlayerSeatedEvent>(TableMessageNames.PlayerSeated);
			registry.Register<SeatReleasedEvent>(TableMessageNames.SeatReleased);
		}
	}
}