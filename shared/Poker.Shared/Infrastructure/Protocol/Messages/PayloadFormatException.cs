using System;

namespace Poker.Shared.Infrastructure.Protocol.Messages
{
	public sealed class PayloadFormatException : Exception
	{
		public PayloadFormatException(string message, Exception innerException) : base(message, innerException) { }
	}
}