using System.Collections.Generic;

namespace Poker.Shared.Rules.Pots
{
	public static class PotCalculator
	{
		public static List<Pot> Calculate(IReadOnlyList<PotContribution> contributions)
		{
			List<long> levels = new();
			for (int i = 0; i < contributions.Count; i++)
			{
				PotContribution contribution = contributions[i];
				if (contribution.IsFolded) continue;
				if (contribution.Amount <= 0) continue;
				if (levels.Contains(contribution.Amount)) continue;

				levels.Add(contribution.Amount);
			}

			levels.Sort();
			List<Pot> pots = new();
			long previous = 0;
			foreach (long level in levels)
			{
				Pot pot = CreateLayer(contributions, previous, level);
				previous = level;
				if (pot.Amount == 0) continue;

				AddOrMerge(pots, pot);
			}

			long leftover = CollectAbove(contributions, previous);
			if (leftover > 0 && pots.Count > 0) pots[pots.Count - 1].Amount += leftover;

			return pots;
		}

		private static Pot CreateLayer(IReadOnlyList<PotContribution> contributions, long from, long to)
		{
			long amount = 0;
			List<int> eligible = new();
			for (int i = 0; i < contributions.Count; i++)
			{
				PotContribution contribution = contributions[i];
				amount += Clamp(contribution.Amount, to) - Clamp(contribution.Amount, from);
				if (contribution.IsFolded) continue;
				if (contribution.Amount < to) continue;

				eligible.Add(contribution.SeatIndex);
			}

			return new Pot { Amount = amount, EligibleSeats = eligible.ToArray() };
		}

		private static long CollectAbove(IReadOnlyList<PotContribution> contributions, long level)
		{
			long amount = 0;
			for (int i = 0; i < contributions.Count; i++)
			{
				PotContribution contribution = contributions[i];
				if (contribution.Amount <= level) continue;

				amount += contribution.Amount - level;
			}

			return amount;
		}

		private static void AddOrMerge(List<Pot> pots, Pot pot)
		{
			if (pots.Count > 0 && SameSeats(pots[pots.Count - 1].EligibleSeats, pot.EligibleSeats))
			{
				pots[pots.Count - 1].Amount += pot.Amount;
				return;
			}

			pots.Add(pot);
		}

		private static bool SameSeats(int[] left, int[] right)
		{
			if (left.Length != right.Length) return false;

			for (int i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i]) return false;
			}

			return true;
		}

		private static long Clamp(long amount, long ceiling) => amount < ceiling ? amount : ceiling;
	}
}