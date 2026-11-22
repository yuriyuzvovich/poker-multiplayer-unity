using UnityEngine;
using UnityEngine.SceneManagement;

namespace Poker.Client.App
{
	public sealed class AppReloader
	{
		private bool _isReloading;

		public void Reload(string reason)
		{
			if (_isReloading) return;

			_isReloading = true;
			Debug.Log("[APP] Reloading: " + reason);
			SceneManager.LoadSceneAsync("Main");
		}
	}
}