using Poker.Server.Features.Table.Domain;

namespace Poker.Server.Features.Table.Runtime.Actor
{
	public interface ITableEventListener
	{
		public void OnEvent(TableContext context, TableEvent tableEvent);
	}
}