using System;
using UnityEngine;

namespace Poker.Client.Infrastructure.Core
{
	public sealed class AppStateMachine
	{
		public event Action<AppState, AppState> Changed;

		public AppState Current { get; private set; } = AppState.Booting;

		public string Detail { get; private set; } = "";

		public void TransitionTo(AppState next, string detail = "")
		{
			if (!IsAllowed(Current, next)) throw new InvalidOperationException("Illegal app transition " + Current + " -> " + next);

			AppState previous = Current;
			Current = next;
			Detail = detail;
			Debug.Log("[APP] " + previous + " -> " + next + (detail.Length > 0 ? " (" + detail + ")" : ""));
			Changed?.Invoke(previous, next);
		}

		public static bool IsAllowed(AppState from, AppState to) => (from, to) switch
		{
			(AppState.Booting, AppState.Connecting) => true,
			(AppState.Connecting, AppState.Connected) => true,
			(AppState.Connecting, AppState.Failed) => true,
			(AppState.Connected, AppState.Disconnected) => true,
			(AppState.Disconnected, AppState.Connecting) => true,
			(AppState.Failed, AppState.Connecting) => true,
			_ => false,
		};
	}
}