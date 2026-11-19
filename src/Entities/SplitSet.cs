using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     What a run did at every checkpoint it reached: time and speed. A checkpoint the run never
///     reached reads as -1 rather than an exception.
/// </summary>
public class SplitSet
{
	public static readonly SplitSet Empty = new([], []);

	public SplitSet(IReadOnlyList<double> times, IReadOnlyList<double> speeds)
	{
		Times = times;
		Speeds = speeds;
	}

	public IReadOnlyList<double> Times { get; }

	public IReadOnlyList<double> Speeds { get; }

	public int Count => Times.Count;

	/// <summary>Seconds off the start at that checkpoint, or -1 where the run has none.</summary>
	public double TimeAt(int index)
	{
		return index >= 0 && index < Times.Count ? Times[index] : -1d;
	}

	/// <summary>Km/h through that checkpoint, or -1 where the run has none.</summary>
	public double SpeedAt(int index)
	{
		return index >= 0 && index < Speeds.Count ? Speeds[index] : -1d;
	}

	/// <summary>
	///     The better of the two at every checkpoint, each with the speed it was driven at -
	///     per checkpoint, not per run.
	/// </summary>
	public SplitSet Best(SplitSet other)
	{
		if (other.Count == 0)
		{
			return this;
		}

		int count = Math.Max(Count, other.Count);

		List<double> times = [];
		List<double> speeds = [];

		for (int index = 0; index < count; index++)
		{
			bool mine = Faster(TimeAt(index), other.TimeAt(index));

			times.Add(mine ? TimeAt(index) : other.TimeAt(index));
			speeds.Add(mine ? SpeedAt(index) : other.SpeedAt(index));
		}

		return new SplitSet(times, speeds);
	}

	/// <summary>Whether <paramref name="mine" /> is the one to keep. A time nobody drove never is.</summary>
	private static bool Faster(double mine, double theirs)
	{
		if (mine < 0d)
		{
			return false;
		}

		return theirs < 0d || mine <= theirs;
	}
}
