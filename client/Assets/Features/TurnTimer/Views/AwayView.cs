using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.TurnTimer.Views
{
	public sealed class AwayView : MonoBehaviour, IAwayView
	{
		private Button _returnButton;

		public event Action ReturnClicked;

		public void Bind(Button returnButton)
		{
			_returnButton = returnButton;
			_returnButton.onClick.AddListener(OnReturnClicked);
		}

		public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);

		private void OnReturnClicked() => ReturnClicked?.Invoke();

		private void OnDestroy()
		{
			if (_returnButton == null) return;

			_returnButton.onClick.RemoveListener(OnReturnClicked);
		}
	}
}