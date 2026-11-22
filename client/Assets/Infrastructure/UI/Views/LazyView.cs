using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Views
{
	public abstract class LazyView<TView> where TView : Component
	{
		private readonly ViewLoader _loader;
		private readonly string _address;
		private CancellationTokenSource _loading;

		protected LazyView(ViewLoader loader, string address)
		{
			_loader = loader ?? throw new ArgumentNullException(nameof(loader));
			_address = address ?? throw new ArgumentNullException(nameof(address));
		}

		public event Action<TView> Opened;
		public event Action<TView> Closing;

		public TView Current { get; private set; }

		public bool IsRequested => _loading != null || Current != null;

		protected void Open(Transform parent)
		{
			if (IsRequested) return;

			CancellationTokenSource loading = new();
			_loading = loading;
			LoadAsync(parent, loading).Forget();
		}

		protected void Close()
		{
			if (_loading != null)
			{
				_loading.Cancel();
				_loading.Dispose();
				_loading = null;
			}

			TView view = Current;
			if (view == null) return;

			Closing?.Invoke(view);
			OnDetaching(view);
			Current = null;
			_loader.Release(view);
		}

		protected abstract void OnAttached(TView view);

		protected virtual void OnDetaching(TView view) { }

		private async UniTaskVoid LoadAsync(Transform parent, CancellationTokenSource loading)
		{
			TView view;
			try
			{
				view = await _loader.LoadAsync<TView>(_address, parent, loading.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				if (_loading == loading) _loading = null;

				loading.Dispose();
				return;
			}

			_loading = null;
			loading.Dispose();
			Current = view;
			OnAttached(view);
			Opened?.Invoke(view);
		}
	}
}