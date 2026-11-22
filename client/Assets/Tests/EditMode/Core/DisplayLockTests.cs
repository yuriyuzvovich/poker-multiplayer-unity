using NUnit.Framework;
using Poker.Client.Infrastructure.Core.Presentation;

namespace Poker.Client.Tests.EditMode.Core
{
	public sealed class DisplayLockTests
	{
		[Test]
		public void Lock_IsReleasedOnlyWhenTheLastHolderLetsGo()
		{
			DisplayLock displayLock = new();
			int released = 0;
			displayLock.Released += () => released++;
			DisplayHold first = displayLock.Acquire();
			DisplayHold second = displayLock.Acquire();

			first.Dispose();
			Assert.IsTrue(displayLock.IsLocked);
			Assert.AreEqual(0, released);

			second.Dispose();
			Assert.IsFalse(displayLock.IsLocked);
			Assert.AreEqual(1, released);
		}

		[Test]
		public void DisposingAHoldTwice_ReleasesItOnce()
		{
			DisplayLock displayLock = new();
			DisplayHold hold = displayLock.Acquire();
			DisplayHold other = displayLock.Acquire();

			hold.Dispose();
			hold.Dispose();

			Assert.IsTrue(hold.IsReleased);
			Assert.IsTrue(displayLock.IsLocked, "the second dispose must not release the other holder");
			other.Dispose();
			Assert.IsFalse(displayLock.IsLocked);
		}
	}
}