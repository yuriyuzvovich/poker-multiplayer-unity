using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Poker.Client.Infrastructure.UI.Motion
{
	public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		private const float PressedScale = 0.94f;

		public void OnPointerDown(PointerEventData eventData) => ScaleTo(PressedScale, 0.08f, Ease.OutQuad);

		public void OnPointerUp(PointerEventData eventData) => ScaleTo(1f, 0.22f, Ease.OutBack);

		private void ScaleTo(float scale, float seconds, Ease ease)
		{
			transform.DOKill();
			transform.DOScale(scale, seconds).SetEase(ease).SetLink(gameObject, LinkBehaviour.KillOnDisable);
		}

		private void OnDisable() => transform.localScale = Vector3.one;
	}
}