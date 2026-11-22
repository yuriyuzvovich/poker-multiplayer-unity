using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Server.Infrastructure.Session
{
	public sealed class SessionManager
	{
		private readonly object _lock = new object();
		private readonly Dictionary<string, ClientSession> _byResumeKey = new();
		private readonly Dictionary<string, PlayerIdentity> _byDeviceKey = new();
		private readonly MessageCodec _codec;
		private readonly MessageRegistry _registry;
		private readonly ConnectionOptions _options;

		public SessionManager(MessageCodec codec, MessageRegistry registry, ConnectionOptions options)
		{
			_codec = codec ?? throw new ArgumentNullException(nameof(codec));
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public PlayerIdentity IdentityFor(string deviceKey, string playerName)
		{
			if (deviceKey.Length == 0) return NewIdentity(playerName);

			lock (_lock)
			{
				if (_byDeviceKey.TryGetValue(deviceKey, out PlayerIdentity? known)) return known;

				PlayerIdentity player = NewIdentity(playerName);
				_byDeviceKey.Add(deviceKey, player);
				return player;
			}
		}

		public ClientSession Create(PlayerIdentity player)
		{
			ClientSession session = new(Guid.NewGuid().ToString("N"), player, NewResumeKey(), _codec, _registry, _options);
			lock (_lock) _byResumeKey.Add(session.ResumeKey, session);

			return session;
		}

		public bool TryTakeOver(string playerId, [NotNullWhen(true)] out ClientSession? replaced)
		{
			lock (_lock)
			{
				if (!TryFindByPlayerId(playerId, out replaced)) return false;

				_byResumeKey.Remove(replaced.ResumeKey);
				return true;
			}
		}

		public bool TryResume(string resumeKey, DateTimeOffset now, [NotNullWhen(true)] out ClientSession? session)
		{
			lock (_lock)
			{
				if (!_byResumeKey.TryGetValue(resumeKey, out session)) return false;
				if (IsExpired(session, now))
				{
					session = null;
					return false;
				}

				_byResumeKey.Remove(resumeKey);
				session.RotateResumeKey(NewResumeKey());
				_byResumeKey.Add(session.ResumeKey, session);
				return true;
			}
		}

		public bool TryFindByPlayerId(string playerId, [NotNullWhen(true)] out ClientSession? session)
		{
			lock (_lock)
			{
				foreach (ClientSession candidate in _byResumeKey.Values)
				{
					if (candidate.Player.PlayerId != playerId) continue;

					session = candidate;
					return true;
				}
			}

			session = null;
			return false;
		}

		public void End(ClientSession session)
		{
			lock (_lock) _byResumeKey.Remove(session.ResumeKey);
		}

		public void TakeExpired(DateTimeOffset now, List<ClientSession> expired)
		{
			lock (_lock)
			{
				foreach (ClientSession session in _byResumeKey.Values)
				{
					if (!IsExpired(session, now)) continue;

					expired.Add(session);
				}

				foreach (ClientSession session in expired)
				{
					_byResumeKey.Remove(session.ResumeKey);
				}
			}
		}

		private bool IsExpired(ClientSession session, DateTimeOffset now)
		{
			if (session.IsBroken) return true;
			if (session.DetachedAt == null) return false;

			return now >= session.DetachedAt.Value + _options.ZombieWindow;
		}

		private static PlayerIdentity NewIdentity(string playerName) => new("p-" + Guid.NewGuid().ToString("N").Substring(0, 12), playerName);

		private static string NewResumeKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
	}
}