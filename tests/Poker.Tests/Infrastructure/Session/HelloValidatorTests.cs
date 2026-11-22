using Poker.Server.Infrastructure.Session;
using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Xunit;

namespace Poker.Tests.Infrastructure.Session
{
	public sealed class HelloValidatorTests
	{
		[Fact]
		public void ValidHello_PassesWithTheTrimmedName()
		{
			bool isValid = HelloValidator.TryValidate(new HelloRequest { ProtocolVersion = ProtocolInfo.Version, PlayerName = "  Alice " }, out string name, out _);

			Assert.True(isValid);
			Assert.Equal("Alice", name);
		}

		[Fact]
		public void AnotherProtocolVersion_IsRejected()
		{
			bool isValid = HelloValidator.TryValidate(new HelloRequest { ProtocolVersion = ProtocolInfo.Version + 1, PlayerName = "Alice" }, out _, out ErrorResponse error);

			Assert.False(isValid);
			Assert.Equal(ProtocolErrorCodes.UnsupportedVersion, error.Code);
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("A name that is far too long for a seat")]
		public void InvalidName_IsRejected(string playerName)
		{
			bool isValid = HelloValidator.TryValidate(new HelloRequest { ProtocolVersion = ProtocolInfo.Version, PlayerName = playerName }, out _, out ErrorResponse error);

			Assert.False(isValid);
			Assert.Equal(ProtocolErrorCodes.InvalidPlayerName, error.Code);
		}
	}
}