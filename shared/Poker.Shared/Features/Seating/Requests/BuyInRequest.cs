namespace Poker.Shared.Features.Seating.Requests
{
	public sealed class BuyInRequest
	{
		public string TableId { get; set; } = "";
		public long Amount { get; set; }
	}
}