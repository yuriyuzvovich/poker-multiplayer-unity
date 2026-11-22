using System;
using UnityEngine;

namespace Poker.Client.Infrastructure.Network.Session
{
	public static class DeviceKey
	{
		public const string PrefsKey = "mp-poker.device-key";

		public static string LoadOrCreate()
		{
			string key = PlayerPrefs.GetString(PrefsKey, "");
			if (key.Length > 0) return key;

			key = Guid.NewGuid().ToString("N");
			PlayerPrefs.SetString(PrefsKey, key);
			PlayerPrefs.Save();
			return key;
		}
	}
}