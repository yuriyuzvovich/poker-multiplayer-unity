using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Tests.EditMode.UI
{
	public sealed class ViewPoolTests
	{
		private GameObject _root;
		private Transform _storage;
		private ViewPool<Transform> _pool;

		[SetUp]
		public void SetUp()
		{
			_root = new GameObject("pool-test");
			_storage = new GameObject("storage").transform;
			_storage.SetParent(_root.transform, false);
			_pool = new ViewPool<Transform>(CreateView, _storage);
		}

		[TearDown]
		public void TearDown() => Object.DestroyImmediate(_root);

		[Test]
		public void Release_ThenGet_ReusesTheSameInstance()
		{
			Transform first = _pool.Get(_root.transform);
			_pool.Release(first);

			Transform second = _pool.Get(_root.transform);

			Assert.AreSame(first, second);
			Assert.AreEqual(1, _pool.CreatedCount);
			Assert.IsTrue(second.gameObject.activeSelf);
			Assert.AreSame(_root.transform, second.parent);
		}

		[Test]
		public void Release_HidesTheViewUnderStorage()
		{
			Transform view = _pool.Get(_root.transform);

			_pool.Release(view);

			Assert.IsFalse(view.gameObject.activeSelf);
			Assert.AreSame(_storage, view.parent);
			Assert.AreEqual(1, _pool.FreeCount);
		}

		[Test]
		public void ReleaseAll_ReturnsEveryView_AndEmptiesTheList()
		{
			List<Transform> views = new() { _pool.Get(_root.transform), _pool.Get(_root.transform) };

			_pool.ReleaseAll(views);
			_pool.Get(_root.transform);
			_pool.Get(_root.transform);

			Assert.AreEqual(0, views.Count);
			Assert.AreEqual(2, _pool.CreatedCount);
		}

		private static Transform CreateView(Transform parent)
		{
			Transform view = new GameObject("view").transform;
			view.SetParent(parent, false);
			return view;
		}
	}
}