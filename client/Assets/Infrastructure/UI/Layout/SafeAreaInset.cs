using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Layout
{
	public sealed class SafeAreaInset : MonoBehaviour
	{
		[SerializeField]
		private bool _isTop = true;

		[SerializeField]
		private bool _grows = true;

		private RectTransform _rect;
		private Vector2 _baseSize;
		private Vector2 _basePosition;
		private Rect _appliedArea;

		public static void Attach(RectTransform target, bool isTop, bool grows)
		{
			SafeAreaInset inset = target.gameObject.AddComponent<SafeAreaInset>();
			inset._isTop = isTop;
			inset._grows = grows;
			inset._appliedArea = default;
			inset.Apply();
		}

		private void Awake()
		{
			_rect = (RectTransform)transform;
			_baseSize = _rect.sizeDelta;
			_basePosition = _rect.anchoredPosition;
		}

		private void OnEnable() => Apply();

		private void OnRectTransformDimensionsChange() => Apply();

		private void Apply()
		{
			if (_rect == null) return;

			Rect safeArea = Screen.safeArea;
			if (safeArea == _appliedArea) return;

			Canvas canvas = GetComponentInParent<Canvas>();
			if (canvas == null) return;

			_appliedArea = safeArea;
			float pixels = _isTop ? SafeArea.TopInset(safeArea, new Vector2(Screen.width, Screen.height)) : SafeArea.BottomInset(safeArea);
			float inset = pixels / canvas.rootCanvas.scaleFactor;
			float growth = _grows ? inset : 0f;
			float shift = _grows ? 0f : inset;
			_rect.sizeDelta = _baseSize + new Vector2(0f, growth);
			_rect.anchoredPosition = _basePosition + new Vector2(0f, _isTop ? -shift : shift);
		}
	}
}