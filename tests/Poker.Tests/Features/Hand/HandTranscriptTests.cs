using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Poker.Server.Features.Hand.Domain;
using Poker.Server.Features.Hand.Runtime;
using Poker.Server.Features.Seating;
using Poker.Server.Features.Table.Domain;
using Poker.Server.Features.Table.Runtime.Actor;
using Poker.Server.Features.Table.Runtime.Work;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared;
using Poker.Shared.Features.Hand.Events;
using Poker.Shared.Features.Hand.Models;
using Poker.Shared.Features.Hand.Turns;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Tests.Architecture;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Hand
{
	public sealed class HandTranscriptTests
	{
		private const string FixturePath = "client/Assets/Tests/EditMode/Fixtures/heads-up-hand.json";
		private const string UpdateVariable = "POKER_UPDATE_FIXTURES";
		private const int HeroSeat = 1;
		private const int DeckSeed = 1;
		private const int BotSeed = 3;

		private static readonly PlayerIdentity Hero = new("p-hero", "Hero");

		[Fact]
		public async Task RecordedHand_CoversEveryMessageTheClientReplays()
		{
			List<SentMessage> messages = await RecordAsync();

			Assert.Contains(messages, m => m.Message is HoleCardsEvent cards && cards.SeatIndex == HeroSeat);
			Assert.Contains(messages, m => m.Message is TurnStartedEvent turn && turn.SeatIndex == HeroSeat);
			Assert.Contains(messages, m => m.Message is TurnActedEvent acted && acted.Action == PlayerAction.Raise);
			Assert.Contains(messages, m => m.Message is RoundStartedEvent round && round.Street == Street.River);
			Assert.Contains(messages, m => m.Message is ShowdownEvent);
			Assert.IsType<HandEndedEvent>(messages[^1].Message);
		}

		[Fact]
		public async Task RecordedHand_MatchesTheClientReplayFixture()
		{
			string transcript = Serialize(await RecordAsync());
			string path = Path.Combine(SourceTree.Root, FixturePath);
			if (Environment.GetEnvironmentVariable(UpdateVariable) == "1") await File.WriteAllTextAsync(path, transcript);

			Assert.True(File.Exists(path), "Missing " + FixturePath + "; run the tests once with " + UpdateVariable + "=1.");
			string fixture = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
			Assert.True(fixture == transcript, FixturePath + " is stale; rerun the tests with " + UpdateVariable + "=1 and commit the fixture.");
		}

		private static async Task<List<SentMessage>> RecordAsync(int deckSeed = DeckSeed, int botSeed = BotSeed)
		{
			ManualClock clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
			PokerTable table = new(new TableDefinition("heads-up", "Heads-up", 2, 50, 100, 2000, 10000, TimeSpan.FromSeconds(20)));
			table.SeatBot(0, new PlayerIdentity("bot-edsger", "Edsger"), 5000);
			TableActor actor = new(table, clock, NullLogger<TableActor>.Instance);
			HandEngine engine = new(new SeededDeckFactory(deckSeed), new HandOptions { BotSeed = botSeed });
			HandTickWork tick = new(engine);
			RecordingConnection hero = new(Hero);
			ushort requestId = 0;

			await actor.ExecuteAsync(new JoinTableWork(new MessageContext(hero, ++requestId)), CancellationToken.None);
			await actor.ExecuteAsync(new ReserveSeatWork(new MessageContext(hero, ++requestId), HeroSeat), CancellationToken.None);
			await actor.ExecuteAsync(new BuyInWork(new MessageContext(hero, ++requestId), 2000), CancellationToken.None);

			long actedTurnId = 0;
			for (int i = 0; i < 1000; i++)
			{
				if (hero.Sent[^1].Message is HandEndedEvent) return new List<SentMessage>(hero.Sent);

				TurnStartedEvent? turn = FindLastTurn(hero.Sent);
				if (turn != null && turn.SeatIndex == HeroSeat && turn.TurnId != actedTurnId)
				{
					actedTurnId = turn.TurnId;
					TurnActRequest act = new() { TableId = "heads-up", TurnId = turn.TurnId, Action = turn.CanCheck ? PlayerAction.Check : PlayerAction.Call };
					await actor.ExecuteAsync(new TurnActWork(new MessageContext(hero, ++requestId), act, engine), CancellationToken.None);
					continue;
				}

				clock.Advance(TimeSpan.FromMilliseconds(250));
				await actor.ExecuteAsync(tick, CancellationToken.None);
			}

			throw new InvalidOperationException("The recorded hand did not end.");
		}

		private static TurnStartedEvent? FindLastTurn(IReadOnlyList<SentMessage> messages)
		{
			for (int i = messages.Count - 1; i >= 0; i--)
			{
				if (messages[i].Message is not TurnStartedEvent turn) continue;

				return turn;
			}

			return null;
		}

		private static string Serialize(IReadOnlyList<SentMessage> messages)
		{
			MessageRegistry registry = MessageCatalog.CreateRegistry();
			JsonPayloadSerializer serializer = new();
			using MemoryStream stream = new();
			using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
			{
				writer.WriteStartObject();
				writer.WriteString("heroPlayerId", Hero.PlayerId);
				writer.WriteStartArray("messages");
				foreach (SentMessage sent in messages)
				{
					Type type = sent.Message.GetType();
					using JsonDocument payload = JsonDocument.Parse(serializer.Serialize(sent.Message, type));
					writer.WriteStartObject();
					writer.WriteString("type", registry.Get(type).Name);
					writer.WriteNumber("requestId", sent.RequestId);
					writer.WritePropertyName("payload");
					payload.WriteTo(writer);
					writer.WriteEndObject();
				}

				writer.WriteEndArray();
				writer.WriteEndObject();
			}

			return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n");
		}
	}
}