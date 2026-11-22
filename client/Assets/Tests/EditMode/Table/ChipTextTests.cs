using NUnit.Framework;
using Poker.Client.Features.Table.Views.Formatting;

namespace Poker.Client.Tests.EditMode.Table
{
	public sealed class ChipTextTests
	{
		[TestCase(0, "0")]
		[TestCase(40, "40")]
		[TestCase(9_999, "9,999")]
		[TestCase(10_000, "10K")]
		[TestCase(12_500, "12.5K")]
		[TestCase(12_950, "12.9K")]
		[TestCase(999_999, "999.9K")]
		[TestCase(1_000_000, "1M")]
		[TestCase(1_250_000, "1.2M")]
		public void Format_KeepsSmallAmountsExact_AndShortensLargeOnesWithoutRoundingUp(long chips, string expected) =>
			Assert.AreEqual(expected, ChipText.Format(chips));
	}
}