using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Infrastructure.Hosting;

namespace Poker.Tests.Integration
{
	public static class TestServer
	{
		public static async Task<WebApplication> StartAsync(CancellationToken cancellationToken, params string[] settings)
		{
			List<string> args = new() { "--urls", "http://127.0.0.1:0" };
			args.AddRange(settings);
			WebApplication app = ServerHost.Build(args.ToArray());
			await app.StartAsync(cancellationToken);
			return app;
		}

		public static Uri SocketUri(WebApplication app)
		{
			IServerAddressesFeature addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
			foreach (string address in addresses.Addresses)
			{
				return new Uri(address.Replace("http://", "ws://") + "/ws");
			}

			throw new InvalidOperationException("The server has no bound address.");
		}
	}
}