namespace Poker.Shared.Infrastructure.Protocol
{
	public static class ProtocolErrorCodes
	{
		public const string SessionRequired = "session.required";
		public const string UnsupportedVersion = "protocol.unsupported_version";
		public const string InvalidPlayerName = "session.invalid_name";
		public const string InvalidDeviceKey = "session.invalid_device_key";
	}
}