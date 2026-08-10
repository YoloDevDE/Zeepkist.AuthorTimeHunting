using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

public class Level
{
	public Level(LevelScriptableObject level)
		: this(level.UID, level.Name, level.Author, level.TimeAuthor, level.TimeGold)
	{
	}

	public Level(string levelUid, string name, string author, double authorTime, double goldTime)
	{
		LevelUid = levelUid;
		Name = name;
		Author = author;
		AuthorTime = authorTime;
		GoldTime = goldTime;
	}

	public string LevelUid { get; }

	public string Name { get; }
	public string Author { get; }
	public double AuthorTime { get; }
	public double GoldTime { get; }

	public int Attempt { get; set; }

	public int Crashes { get; private set; }

	public int WheelsLost { get; private set; }

	public bool Skipped { get; set; }
	public bool FreeSkipped { get; set; }
	public bool LevelBroken { get; set; }

	public LevelStatus Status
	{
		get
		{
			if (LevelBroken)
			{
				return LevelStatus.Broken;
			}

			if (PersonalBestTime <= AuthorTime && PersonalBestTime >= 0)
			{
				return LevelStatus.Author;
			}

			if (PersonalBestTime <= GoldTime && PersonalBestTime >= 0)
			{
				return LevelStatus.Gold;
			}

			if (FreeSkipped)
			{
				return LevelStatus.Free;
			}

			if (Skipped)
			{
				return LevelStatus.Failed;
			}

			{
				return LevelStatus.Unknown;
			}
		}
	}

	public bool AuthorTimeAcquired => Status == LevelStatus.Author;
	public bool GoldMedalAcquired => Status is LevelStatus.Gold or LevelStatus.Author;
	public bool PenaltySkipped => Status == LevelStatus.Failed && Skipped;

	public float PersonalBestTime
	{
		get;
		set
		{
			if (value >= 0 && (field <= 0 || value < field))
			{
				field = value;
			}
		}
	} = -1f;

	/// <summary>
	///     The best each checkpoint has been driven, which is what an attempt is measured against.
	///     It used to be the splits of the best finished run, and that made it worth nothing for
	///     most of a hunt: a level is respawned out of far more often than it is finished, and until
	///     the first clean run there was no reference at all - so the column the panel exists for
	///     was empty exactly while it was being needed. Kept per checkpoint it survives every
	///     respawn, and <see cref="SplitSet.Best" /> says what "better" means.
	/// </summary>
	public SplitSet BestSplits { get; private set; } = SplitSet.Empty;

	/// <summary>
	///     What the attempt being driven has done so far. It is not part of the reference yet - it
	///     is the thing being compared to it, and a checkpoint that was its own best would show
	///     every split dead even.
	/// </summary>
	private SplitSet AttemptSplits { get; set; } = SplitSet.Empty;

	public TimeSpan TimeWasted =>
		LevelBroken ? TimeSpan.Zero
		: AuthorTimeAcquired ? GetPlayDuration() - TimeSpan.FromSeconds(PersonalBestTime)
		: GetPlayDuration();

	public string StatusString
	{
		get
		{
			return Status switch
			{
				LevelStatus.Author => "Completed",
				LevelStatus.Gold => "Gold-Skipped",
				LevelStatus.Free => "Free-Skipped",
				LevelStatus.Broken => "Broken",
				LevelStatus.Failed => "Failed",
				_ => "Unknown"
			};
		}
	}

	public DateTime StartTime { get; set; }

	public DateTime EndTime
	{
		get => field == default ? DateTime.Now : field;
		set;
	}

	private List<DateTime> TimeStamps { get; } = [];

	public bool IsTiming => TimeStamps.Count % 2 == 1;

	public void PauseTiming()
	{
		if (TimeStamps.Count % 2 == 1)
		{
			TimeStamps.Add(DateTime.Now);
		}
	}

	public void ResumeTiming()
	{
		if (TimeStamps.Count % 2 == 0)
		{
			TimeStamps.Add(DateTime.Now);
		}
	}

	/// <summary>
	///     A finished attempt. Whether it is the new best is what <see cref="PersonalBestTime" />
	///     answers by either taking the time or leaving it; the checkpoints it went through are
	///     <see cref="TrackSplits" />'s business either way.
	/// </summary>
	public void RecordRun(float time)
	{
		PersonalBestTime = time;
	}

	/// <summary>Where the attempt being driven has got to, as the game last reported it.</summary>
	public void TrackSplits(SplitSet splits)
	{
		AttemptSplits = splits ?? SplitSet.Empty;
	}

	/// <summary>
	///     The attempt is over, however it ended: what it drove becomes part of what the next one
	///     is measured against. A run the player reset out of has still been past three checkpoints,
	///     and those three are the only reference there is until somebody finishes.
	/// </summary>
	public void SealAttempt()
	{
		BestSplits = BestSplits.Best(AttemptSplits);
		AttemptSplits = SplitSet.Empty;
	}

	public void RegisterCrash()
	{
		Crashes++;
	}

	public void RegisterWheelLost()
	{
		WheelsLost++;
	}

	/// <summary>
	///     A fresh level has no attempts on it. They are counted when the zeepkists are actually
	///     released, so that during a countdown the attempt about to start is always
	///     <c>Attempt + 1</c> - which is what the start lights put on screen.
	/// </summary>
	public void Start()
	{
		StartTime = DateTime.Now;
		TimeStamps.Clear();
		Attempt = 0;
	}

	public void Stop()
	{
		EndTime = DateTime.Now;

		if (TimeStamps.Count % 2 == 1)
		{
			TimeStamps.Add(EndTime);
		}
	}

	public TimeSpan GetPlayDuration()
	{
		if (TimeStamps == null || TimeStamps.Count == 0)
		{
			return TimeSpan.Zero;
		}

		DateTime now = DateTime.Now;
		TimeSpan result = TimeSpan.Zero;

		if (TimeStamps.Count == 1)
		{
			TimeSpan duration = now - TimeStamps[0];
			return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
		}

		for (int i = 0; i < TimeStamps.Count - 1; i++)
		{
			if (i % 2 == 1)
			{
				continue;
			}

			DateTime startTime = TimeStamps[i];
			DateTime endTime = i + 1 < TimeStamps.Count ? TimeStamps[i + 1] : now;

			TimeSpan sessionDuration = endTime - startTime;

			if (sessionDuration > TimeSpan.Zero)
			{
				result += sessionDuration;
			}
		}

		if (TimeStamps.Count % 2 == 1)
		{
			result += now - TimeStamps[^1];
		}

		return result;
	}

	public override bool Equals(object obj)
	{
		if (obj is Level other)
		{
			return LevelUid == other.LevelUid;
		}

		return false;
	}

	public override int GetHashCode()
	{
		return LevelUid.GetHashCode();
	}
}
