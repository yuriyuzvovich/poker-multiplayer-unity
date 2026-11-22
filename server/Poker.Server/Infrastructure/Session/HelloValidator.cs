using Poker.Shared.Infrastructure.Protocol;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;

namespace Poker.Server.Infrastructure.Session
{
	public static class HelloValidator
	{
		public static bool TryValidate(HelloRequest hello, out string name, out ErrorResponse error)
		{
			name = hello.PlayerName.Trim();
			error = new ErrorResponse();
			if (hello.ProtocolVersion != ProtocolInfo.Version)
			{
				error = new ErrorResponse { Code = ProtocolErrorCodes.UnsupportedVersion, Message = "Server speaks protocol " + ProtocolInfo.Version + "." };
				return false;
			}

			if (name.Length == 0 || name.Length > ProtocolInfo.MaxPlayerNameLength)
			{
				error = new ErrorResponse { Code = ProtocolErrorCodes.InvalidPlayerName, Message = "Name must be 1 to " + ProtocolInfo.MaxPlayerNameLength + " characters." };
				return false;
			}

			if (hello.DeviceKey.Length > ProtocolInfo.MaxDeviceKeyLength)
			{
				error = new ErrorResponse { Code = ProtocolErrorCodes.InvalidDeviceKey, Message = "Device key must be at most " + ProtocolInfo.MaxDeviceKeyLength + " characters." };
				return false;
			}

			return true;
		}
	}
}