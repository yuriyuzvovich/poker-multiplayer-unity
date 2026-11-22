using System.Collections.Generic;
using Xunit;
using Poker.Shared.Rules.Pots;

namespace Poker.Tests.Shared.Rules
{
	public sealed class PotCalculatorTests
	{
		[Fact]
		public void EqualContributions_MakeOneMainPot()
		{
			List<Pot> pots = PotCalculator.Calculate(new[]
			{
				new PotContribution(0, 100, false),
				new PotContribution(1, 100, false),
				new PotContribution(2, 100, false),
			});

			Pot pot = Assert.Single(pots);
			Assert.Equal(300, pot.Amount);
			Assert.Equal(new[] { 0, 1, 2 }, pot.EligibleSeats);
		}

		[Fact]
		public void ShortAllIn_CreatesAMainPotAndASidePot()
		{
			List<Pot> pots = PotCalculator.Calculate(new[]
			{
				new PotContribution(0, 50, false),
				new PotContribution(1, 200, false),
				new PotContribution(2, 200, false),
			});

			Assert.Equal(2, pots.Count);
			Assert.Equal(150, pots[0].Amount);
			Assert.Equal(new[] { 0, 1, 2 }, pots[0].EligibleSeats);
			Assert.Equal(300, pots[1].Amount);
			Assert.Equal(new[] { 1, 2 }, pots[1].EligibleSeats);
		}

		[Fact]
		public void FoldedChips_StayInThePot_ButTheFolderIsNotEligible()
		{
			List<Pot> pots = PotCalculator.Calculate(new[]
			{
				new PotContribution(0, 60, true),
				new PotContribution(1, 100, false),
				new PotContribution(2, 100, false),
			});

			Pot pot = Assert.Single(pots);
			Assert.Equal(260, pot.Amount);
			Assert.Equal(new[] { 1, 2 }, pot.EligibleSeats);
		}

		[Fact]
		public void UncalledExcess_IsAPotOnlyItsOwnerCanWin()
		{
			List<Pot> pots = PotCalculator.Calculate(new[]
			{
				new PotContribution(0, 1000, false),
				new PotContribution(1, 300, false),
			});

			Assert.Equal(2, pots.Count);
			Assert.Equal(600, pots[0].Amount);
			Assert.Equal(700, pots[1].Amount);
			Assert.Equal(new[] { 0 }, pots[1].EligibleSeats);
		}

		[Fact]
		public void ThreeAllInLevels_ConserveEveryChip()
		{
			PotContribution[] contributions =
			{
				new(0, 30, false),
				new(1, 80, false),
				new(2, 200, false),
				new(3, 200, false),
				new(4, 120, true),
			};

			List<Pot> pots = PotCalculator.Calculate(contributions);

			long total = 0;
			foreach (Pot pot in pots)
			{
				total += pot.Amount;
			}

			Assert.Equal(630, total);
			Assert.Equal(new[] { 0, 1, 2, 3 }, pots[0].EligibleSeats);
			Assert.Equal(new[] { 1, 2, 3 }, pots[1].EligibleSeats);
			Assert.Equal(new[] { 2, 3 }, pots[2].EligibleSeats);
		}
	}
}