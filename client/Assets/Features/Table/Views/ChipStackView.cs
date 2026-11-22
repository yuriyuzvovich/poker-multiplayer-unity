using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.Table.Views
{
	public sealed class ChipStackView : MonoBehaviour
	{
		private static readonly long[] Denominations = { 1000, 500, 100, 25, 5, 1 };

		private static readonly Color[] BodyColors =
		{
			new Color(0.98f, 0.76f, 0.2f),
			new Color(0.6f, 0.28f, 0.82f),
			new Color(0.13f, 0.13f, 0.15f),
			new Color(0.16f, 0.62f, 0.32f),
			new Color(0.86f, 0.17f, 0.17f),
			new Color(0.96f, 0.96f, 0.96f),
		};

		private static readonly Color[] SpotColors =
		{
			new Color(0.45f, 0.24f, 0.03f),
			Color.white,
			Color.white,
			Color.white,
			Color.white,
			new Color(0.18f, 0.36f, 0.82f),
		};

		[SerializeField]
		private Image[] _chips = Array.Empty<Image>();

		[SerializeField]
		private Image[] _spots = Array.Empty<Image>();

		public void Show(long amount)
		{
			int used = 0;
			long rest = amount;
			for (int d = 0; d < Denominations.Length; d++)
			{
				long denomination = Denominations[d];
				long count = rest / denomination;
				rest -= count * denomination;
				for (long c = 0; c < count && used < _chips.Length; c++)
				{
					_chips[used].color = BodyColors[d];
					_spots[used].color = SpotColors[d];
					used++;
				}
			}

			for (int i = 0; i < _chips.Length; i++)
			{
				_chips[i].gameObject.SetActive(i < used);
			}
		}
	}
}