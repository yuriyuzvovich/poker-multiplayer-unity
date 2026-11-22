using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Infrastructure.Core.Messaging;
using Poker.Client.Infrastructure.Network.Session;
using Poker.Client.Infrastructure.UI.Popups;
using VContainer.Unity;

namespace Poker.Client.App.Connection
{
	public sealed class ReplacedSessionFlow : IInitializable, IDisposable
	{
		private readonly IMessageBus _bus;
		private readonly PopupService _popups;
		private readonly AppReloader _reloader;
		private readonly CancellationTokenSource _lifetime = new();
		private IDisposable _subscription;

		public ReplacedSessionFlow(IMessageBus bus, PopupService popups, AppReloader reloader)
		{
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_popups = popups ?? throw new ArgumentNullException(nameof(popups));
			_reloader = reloader ?? throw new ArgumentNullException(nameof(reloader));
		}

		public void Initialize() => _subscription = _bus.Subscribe<SessionReplaced>(OnSessionReplaced);

		public void Dispose()
		{
			_subscription?.Dispose();
			_lifetime.Cancel();
			_lifetime.Dispose();
		}

		private void OnSessionReplaced(SessionReplaced replaced) => OfferPlayHereAsync(replaced.Reason, _lifetime.Token).Forget();

		private async UniTaskVoid OfferPlayHereAsync(string reason, CancellationToken cancellationToken)
		{
			NoticePopup popup = await _popups.LoadAsync<NoticePopup>(NoticePopup.Address, cancellationToken);
			popup.Setup("Signed in elsewhere", reason, "Play here");
			await _popups.ShowAsync(popup, PopupPriority.Critical, cancellationToken);
			_reloader.Reload("the player chose to play here after: " + reason);
		}
	}
}