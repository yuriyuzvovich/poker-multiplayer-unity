using DG.Tweening;
using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Motion
{
	public sealed class AppearMotion : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup _group;

		[SerializeField]
		private RectTransform _pop;

		[SerializeField]
		private float _seconds = 0.22f;

		private void OnEnable()
		{
			_group.DOKill();
			_group.alpha = 0f;
			_group.DOFade(1f, _seconds).SetEase(Ease.OutQuad).SetLink(gameObject, LinkBehaviour.KillOnDisable);
			if (_pop == null) return;

			_pop.DOKill();
			_pop.localScale = Vector3.one * 0.88f;
			_pop.DOScale(1f, _seconds * 1.4f).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDisable);
		}

		private void OnDisable() => _group.alpha = 1f;
	}
}