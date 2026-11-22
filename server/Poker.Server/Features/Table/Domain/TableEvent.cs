namespace Poker.Server.Features.Table.Domain
{
	public readonly record struct TableEvent(object Message, string? RecipientPlayerId)
	{
		public bool IsPublic => RecipientPlayerId == null;
	}
}