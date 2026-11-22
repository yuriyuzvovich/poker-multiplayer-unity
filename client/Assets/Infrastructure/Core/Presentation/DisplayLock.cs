using System;

namespace Poker.Client.Infrastructure.Core.Presentation
{
	public sealed class DisplayLock
	{
		private int _holders;

		public event Action Released;

		public bool IsLocked => _holders > 0;

		public DisplayHold Acquire()
		{
			_holders++;
			return new DisplayHold(this);
		}

		internal void Release()
		{
			_holders--;
			if (_holders > 0) return;

			Released?.Invoke();
		}
	}
}