using System;

namespace Poker.Client.Infrastructure.Network.Requests
{
	public sealed class RequestRejectedException : Exception
	{
		public RequestRejectedException(string code, string message) : base(code + ": " + message) => Code = code;

		public string Code { get; }
	}
}