using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     The split list as text: one row per checkpoint plus the finish, always full length.
///     Rows are filled in as they are driven rather than appended, so nothing shifts while read.
/// </summary>
public static class SplitsTable
{
	public const string NoTime = "--:--.---";

	private const string _finishLabel = "F";

	public static IReadOnlyList<SplitRow> Build(int checkpoints, SplitSet driven, SplitSet reference, double running,
		double finishTime, double bestTime)
	{
		List<SplitRow> rows = [];

		for (int i = 0; i < checkpoints; i++)
		{
			rows.Add(Checkpoint(i, driven, reference, running));
		}

		rows.Add(Finish(finishTime, bestTime));

		return rows;
	}

	/// <summary>
	///     A driven checkpoint freezes its row; the one being driven at counts up against its
	///     own reference.
	/// </summary>
	private static SplitRow Checkpoint(int index, SplitSet run, SplitSet best, double running)
	{
		double time = run.TimeAt(index);

		if (time >= 0d)
		{
			return Row(index, time, run.SpeedAt(index), best);
		}

		if (index != run.Count || running < 0d)
		{
			return Waiting(UiNumbers.Text(index + 1));
		}

		return Row(index, running, -1d, best);
	}

	private static SplitRow Row(int index, double time, double speed, SplitSet best)
	{
		return new SplitRow(UiNumbers.Text(index + 1), TimeFormatter.FormatTime(time), Gap(time, best.TimeAt(index)),
			Pace(time, best.TimeAt(index)), Speed(speed), SpeedPace(speed, best.SpeedAt(index)));
	}

	/// <summary>
	///     The run as a whole, measured against the best time rather than against a split - the
	///     finish line is not a checkpoint and the game keeps no split for it.
	/// </summary>
	private static SplitRow Finish(double finishTime, double bestTime)
	{
		if (finishTime < 0d)
		{
			return Waiting(_finishLabel);
		}

		return new SplitRow(_finishLabel, TimeFormatter.FormatTime(finishTime), Gap(finishTime, bestTime),
			Pace(finishTime, bestTime), "", SplitPace.Unknown);
	}

	private static SplitRow Waiting(string label)
	{
		return new SplitRow(label, NoTime, "", SplitPace.Unknown, "", SplitPace.Unknown);
	}

	private static string Gap(double time, double reference)
	{
		return reference <= 0d ? "" : TimeFormatter.FormatDelta(time - reference);
	}

	/// <summary>A time: less of it is better.</summary>
	private static SplitPace Pace(double time, double reference)
	{
		if (reference <= 0d)
		{
			return SplitPace.Unknown;
		}

		return time <= reference ? SplitPace.Ahead : SplitPace.Behind;
	}

	/// <summary>A speed: more of it is better. The same shape as <see cref="Pace" /> and the opposite rule.</summary>
	private static SplitPace SpeedPace(double speed, double reference)
	{
		if (reference <= 0d)
		{
			return SplitPace.Unknown;
		}

		return speed >= reference ? SplitPace.Ahead : SplitPace.Behind;
	}

	private static string Speed(double kilometresPerHour)
	{
		return kilometresPerHour < 0d ? "" : UiNumbers.Text((int)kilometresPerHour);
	}
}
