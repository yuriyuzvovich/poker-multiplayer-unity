namespace Poker.Client.Features.Hand.Presentation
{
	public sealed class HandTimings
	{
		public float HandStartedSeconds { get; set; } = 0.5f;
		public float HoleCardsSeconds { get; set; } = 0.4f;
		public float PlayerActedSeconds { get; set; } = 0.6f;
		public float RoundEndedSeconds { get; set; } = 0.3f;
		public float BoardCardSeconds { get; set; } = 0.3f;
		public float ShowdownSeconds { get; set; } = 1.2f;
		public float HandEndedSeconds { get; set; } = 1.5f;
		public float ChipFlightSeconds { get; set; } = 0.4f;
		public float CardFlightSeconds { get; set; } = 0.25f;
		public float CelebrationSeconds { get; set; } = 0.9f;

		public static HandTimings Instant() => new HandTimings
		{
			HandStartedSeconds = 0f,
			HoleCardsSeconds = 0f,
			PlayerActedSeconds = 0f,
			RoundEndedSeconds = 0f,
			BoardCardSeconds = 0f,
			ShowdownSeconds = 0f,
			HandEndedSeconds = 0f,
			ChipFlightSeconds = 0f,
			CardFlightSeconds = 0f,
			CelebrationSeconds = 0f,
		};

		public static HandTimings Fast() => new HandTimings
		{
			HandStartedSeconds = 0.2f,
			HoleCardsSeconds = 0.15f,
			PlayerActedSeconds = 0.25f,
			RoundEndedSeconds = 0.12f,
			BoardCardSeconds = 0.12f,
			ShowdownSeconds = 0.5f,
			HandEndedSeconds = 0.6f,
			ChipFlightSeconds = 0.2f,
			CardFlightSeconds = 0.12f,
			CelebrationSeconds = 0.5f,
		};

		public void CopyFrom(HandTimings source)
		{
			HandStartedSeconds = source.HandStartedSeconds;
			HoleCardsSeconds = source.HoleCardsSeconds;
			PlayerActedSeconds = source.PlayerActedSeconds;
			RoundEndedSeconds = source.RoundEndedSeconds;
			BoardCardSeconds = source.BoardCardSeconds;
			ShowdownSeconds = source.ShowdownSeconds;
			HandEndedSeconds = source.HandEndedSeconds;
			ChipFlightSeconds = source.ChipFlightSeconds;
			CardFlightSeconds = source.CardFlightSeconds;
			CelebrationSeconds = source.CelebrationSeconds;
		}
	}
}