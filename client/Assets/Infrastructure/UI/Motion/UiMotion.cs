using System;
using DG.Tweening;
using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Motion
{
	public static class UiMotion
	{
		public static void Stop(Transform target)
		{
			target.DOKill();
			target.localScale = Vector3.one;
		}

		public static void Punch(Transform target, float strength = 0.15f, float seconds = 0.28f)
		{
			Stop(target);
			target.DOPunchScale(Vector3.one * strength, seconds, 6, 0.5f).SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
		}

		public static void SlideIn(RectTransform target, Vector2 rest, Vector2 offset, float seconds = 0.24f)
		{
			target.DOKill();
			target.anchoredPosition = rest + offset;
			target.DOAnchorPos(rest, seconds).SetEase(Ease.OutBack).SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
		}

		public static bool IsMoving(Transform target) => DOTween.IsTweening(target);

		public static void MoveTo(Transform target, Vector3 position, float seconds = 0.4f)
		{
			target.DOKill(true);
			target.DOMove(position, seconds).SetEase(Ease.InOutCubic).SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
		}

		public static void Flip(Transform target, Action atEdge, float seconds = 0.24f)
		{
			target.DOKill(true);
			target.localScale = Vector3.one;
			DOTween.Sequence(target)
				.Append(target.DOScaleX(0f, seconds / 2f).SetEase(Ease.InQuad))
				.AppendCallback(atEdge.Invoke)
				.Append(target.DOScaleX(1f, seconds / 2f).SetEase(Ease.OutQuad))
				.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
		}
	}
}