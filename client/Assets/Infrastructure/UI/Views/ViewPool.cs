using System;
using System.Collections.Generic;
using UnityEngine;

namespace Poker.Client.Infrastructure.UI.Views
{
	public sealed class ViewPool<T> where T : Component
	{
		private readonly Func<Transform, T> _create;
		private readonly Transform _storage;
		private readonly Stack<T> _free = new();

		public ViewPool(Func<Transform, T> create, Transform storage)
		{
			_create = create ?? throw new ArgumentNullException(nameof(create));
			_storage = storage != null ? storage : throw new ArgumentNullException(nameof(storage));
		}

		public int CreatedCount { get; private set; }

		public int FreeCount => _free.Count;

		public T Get(Transform parent)
		{
			T view;
			if (_free.Count > 0)
			{
				view = _free.Pop();
			}
			else
			{
				view = _create(parent);
				CreatedCount++;
			}

			view.transform.SetParent(parent, false);
			view.transform.SetAsLastSibling();
			view.gameObject.SetActive(true);
			return view;
		}

		public void Release(T view)
		{
			view.gameObject.SetActive(false);
			view.transform.SetParent(_storage, false);
			_free.Push(view);
		}

		public void ReleaseAll(List<T> views)
		{
			foreach (T view in views)
			{
				Release(view);
			}

			views.Clear();
		}
	}
}