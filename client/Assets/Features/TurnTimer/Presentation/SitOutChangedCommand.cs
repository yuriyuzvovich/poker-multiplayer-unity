using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Poker.Client.Features.Table.Domain;
using Poker.Client.Infrastructure.Core.Presentation;

namespace Poker.Client.Features.TurnTimer.Presentation
{
	public sealed class SitOutChangedCommand : IVisualCommand
	{
		private readonly TableModel _table;
		private readonly string _tableId;
		private readonly int _seatIndex;
		private readonly bool _isSittingOut;

		public SitOutChangedCommand(TableModel table, string tableId, int seatIndex, bool isSittingOut)
		{
			_table = table ?? throw new ArgumentNullException(nameof(table));
			_tableId = tableId;
			_seatIndex = seatIndex;
			_isSittingOut = isSittingOut;
		}

		public bool RunsOutOfOrder => true;

		public UniTask PlayAsync(CancellationToken cancellationToken)
		{
			Finish();
			return UniTask.CompletedTask;
		}

		public void Finish() => _table.SetSittingOut(_tableId, _seatIndex, _isSittingOut);
	}
}