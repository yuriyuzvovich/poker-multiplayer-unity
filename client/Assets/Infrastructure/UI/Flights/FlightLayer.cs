using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Infrastructure.UI.Flights
{
	public sealed class FlightLayer : MonoBehaviour
	{
		private const float ArcHeight = 70f;
		private const float FloatRise = 90f;

		[SerializeField]
		private Image _template;

		[SerializeField]
		private TMP_Text _labelTemplate;

		[SerializeField]
		private RectTransform _storage;

		private readonly List<Image> _flying = new();
		private readonly List<TMP_Text> _floating = new();
		private ViewPool<Image> _items;
		private ViewPool<TMP_Text> _labels;
		private Sequence _group;

		public async UniTask FlyAsync(IReadOnlyList<Flight> flights, CancellationToken cancellationToken)
		{
			Stop();
			if (flights.Count == 0) return;

			Sequence group = Launch(flights);
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
			await group.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken: cts.Token);
			if (this == null) return;
			if (_group != group) return;

			_group = null;
			ReleaseAll();
		}

		public void Stop()
		{
			DOTween.Kill(this);
			_group = null;
			ReleaseAll();
		}

		private Sequence Launch(IReadOnlyList<Flight> flights)
		{
			Sequence group = DOTween.Sequence(this);
			_group = group;
			float scale = transform.lossyScale.y;
			for (int i = 0; i < flights.Count; i++)
			{
				Flight flight = flights[i];
				if (flight.Motion == FlightMotion.Float)
				{
					AddLabel(group, flight, scale);
					continue;
				}

				AddItem(group, flight, scale);
			}

			return group;
		}

		private void AddItem(Sequence group, Flight flight, float scale)
		{
			Image item = _items.Get(transform);
			item.sprite = flight.Sprite;
			item.color = flight.Color;
			RectTransform rect = item.rectTransform;
			rect.sizeDelta = flight.Size;
			rect.position = flight.From;
			rect.localScale = Vector3.one;
			rect.localRotation = Quaternion.identity;
			_flying.Add(item);
			switch (flight.Motion)
			{
				case FlightMotion.Burst:
					group.Insert(flight.Delay, rect.DOMove(flight.To, flight.Seconds).SetEase(Ease.OutCubic));
					group.Insert(flight.Delay, rect.DOScale(0.3f, flight.Seconds).SetEase(Ease.InQuad));
					group.Insert(flight.Delay, item.DOFade(0f, flight.Seconds).SetEase(Ease.InQuad));
					break;
				case FlightMotion.Glow:
					rect.localScale = Vector3.one * 0.6f;
					group.Insert(flight.Delay, rect.DOScale(1.4f, flight.Seconds).SetEase(Ease.OutQuad));
					group.Insert(flight.Delay, item.DOFade(0f, flight.Seconds).SetEase(Ease.InQuad));
					break;
				default:
					if (flight.Spin != 0f)
					{
						rect.localRotation = Quaternion.Euler(0f, 0f, flight.Spin);
						group.Insert(flight.Delay, rect.DOLocalRotate(Vector3.zero, flight.Seconds).SetEase(Ease.OutQuad));
					}

					group.Insert(flight.Delay, rect.DOJump(flight.To, ArcHeight * scale, 1, flight.Seconds).SetEase(Ease.OutQuad));
					break;
			}
		}

		private void AddLabel(Sequence group, Flight flight, float scale)
		{
			TMP_Text label = _labels.Get(transform);
			label.text = flight.Text;
			label.color = flight.Color;
			RectTransform rect = label.rectTransform;
			rect.position = flight.From;
			_floating.Add(label);
			group.Insert(flight.Delay, rect.DOMove(flight.From + Vector3.up * (FloatRise * scale), flight.Seconds).SetEase(Ease.OutCubic));
			group.Insert(flight.Delay + flight.Seconds * 0.6f, DOTween.ToAlpha(() => label.color, value => label.color = value, 0f, flight.Seconds * 0.4f));
		}

		private void ReleaseAll()
		{
			_items.ReleaseAll(_flying);
			_labels.ReleaseAll(_floating);
		}

		private Image CreateItem(Transform parent) => Instantiate(_template, parent);

		private TMP_Text CreateLabel(Transform parent) => Instantiate(_labelTemplate, parent);

		private void Awake()
		{
			_items = new ViewPool<Image>(CreateItem, _storage);
			_labels = new ViewPool<TMP_Text>(CreateLabel, _storage);
		}

		private void OnDestroy() => DOTween.Kill(this);
	}
}