using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Infrastructure.UI.Popups
{
	public sealed class PopupService
	{
		private readonly ViewLoader _loader;
		private readonly PopupQueue _queue = new();

		public PopupService(UiLayers layers, ViewLoader loader)
		{
			if (layers == null) throw new ArgumentNullException(nameof(layers));

			Layer = layers.Popups;
			_loader = loader ?? throw new ArgumentNullException(nameof(loader));
		}

		public Transform Layer { get; }

		public UniTask<TPopup> LoadAsync<TPopup>(string address, CancellationToken cancellationToken) where TPopup : Component =>
			_loader.LoadAsync<TPopup>(address, Layer, cancellationToken);

		public void Discard(Component popup) => _loader.Release(popup);

		public UniTask<TResult> ShowAsync<TResult>(Popup<TResult> popup, CancellationToken cancellationToken) =>
			ShowAsync(popup, PopupPriority.Normal, cancellationToken);

		public async UniTask<TResult> ShowAsync<TResult>(Popup<TResult> popup, PopupPriority priority, CancellationToken cancellationToken)
		{
			_queue.Open(popup.gameObject, priority);
			try
			{
				return await popup.Result.AttachExternalCancellation(cancellationToken);
			}
			finally
			{
				_queue.Close(popup.gameObject);
				_loader.Release(popup);
			}
		}
	}
}