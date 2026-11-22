using NUnit.Framework;
using Poker.Client.Features.Table.Domain;
using Poker.Shared.Features.Table.Models;
using Poker.Shared.Features.Table.Requests;

namespace Poker.Client.Tests.EditMode.Table
{
	public sealed class TableModelTests
	{
		private const string TableId = "practice";

		private TableModel _table;
		private int _changes;

		[SetUp]
		public void SetUp()
		{
			_table = new TableModel();
			_table.Load(new TableSnapshot
			{
				TableId = TableId,
				Name = "Practice",
				MinBuyIn = 400,
				MaxBuyIn = 4000,
				Seats = new[]
				{
					new SeatSnapshot { Index = 0, Status = SeatStatus.Seated, PlayerId = "bot-ada", PlayerName = "Ada", Stack = 5000 },
					new SeatSnapshot { Index = 1, Status = SeatStatus.Empty },
					new SeatSnapshot { Index = 2, Status = SeatStatus.Seated, PlayerId = "p-hero", PlayerName = "Hero", Stack = 2000 },
				},
			});
			_changes = 0;
			_table.Changed += () => _changes++;
		}

		[Test]
		public void Load_TakesTheSnapshot()
		{
			Assert.IsTrue(_table.IsJoined);
			Assert.AreEqual("Practice", _table.Name);
			Assert.AreEqual(400, _table.MinBuyIn);
			Assert.AreEqual(3, _table.Seats.Count);
			Assert.AreEqual("Hero", _table.NameAt(2));
		}

		[Test]
		public void EventsForAnotherTable_AreIgnored()
		{
			_table.ApplySeat("heads-up", new SeatSnapshot { Index = 1, Status = SeatStatus.Seated, PlayerId = "p-x", PlayerName = "X" });
			_table.ApplyStack("heads-up", 2, "p-hero", 1);
			_table.ReleaseSeat("heads-up", 0);

			Assert.AreEqual(0, _changes);
			Assert.AreEqual(SeatStatus.Empty, _table.Seats[1].Status);
			Assert.AreEqual(2000, _table.Seats[2].Stack);
		}

		[Test]
		public void ApplyStack_ChangesOnlyTheSeatedPlayersStack_AndOnlyWhenItDiffers()
		{
			_table.ApplyStack(TableId, 2, "p-hero", 1800);
			_table.ApplyStack(TableId, 2, "p-hero", 1800);
			_table.ApplyStack(TableId, 2, "p-someone-else", 10);
			_table.ApplyStack(TableId, 1, "p-hero", 10);

			Assert.AreEqual(1, _changes);
			Assert.AreEqual(1800, _table.Seats[2].Stack);
		}

		[Test]
		public void ApplySeat_ReplacesTheSeat()
		{
			_table.ApplySeat(TableId, new SeatSnapshot { Index = 1, Status = SeatStatus.Reserved, PlayerId = "p-bob", PlayerName = "Bob" });

			Assert.AreEqual(1, _changes);
			Assert.AreEqual(SeatStatus.Reserved, _table.Seats[1].Status);
			Assert.AreEqual(1, _table.FindSeatOf("p-bob"));
		}

		[Test]
		public void SetSittingOut_FlagsOnlyASeatedPlayer_AndOnlyOnChange()
		{
			_table.SetSittingOut(TableId, 2, true);
			_table.SetSittingOut(TableId, 2, true);
			_table.SetSittingOut(TableId, 1, true);

			Assert.AreEqual(1, _changes);
			Assert.IsTrue(_table.Seats[2].IsSittingOut);
			Assert.IsFalse(_table.Seats[1].IsSittingOut);
		}

		[Test]
		public void ReleaseSeat_EmptiesIt_AndTheOldPlayerIsNoLongerFound()
		{
			_table.ReleaseSeat(TableId, 2);

			Assert.AreEqual(SeatStatus.Empty, _table.Seats[2].Status);
			Assert.AreEqual(-1, _table.FindSeatOf("p-hero"));
			Assert.AreEqual(0, _table.FindSeatOf("bot-ada"));
		}

		[Test]
		public void OutOfRangeSeats_AreIgnored()
		{
			_table.ReleaseSeat(TableId, 7);
			_table.ApplyStack(TableId, -1, "p-hero", 1);

			Assert.AreEqual(0, _changes);
			Assert.AreEqual("", _table.NameAt(9));
		}

		[Test]
		public void Clear_LeavesTheTable()
		{
			_table.Clear();

			Assert.IsFalse(_table.IsJoined);
			Assert.AreEqual(0, _table.Seats.Count);
			Assert.AreEqual(1, _changes);
		}
	}
}