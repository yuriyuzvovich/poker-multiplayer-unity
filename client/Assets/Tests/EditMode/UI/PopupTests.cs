using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Poker.Client.Infrastructure.UI.Popups;

namespace Poker.Client.Tests.EditMode.UI
{
	public sealed class PopupTests
	{
		[UnityTest]
		public IEnumerator Close_CompletesTheResult_OnlyOnce() => UniTask.ToCoroutine(async () =>
		{
			GameObject host = new("popup");
			try
			{
				TestPopup popup = host.AddComponent<TestPopup>();

				popup.Close(7);
				popup.Close(9);

				Assert.IsTrue(popup.IsClosed);
				Assert.AreEqual(7, await popup.Result);
			}
			finally
			{
				Object.DestroyImmediate(host);
			}
		});

		private sealed class TestPopup : Popup<int> { }
	}
}