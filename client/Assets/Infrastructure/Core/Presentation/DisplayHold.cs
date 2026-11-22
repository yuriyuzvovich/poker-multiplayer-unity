using System;

namespace Poker.Client.Infrastructure.Core.Presentation
{
	public sealed class DisplayHold : IDisposable
	{
		private DisplayLock _owner;

		internal DisplayHold(DisplayLock owner) => _owner = owner;

		public bool IsReleased => _owner == null;

		public void Dispose()
		{
			DisplayLock owner = _owner;
			if (owner == null) return;

			_owner = null;
			owner.Release();
		}
	}
}