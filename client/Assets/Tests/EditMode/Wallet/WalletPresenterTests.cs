using NUnit.Framework;
using Poker.Client.Features.Wallet.Domain;
using Poker.Client.Features.Wallet.Presentation;
using Poker.Client.Infrastructure.Core.Presentation;
using Poker.Client.Infrastructure.Network.Requests;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Tests.EditMode.Network;
using Poker.Shared.Features.Wallet;
using Poker.Shared.Features.Wallet.Events;
using Poker.Shared.Features.Wallet.Models;
using Poker.Shared.Features.Wallet.Requests;

namespace Poker.Client.Tests.EditMode.Wallet
{
	public sealed class WalletPresenterTests
	{
		private FakeServerConnection _connection;
		private PlayerSession _session;
		private DisplayLock _displayLock;
		private WalletModel _model;
		private FakeWalletView _view;
		private WalletPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_connection = new FakeServerConnection();
			_session = new PlayerSession();
			_displayLock = new DisplayLock();
			_model = new WalletModel();
			_view = new FakeWalletView();
			_presenter = new WalletPresenter(_connection, _session, _displayLock, _model, _view);
		}

		[TearDown]
		public void TearDown() => _presenter.Dispose();

		[Test]
		public void BeforeTheSession_TheBalanceIsUnknown()
		{
			_presenter.Initialize();

			Assert.AreEqual("Balance ...", _view.Balance);
			Assert.AreEqual(0, _connection.Requests.Count);
		}

		[Test]
		public void OnceSignedIn_TheBalanceIsLoaded_AndShownWithThousandsSeparators()
		{
			_connection.Responses[typeof(BalanceResponse)] = new BalanceResponse { Balance = 12500 };
			_presenter.Initialize();

			_session.Establish("session-1", "p-hero", "Hero");

			Assert.IsInstanceOf<GetBalanceRequest>(_connection.Requests[0]);
			Assert.AreEqual("Balance 12,500", _view.Balance);
		}

		[Test]
		public void BalanceChange_WaitsForTheDisplayLock_ThenShows()
		{
			_session.Establish("session-1", "p-hero", "Hero");
			_connection.Responses[typeof(BalanceResponse)] = new BalanceResponse { Balance = 5000 };
			_presenter.Initialize();
			DisplayHold hold = _displayLock.Acquire();

			_connection.Deliver(new BalanceChangedEvent { Balance = 5800, Delta = 800, Reason = BalanceChangeReason.CashOut });

			Assert.AreEqual(5800, _model.Balance);
			Assert.AreEqual("Balance 5,000", _view.Balance);
			hold.Dispose();
			Assert.AreEqual("Balance 5,800", _view.Balance);
		}

		[Test]
		public void RefusedFreeChips_ShowTheServerReason()
		{
			_presenter.Initialize();
			_connection.Failures[typeof(AddChipsRequest)] = new RequestRejectedException(WalletErrorCodes.NotEligible, "Free chips are only for balances under the limit.");

			_view.ClickFreeChips();

			Assert.AreEqual(WalletErrorCodes.NotEligible + ": Free chips are only for balances under the limit.", _view.Message);
		}

		[Test]
		public void AcceptedFreeChips_ClearTheMessage_AndTheEventCarriesTheBalance()
		{
			_presenter.Initialize();
			_view.SetMessage("old message");

			_view.ClickFreeChips();
			_connection.Deliver(new BalanceChangedEvent { Balance = 5000, Delta = 4200, Reason = BalanceChangeReason.FreeChips });

			Assert.AreEqual("", _view.Message);
			Assert.AreEqual("Balance 5,000", _view.Balance);
		}

		[Test]
		public void NewSession_ForgetsTheOldBalance_AndLoadsItAgain()
		{
			_connection.Responses[typeof(BalanceResponse)] = new BalanceResponse { Balance = 900 };
			_presenter.Initialize();
			_session.Establish("session-1", "p-hero", "Hero");
			_connection.Responses[typeof(BalanceResponse)] = new BalanceResponse { Balance = 5000 };

			_session.Establish("session-2", "p-hero", "Hero");

			Assert.AreEqual(2, _connection.Requests.Count);
			Assert.AreEqual("Balance 5,000", _view.Balance);
		}
	}
}