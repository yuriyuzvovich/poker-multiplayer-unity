using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Poker.Client.Features.Wallet.Views
{
	public sealed class WalletView : MonoBehaviour, IWalletView
	{
		private TMP_Text _balance;
		private TMP_Text _message;
		private Button _freeChips;

		public event Action FreeChipsClicked;

		public void Bind(TMP_Text balance, TMP_Text message, Button freeChips)
		{
			_balance = balance;
			_message = message;
			_freeChips = freeChips;
			_freeChips.onClick.AddListener(OnFreeChipsClicked);
		}

		public void SetBalance(string balance) => _balance.text = balance;

		public void SetMessage(string message) => _message.text = message;

		private void OnFreeChipsClicked() => FreeChipsClicked?.Invoke();

		private void OnDestroy()
		{
			if (_freeChips == null) return;

			_freeChips.onClick.RemoveListener(OnFreeChipsClicked);
		}
	}
}