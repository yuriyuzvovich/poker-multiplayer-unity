using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Poker.Server.Features.Wallet;
using Poker.Server.Infrastructure.Messaging;
using Poker.Server.Infrastructure.Session;
using Poker.Server.Infrastructure.Transport;
using Poker.Shared;
using Poker.Shared.Features.Wallet;
using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Features.Wallet.Models;
using Poker.Shared.Features.Wallet.Requests;
using Poker.Shared.Infrastructure.Protocol.Messages;
using Poker.Shared.Infrastructure.Protocol.SystemMessages;
using Poker.Tests.TestDoubles;
using Xunit;

namespace Poker.Tests.Features.Wallet
{
	public sealed class WalletBookTests
	{
		private const string PlayerId = "p-1";

		private readonly MessageRegistry _registry = MessageCatalog.CreateRegistry();
		private readonly MessageCodec _codec;
		private readonly SessionManager _sessions;
		private readonly WalletBook _wallets;

		public WalletBookTests()
		{
			_codec = new MessageCodec(_registry, new JsonPayloadSerializer());
			_sessions = new SessionManager(_codec, _registry, new ConnectionOptions());
			_wallets = new WalletBook(new WalletOptions { StartingBalance = 5000, FreeChipsBelow = 1000, FreeChipsUpTo = 5000 }, _sessions);
		}

		[Fact]
		public void NewPlayer_StartsWithTheStartingBalance()
		{
			long balance = _wallets.GetBalance(PlayerId);

			Assert.Equal(5000, balance);
		}

		[Fact]
		public void Withdraw_MoreThanTheBalance_IsRefused_AndChangesNothing()
		{
			bool withdrawn = _wallets.TryWithdraw(PlayerId, 5001);

			Assert.False(withdrawn);
			Assert.Equal(5000, _wallets.GetBalance(PlayerId));
		}

		[Fact]
		public void WithdrawThenDeposit_MovesExactlyTheAmounts()
		{
			Assert.True(_wallets.TryWithdraw(PlayerId, 400));
			Assert.Equal(4600, _wallets.GetBalance(PlayerId));

			_wallets.Deposit(PlayerId, 650);

			Assert.Equal(5250, _wallets.GetBalance(PlayerId));
		}

		[Theory]
		[InlineData(999, true, 5000)]
		[InlineData(0, true, 5000)]
		[InlineData(1000, false, 1000)]
		[InlineData(4000, false, 4000)]
		public void FreeChips_TopTheBalanceUp_OnlyUnderTheLimit(long balanceBefore, bool expectedClaim, long expectedBalance)
		{
			Assert.True(_wallets.TryWithdraw(PlayerId, 5000 - balanceBefore));

			bool claimed = _wallets.TryClaimFreeChips(PlayerId, out long balance);

			Assert.Equal(expectedClaim, claimed);
			Assert.Equal(expectedBalance, _wallets.GetBalance(PlayerId));
			if (claimed) Assert.Equal(expectedBalance, balance);
		}

		[Fact]
		public async Task EveryChange_IsPushedToTheOwner_WithItsDeltaAndReason()
		{
			ClientSession session = _sessions.Create(new PlayerIdentity(PlayerId, "Alice"));
			StalledWebSocket socket = new() { IsStalled = false };
			session.Attach(socket, new WelcomeResponse(), 1, 0);

			_wallets.TryWithdraw(PlayerId, 400);
			_wallets.Deposit(PlayerId, 100);
			_wallets.TryWithdraw(PlayerId, 4000);
			_wallets.TryClaimFreeChips(PlayerId, out _);
			await session.CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

			List<BalanceChangedEvent> changes = DecodeBalanceChanges(socket);
			Assert.Equal(4, changes.Count);
			AssertChange(changes[0], 4600, -400, BalanceChangeReason.BuyIn);
			AssertChange(changes[1], 4700, 100, BalanceChangeReason.CashOut);
			AssertChange(changes[2], 700, -4000, BalanceChangeReason.BuyIn);
			AssertChange(changes[3], 5000, 4300, BalanceChangeReason.FreeChips);
		}

		[Fact]
		public void RefusedWithdraw_PushesNothing()
		{
			ClientSession session = _sessions.Create(new PlayerIdentity(PlayerId, "Alice"));
			StalledWebSocket socket = new() { IsStalled = false };
			session.Attach(socket, new WelcomeResponse(), 1, 0);
			int sentBefore = socket.Sent.Count;

			_wallets.TryWithdraw(PlayerId, 9999);

			Assert.Equal(sentBefore, socket.Sent.Count);
		}

		[Fact]
		public async Task AddChips_UnderTheLimit_RespondsWithTheNewBalance()
		{
			_wallets.TryWithdraw(PlayerId, 4500);
			RecordingConnection connection = new(new PlayerIdentity(PlayerId, "Alice"));
			IMessageHandler handler = new AddChipsHandler(_wallets);

			await handler.HandleAsync(new MessageContext(connection, 7), new AddChipsRequest(), CancellationToken.None);

			SentMessage sent = Assert.Single(connection.Sent);
			Assert.Equal(7, sent.RequestId);
			Assert.Equal(5000, Assert.IsType<BalanceResponse>(sent.Message).Balance);
		}

		[Fact]
		public async Task AddChips_AtOrAboveTheLimit_IsRejectedAsNotEligible()
		{
			RecordingConnection connection = new(new PlayerIdentity(PlayerId, "Alice"));
			IMessageHandler handler = new AddChipsHandler(_wallets);

			await handler.HandleAsync(new MessageContext(connection, 8), new AddChipsRequest(), CancellationToken.None);

			ErrorResponse error = Assert.IsType<ErrorResponse>(Assert.Single(connection.Sent).Message);
			Assert.Equal(WalletErrorCodes.NotEligible, error.Code);
			Assert.Equal(5000, _wallets.GetBalance(PlayerId));
		}

		[Fact]
		public async Task GetBalance_RespondsWithTheCurrentBalance()
		{
			_wallets.TryWithdraw(PlayerId, 1234);
			RecordingConnection connection = new(new PlayerIdentity(PlayerId, "Alice"));
			IMessageHandler handler = new GetBalanceHandler(_wallets);

			await handler.HandleAsync(new MessageContext(connection, 9), new GetBalanceRequest(), CancellationToken.None);

			Assert.Equal(3766, Assert.IsType<BalanceResponse>(Assert.Single(connection.Sent).Message).Balance);
		}

		private List<BalanceChangedEvent> DecodeBalanceChanges(StalledWebSocket socket)
		{
			List<BalanceChangedEvent> changes = new();
			foreach (byte[] packet in socket.Sent)
			{
				if (_codec.Decode(new ArraySegment<byte>(packet), out IncomingMessage message) != DecodeStatus.Ok) continue;
				if (message.Payload is BalanceChangedEvent change) changes.Add(change);
			}

			return changes;
		}

		private static void AssertChange(BalanceChangedEvent change, long balance, long delta, BalanceChangeReason reason)
		{
			Assert.Equal(balance, change.Balance);
			Assert.Equal(delta, change.Delta);
			Assert.Equal(reason, change.Reason);
		}
	}
}