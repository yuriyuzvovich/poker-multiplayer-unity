using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Infrastructure.UI.Motion;

namespace Poker.Client.Features.Hand.Views
{
	public sealed class CardView : MonoBehaviour
	{
		private static readonly Color RedSuitColor = new(0.78f, 0.1f, 0.12f);
		private static readonly Color BlackSuitColor = new(0.12f, 0.12f, 0.14f);
		private static readonly Color DimColor = new(0.5f, 0.5f, 0.52f);

		[SerializeField]
		private Image _background;

		[SerializeField]
		private TMP_Text _rank;

		[SerializeField]
		private Image _suit;

		[SerializeField]
		private GameObject _highlight;

		[SerializeField]
		private Sprite _face;

		[SerializeField]
		private Sprite _back;

		[SerializeField]
		private Sprite _spade;

		[SerializeField]
		private Sprite _heart;

		[SerializeField]
		private Sprite _diamond;

		[SerializeField]
		private Sprite _club;

		private Action _applyCode;

		public string Code { get; private set; } = "";

		public void Show(string code)
		{
			UiMotion.Stop(transform);
			Code = code;
			ApplyCode();
		}

		public void Reveal(string code)
		{
			Code = code;
			ShowSide(false);
			_applyCode ??= ApplyCode;
			UiMotion.Flip(transform, _applyCode);
		}

		public void SetEmphasis(CardEmphasis emphasis)
		{
			bool isDimmed = emphasis == CardEmphasis.Dimmed;
			_highlight.SetActive(emphasis == CardEmphasis.Lifted);
			_background.color = isDimmed ? DimColor : Color.white;
			_suit.color = isDimmed ? DimColor : Color.white;
			_rank.alpha = isDimmed ? 0.5f : 1f;
			transform.localScale = emphasis == CardEmphasis.Lifted ? Vector3.one * 1.08f : Vector3.one;
		}

		private void ApplyCode()
		{
			bool isFaceUp = Code.Length >= 2;
			ShowSide(isFaceUp);
			if (!isFaceUp) return;

			char suit = Code[1];
			_rank.color = suit is 'h' or 'd' ? RedSuitColor : BlackSuitColor;
			_rank.text = Code[0] == 'T' ? "10" : Code[0].ToString();
			_suit.sprite = SuitSprite(suit);
		}

		private void ShowSide(bool isFaceUp)
		{
			_background.sprite = isFaceUp ? _face : _back;
			_rank.gameObject.SetActive(isFaceUp);
			_suit.gameObject.SetActive(isFaceUp);
		}

		private Sprite SuitSprite(char suit) => suit switch
		{
			'h' => _heart,
			'd' => _diamond,
			'c' => _club,
			_ => _spade,
		};
	}
}