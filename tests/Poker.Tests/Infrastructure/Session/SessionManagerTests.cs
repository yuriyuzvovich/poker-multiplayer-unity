using System;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Xunit;

namespace Poker.Tests.Infrastructure.Session
{
	public sealed class SessionManagerTests
	{
		private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

		private readonly SessionManager _sessions;

		public SessionManagerTests()
		{
			MessageRegistry registry = MessageCatalog.CreateRegistry();
			_sessions = new SessionManager(new MessageCodec(registry, new JsonPayloadSerializer()), registry, new ConnectionOptions());
		}

		[Fact]
		public void Resume_ReturnsTheSameSession_UnderANewKey()
		{
			ClientSession created = _sessions.Create(_sessions.IdentityFor("", "Alice"));
			string firstKey = created.ResumeKey;

			Assert.True(_sessions.TryResume(firstKey, Now, out ClientSession? resumed));
			Assert.Same(created, resumed);
			Assert.NotEqual(firstKey, resumed.ResumeKey);
		}

		[Fact]
		public void ResumeKey_IsSingleUse()
		{
			string firstKey = _sessions.Create(_sessions.IdentityFor("", "Alice")).ResumeKey;
			_sessions.TryResume(firstKey, Now, out _);

			Assert.False(_sessions.TryResume(firstKey, Now, out _));
		}

		[Fact]
		public void SameDeviceKey_IsTheSamePlayer_AndNoKeyIsAlwaysANewOne()
		{
			PlayerIdentity first = _sessions.IdentityFor("device-1", "Alice");

			Assert.Same(first, _sessions.IdentityFor("device-1", "Alice"));
			Assert.NotEqual(first.PlayerId, _sessions.IdentityFor("device-2", "Alice").PlayerId);
			Assert.NotEqual(_sessions.IdentityFor("", "Alice").PlayerId, _sessions.IdentityFor("", "Alice").PlayerId);
		}

		[Fact]
		public void TakeOver_RemovesTheOldSessionOfThatPlayer()
		{
			PlayerIdentity alice = _sessions.IdentityFor("device-1", "Alice");
			ClientSession old = _sessions.Create(alice);
			ClientSession other = _sessions.Create(_sessions.IdentityFor("device-2", "Bob"));

			Assert.True(_sessions.TryTakeOver(alice.PlayerId, out ClientSession? replaced));
			Assert.Same(old, replaced);
			Assert.False(_sessions.TryResume(old.ResumeKey, Now, out _), "a replaced session cannot be resumed");
			Assert.False(_sessions.TryTakeOver(alice.PlayerId, out _));
			Assert.True(_sessions.TryResume(other.ResumeKey, Now, out _), "other players keep their sessions");
		}

		[Fact]
		public void EndedSession_CannotBeResumed()
		{
			ClientSession created = _sessions.Create(_sessions.IdentityFor("", "Alice"));

			_sessions.End(created);

			Assert.False(_sessions.TryResume(created.ResumeKey, Now, out _));
		}
	}
}