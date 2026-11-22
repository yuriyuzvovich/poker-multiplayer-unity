using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Poker.Server.Features.Hand;
using Poker.Server.Features.HandHistory;
using Poker.Server.Features.Lobby;
using Poker.Server.Features.Seating;
using Poker.Server.Features.Table;
using Poker.Server.Features.TurnTimer;
using Poker.Server.Features.Wallet;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Time;
using Poker.Shared;
using Poker.Shared.Infrastructure.Protocol.Messages;

namespace Poker.Server.Infrastructure.Hosting
{
	public static class ServerHost
	{
		public static WebApplication Build(string[] args)
		{
			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
			builder.Services.AddSingleton(MessageCatalog.CreateRegistry());
			builder.Services.AddSingleton<IPayloadSerializer, JsonPayloadSerializer>();
			builder.Services.AddSingleton<MessageCodec>();
			builder.Services.AddSingleton<MessageDispatcher>();
			builder.Services.AddSingleton<WebSocketEndpoint>();
			builder.Services.AddSingleton<IClock, SystemClock>();
			builder.Services.AddSingleton(builder.Configuration.GetSection("Connection").Get<ConnectionOptions>() ?? new ConnectionOptions());
			builder.Services.AddSingleton<SessionManager>();
			builder.Services.AddHostedService<SessionSweepService>();
			builder.Services.AddSingleton<IMessageHandler, PingHandler>();
			TableFeature.Install(builder.Services);
			LobbyFeature.Install(builder.Services);
			SeatingFeature.Install(builder.Services);
			HandFeature.Install(builder.Services);
			HandHistoryFeature.Install(builder.Services);
			TurnTimerFeature.Install(builder.Services);
			WalletFeature.Install(builder.Services);

			WebApplication app = builder.Build();
			app.UseWebSockets();
			app.Map("/ws", static (HttpContext context, WebSocketEndpoint endpoint) => endpoint.HandleAsync(context, context.RequestAborted));
			return app;
		}
	}
}