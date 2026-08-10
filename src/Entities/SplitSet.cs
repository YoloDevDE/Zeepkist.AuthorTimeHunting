using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     What a run did at every checkpoint it reached: when it got there, and how fast it was
///     going through. Two lists that are only ever read together and would be a bug the moment
///     they were not the same length, so they travel as one thing.
///     A missing entry is -1 rather than an exception. Both callers ask about checkpoints the
///     run may never have reached - the attempt being driven has not got there yet, and the
///     reference run may be from a level that has since been rebuilt with more of them.
/// </summary>
public class SplitSet
{
	public static readonly SplitSet Empty = new([], []);

	public SplitSet(IReadOnlyList<double> times, IReadOnlyList<double> speeds)
	{
		Times = times ?? [];
		Speeds = speeds ?? [];
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
	///     The better of the two at every checkpoint, each with the speed it was driven at.
	///     Not the better of the two runs: a reference that only ever came from one attempt is one
	///     the player has to finish to get, and the whole point of a split is that it is worth
	///     something on its own. A checkpoint reached faster is a faster checkpoint whatever the
	///     attempt around it did afterwards, which is also what makes the reference survive the
	///     respawn that follows a mistake.
	/// </summary>
	public SplitSet Best(SplitSet other)
	{
		if (other == null || other.Count == 0)
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
