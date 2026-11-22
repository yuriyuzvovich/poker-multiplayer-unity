using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;

namespace Poker.Client.Infrastructure.UI.Views
{
	public sealed class ViewLoader
	{
		public int LoadedCount { get; private set; }

		public async UniTask<T> LoadAsync<T>(string address, Transform parent, CancellationToken cancellationToken) where T : Component
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent, false);
			GameObject instance = await handle.ToUniTask();
			if (handle.Status != AsyncOperationStatus.Succeeded || instance == null) throw new InvalidOperationException("Could not load the view " + address + ".");

			LoadedCount++;
			if (cancellationToken.IsCancellationRequested)
			{
				Release(instance);
				throw new OperationCanceledException(cancellationToken);
			}

			if (!instance.TryGetComponent(out T view))
			{
				Release(instance);
				throw new InvalidOperationException("The view " + address + " has no " + typeof(T).Name + ".");
			}

			Debug.Log("[UI] Opened " + address + " in " + stopwatch.ElapsedMilliseconds + " ms");
			return view;
		}

		public void Release(Component view)
		{
			if (view == null) return;

			Release(view.gameObject);
		}

		private void Release(GameObject instance)
		{
			Debug.Log("[UI] Closed " + instance.name);
			if (!Addressables.ReleaseInstance(instance)) Debug.LogWarning("[UI] " + instance.name + " was not loaded by Addressables; destroying it.");

			LoadedCount--;
		}
	}
}