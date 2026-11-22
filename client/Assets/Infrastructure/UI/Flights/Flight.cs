using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Flights
{
	public readonly struct Flight
	{
		private Flight(FlightMotion motion, Sprite sprite, string text, Vector2 size, Vector3 from, Vector3 to, float delay, float seconds, Color color, float spin)
		{
			Motion = motion;
			Sprite = sprite;
			Text = text;
			Size = size;
			From = from;
			To = to;
			Delay = delay;
			Seconds = seconds;
			Color = color;
			Spin = spin;
		}

		public FlightMotion Motion { get; }
		public Sprite Sprite { get; }
		public string Text { get; }
		public Vector2 Size { get; }
		public Vector3 From { get; }
		public Vector3 To { get; }
		public float Delay { get; }
		public float Seconds { get; }
		public Color Color { get; }
		public float Spin { get; }

		public static Flight Arc(Sprite sprite, Vector2 size, Vector3 from, Vector3 to, float delay, float seconds, float spin = 0f) =>
			new Flight(FlightMotion.Arc, sprite, "", size, from, to, delay, seconds, Color.white, spin);

		public static Flight Burst(Sprite sprite, Vector2 size, Vector3 from, Vector3 to, float delay, float seconds, Color color) =>
			new Flight(FlightMotion.Burst, sprite, "", size, from, to, delay, seconds, color, 0f);

		public static Flight Glow(Sprite sprite, Vector2 size, Vector3 at, float delay, float seconds, Color color) =>
			new Flight(FlightMotion.Glow, sprite, "", size, at, at, delay, seconds, color, 0f);

		public static Flight Float(string text, Vector3 at, float delay, float seconds, Color color) =>
			new Flight(FlightMotion.Float, null, text, Vector2.zero, at, at, delay, seconds, color, 0f);
	}
}