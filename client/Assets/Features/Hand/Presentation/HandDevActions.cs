using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Hand.Domain;
using Poker.Client.Features.Hand.Views;
using Poker.Client.Features.Hand.Views.Data;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Infrastructure.Diagnostics.DevPanel;
using Poker.Shared.Features.Table.Models;
using UnityEngine;
using VContainer.Unity;

namespace Poker.Client.Features.Hand.Presentation
{
	public sealed class HandDevActions : IInitializable, IDisposable
	{
		private readonly DevPanelActions _actions;
		private readonly IHandView _view;
		private readonly TableModel _table;
		private readonly HandModel _hand;
		private readonly HandTimings _timings;
		private readonly List<int> _seats = new();
		private readonly List<SeatWin> _wins = new();
		private readonly CancellationTokenSource _lifetime = new();
		private bool _isFast;

		public HandDevActions(DevPanelActions actions, IHandView view, TableModel table, HandModel hand, HandTimings timings)
		{
			_actions = actions ?? throw new ArgumentNullException(nameof(actions));
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_hand = hand ?? throw new ArgumentNullException(nameof(hand));
			_timings = timings ?? throw new ArgumentNullException(nameof(timings));
		}

		public void Initialize()
		{
			_actions.Add("FX deal", PlayDeal);
			_actions.Add("FX bets", PlayBets);
			_actions.Add("FX to pot", PlayToPot);
			_actions.Add("FX win", PlayWin);
			_actions.Add("Fast anims", ToggleFastAnimations);
		}

		public void Dispose()
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
		}

		private void PlayDeal()
		{
			if (!CollectSeats()) return;

			_view.DealCardsAsync(_seats, _timings.CardFlightSeconds, _lifetime.Token).Forget();
		}

		private void PlayBets()
		{
			if (!CollectSeats()) return;

			_view.FlyChipsAsync(ChipRoute.AvatarToBet, _seats, _timings.ChipFlightSeconds, _lifetime.Token).Forget();
		}

		private void PlayToPot()
		{
			if (!CollectSeats()) return;

			_view.FlyChipsAsync(ChipRoute.BetToPot, _seats, _timings.ChipFlightSeconds, _lifetime.Token).Forget();
		}

		private void PlayWin()
		{
			if (!CollectSeats()) return;

			_wins.Clear();
			_wins.Add(new SeatWin(_hand.MySeat >= 0 ? _hand.MySeat : _seats[0], 1250));
			_view.CelebrateAsync(_wins, _timings.CelebrationSeconds, _lifetime.Token).Forget();
		}

		private void ToggleFastAnimations()
		{
			_isFast = !_isFast;
			_timings.CopyFrom(_isFast ? HandTimings.Fast() : new HandTimings());
			Debug.Log("[DEV] Hand animations " + (_isFast ? "fast" : "normal"));
		}

		private bool CollectSeats()
		{
			_seats.Clear();
			IReadOnlyList<SeatSnapshot> seats = _table.Seats;
			for (int i = 0; i < seats.Count; i++)
			{
				SeatSnapshot seat = seats[i];
				if (seat.Status != SeatStatus.Seated) continue;

				_seats.Add(seat.Index);
			}

			return _seats.Count > 0;
		}
	}
}