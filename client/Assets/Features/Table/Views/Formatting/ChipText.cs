using System.Globalization;

namespace Poker.Client.Features.Table.Views.Formatting
{
	public static class ChipText
	{
		public static string Format(long chips)
		{
			if (chips < 10_000) return chips.ToString("N0", CultureInfo.InvariantCulture);
			if (chips < 1_000_000) return Short(chips, 1_000, "K");

			return Short(chips, 1_000_000, "M");
		}

		private static string Short(long chips, long unit, string suffix)
		{
			long tenths = chips * 10 / unit;
			string whole = (tenths / 10).ToString(CultureInfo.InvariantCulture);
			if (tenths % 10 == 0) return whole + suffix;

			return whole + "." + (tenths % 10).ToString(CultureInfo.InvariantCulture) + suffix;
		}
	}
}