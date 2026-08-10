using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.UI.Views;
using Xunit;

namespace AuthorTimeHunting.Tests;

/// <summary>
///     The split list is read at 60 km/h out of the corner of an eye, so the two things it must
///     never get wrong are the sign of a gap and the number of rows. A gap with the wrong sign
///     tells a player to reset a run that was ahead; a row appearing or disappearing moves every
///     row under it while they are reading one.
///     Everything here is stated in plain numbers, because that is what the table works in - the
///     colours are the overlay's, and the game is not involved at all.
/// </summary>
public class SplitsTableTests
{
	private const int _checkpoints = 3;

	/// <summary>The first run of a hunt on a level has nothing behind it, and must not pretend otherwise.</summary>
	[Fact]
	public void WithoutAReferenceTheTimesStandOnTheirOwn()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d, 9.25d), SplitSet.Empty, -1d, -1d, -1f);

		Assert.Equal("00:04.500", rows[0].Time);
		Assert.Equal("", rows[0].Gap);
		Assert.Equal(SplitPace.Unknown, rows[0].Pace);
	}

	[Fact]
	public void AFasterCheckpointIsAhead()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.613d), -1d, -1d, -1f);

		Assert.Equal("-00:00.113", rows[0].Gap);
		Assert.Equal(SplitPace.Ahead, rows[0].Pace);
	}

	[Fact]
	public void ASlowerCheckpointIsBehind()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.458d), -1d, -1d, -1f);

		Assert.Equal("+00:00.042", rows[0].Gap);
		Assert.Equal(SplitPace.Behind, rows[0].Pace);
	}

	/// <summary>
	///     The list is the level, not the run: every checkpoint has a row from the moment the lights
	///     go out, and the finish has one under them.
	/// </summary>
	[Fact]
	public void EveryCheckpointHasARowBeforeItIsDriven()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), SplitSet.Empty, -1d, -1d, -1f);

		Assert.Equal(_checkpoints + 1, rows.Count);
		Assert.Equal(SplitsTable.NoTime, rows[1].Time);
		Assert.Equal(SplitsTable.NoTime, rows[2].Time);
		Assert.Equal(SplitsTable.NoTime, rows[3].Time);
	}

	/// <summary>
	///     A level can be rebuilt with more checkpoints between two hunts, which leaves a reference
	///     shorter than the run being driven. It is one attempt's worth of missing gaps, not a crash.
	/// </summary>
	[Fact]
	public void AReferenceShorterThanTheRunSimplyRunsOut()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d, 9.25d, 14d), Best(4.4d), -1d, -1d, -1f);

		Assert.Equal(SplitPace.Behind, rows[0].Pace);
		Assert.Equal("", rows[1].Gap);
		Assert.Equal("00:09.250", rows[1].Time);
	}

	/// <summary>
	///     The finish is not a checkpoint. The game keeps no split for it, so its row is measured
	///     against the best time of the level instead.
	/// </summary>
	[Fact]
	public void TheFinishIsMeasuredAgainstTheBestTime()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.4d), -1d, 19.5d, 20f);

		SplitRow finish = rows[_checkpoints];

		Assert.Equal("00:19.500", finish.Time);
		Assert.Equal("-00:00.500", finish.Gap);
		Assert.Equal(SplitPace.Ahead, finish.Pace);
	}

	[Fact]
	public void AnUnfinishedRunLeavesTheFinishRowWaiting()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.4d), -1d, -1d, 20f);

		Assert.Equal(SplitsTable.NoTime, rows[_checkpoints].Time);
		Assert.Equal("", rows[_checkpoints].Gap);
	}

	/// <summary>Speed reads the other way round: more of it is the good news.</summary>
	[Fact]
	public void MoreSpeedThroughACheckpointIsAhead()
	{
		SplitSet run = new([4.5d], [72d]);
		SplitSet best = new([4.5d], [66d]);

		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, run, best, -1d, -1d, -1f);

		Assert.Equal("72", rows[0].Speed);
		Assert.Equal(SplitPace.Ahead, rows[0].SpeedPace);
	}

	[Fact]
	public void LessSpeedThroughACheckpointIsBehind()
	{
		SplitSet run = new([4.5d], [61d]);
		SplitSet best = new([4.5d], [66d]);

		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, run, best, -1d, -1d, -1f);

		Assert.Equal(SplitPace.Behind, rows[0].SpeedPace);
	}

	/// <summary>A level with no checkpoints at all is still a level, and still has a finish.</summary>
	[Fact]
	public void ALevelWithoutCheckpointsIsJustTheFinish()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(0, SplitSet.Empty, SplitSet.Empty, -1d, 12d, 13f);

		Assert.Single(rows);
		Assert.Equal("00:12.000", rows[0].Time);
	}

	/// <summary>
	///     The checkpoint being driven at counts up while it is being driven at. That is the row the
	///     player is actually reading - the ones above it are history and the ones below have not
	///     happened - so it carries the clock and the gap it is running up against.
	/// </summary>
	[Fact]
	public void TheCheckpointBeingDrivenAtRunsLive()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.4d, 9.1d), 8.9d, -1d, -1f);

		Assert.Equal("00:08.900", rows[1].Time);
		Assert.Equal("-00:00.200", rows[1].Gap);
		Assert.Equal(SplitPace.Ahead, rows[1].Pace);
	}

	/// <summary>Only the next one. A live clock on every empty row would be the same number three times.</summary>
	[Fact]
	public void TheCheckpointsAfterTheNextOneStayEmpty()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.4d, 9.1d), 8.9d, -1d, -1f);

		Assert.Equal(SplitsTable.NoTime, rows[2].Time);
	}

	/// <summary>
	///     And it stops counting the moment it is passed: the row keeps the time that was driven
	///     through it, not the clock that has gone on running.
	/// </summary>
	[Fact]
	public void APassedCheckpointKeepsItsOwnTime()
	{
		IReadOnlyList<SplitRow> rows = SplitsTable.Build(_checkpoints, Driven(4.5d), Best(4.4d), 12d, -1d, -1f);

		Assert.Equal("00:04.500", rows[0].Time);
	}

	private static SplitSet Driven(params double[] times)
	{
		double[] speeds = new double[times.Length];

		for (int i = 0; i < speeds.Length; i++)
		{
			speeds[i] = 60d + i;
		}

		return new SplitSet(times, speeds);
	}

	private static SplitSet Best(params double[] times)
	{
		return new SplitSet(times, new double[times.Length]);
	}
}
