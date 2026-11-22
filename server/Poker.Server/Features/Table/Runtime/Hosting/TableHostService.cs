using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Poker.Server.Features.Table.Runtime.Actor;

namespace Poker.Server.Features.Table.Runtime.Hosting
{
	public sealed class TableHostService : BackgroundService
	{
		private readonly TableDirectory _directory;
		private readonly ITableTickWork[] _ticks;

		public TableHostService(TableDirectory directory, IEnumerable<ITableTickWork> ticks)
		{
			_directory = directory ?? throw new ArgumentNullException(nameof(directory));
			_ticks = ticks?.ToArray() ?? throw new ArgumentNullException(nameof(ticks));
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			List<Task> loops = new();
			foreach (TableActor actor in _directory.Actors)
			{
				loops.Add(actor.RunAsync(stoppingToken));
			}

			try
			{
				using PeriodicTimer ticker = new(TimeSpan.FromMilliseconds(250));
				while (await ticker.WaitForNextTickAsync(stoppingToken))
				{
					PostTicks();
				}
			}
			catch (OperationCanceledException) { }

			try
			{
				await Task.WhenAll(loops);
			}
			catch (OperationCanceledException) { }
		}

		private void PostTicks()
		{
			foreach (TableActor actor in _directory.Actors)
			{
				foreach (ITableTickWork tick in _ticks)
				{
					actor.Post(tick);
				}
			}
		}
	}
}