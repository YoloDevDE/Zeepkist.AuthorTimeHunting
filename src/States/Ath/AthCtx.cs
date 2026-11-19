using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;

namespace AuthorTimeHunting.States.Ath;

/// <summary>
///     The state of a single run: played levels, remaining time budget and scratch values the states share.
/// </summary>
public class AthCtx
{
	private const int _retries = 3;
	private bool _previousTimeRunningLowState;

	public AthCtx(RunSettings settings)
	{
		Settings = settings;
		Duration = settings.DurationMs;
		PenaltyTimeInMilliseconds = settings.PenaltyTimeMs;
		AvaiableFreeSkips = settings.FreeSkips;
	}

	public RunSettings Settings { get; }

	public int Duration { get; }

	public int PenaltyTimeInMilliseconds { get; }

	public Level CurrentLevel { get; set; }

	public List<Level> Levels { get; } = [];

	public int Retries { get; set; } = _retries;

	public int ConsecutiveDuplicateCount { get; set; }

	public int ConsecutiveBrokenCount { get; set; }

	public int AvaiableFreeSkips { get; set; }

	public bool IsPaused { get; set; }

	public LevelStatus LastRunMedalStatus { get; set; } = LevelStatus.Unknown;
	public bool LastRunMedalWasNew { get; set; }
	public double LastRunTime { get; set; } = -1;

	public int AuthorMedals => Levels.Count(level => level.Status == LevelStatus.Author);

	public int GoldMedals => Levels.Count(level => level.Status == LevelStatus.Gold);

	public int Penalties => Levels.Count(level => level.Status == LevelStatus.Failed);

	public bool IsTimeRunningLow => GetRemainingTime().TotalMilliseconds <= PenaltyTimeInMilliseconds;

	public bool IsTimeAfterSkipRunningLow => GetRemainingTime().TotalMilliseconds <= 2 * PenaltyTimeInMilliseconds;

	public int GetAccumulatedPenaltyTime()
	{
		return PenaltyTimeInMilliseconds * Penalties;
	}

	public TimeSpan GetTotalLevelPlayDuration()
	{
		return TimeSpan.FromMilliseconds(Levels.Where(level => !level.LevelBroken)
			.Sum(level => level.GetPlayDuration().TotalMilliseconds));
	}

	public TimeSpan GetRemainingTime()
	{
		return TimeSpan.FromMilliseconds(Duration -
		                                 (GetTotalLevelPlayDuration().TotalMilliseconds + GetAccumulatedPenaltyTime()));
	}

	public TimeSpan GetRemainingTimeWithoutPunishments()
	{
		return TimeSpan.FromMilliseconds(Duration - GetTotalLevelPlayDuration().TotalMilliseconds);
	}

	public bool IsTimeOver()
	{
		return GetRemainingTime() <= TimeSpan.Zero;
	}

	public bool CheckAndNotifyTimeRunningLow()
	{
		bool currentState = IsTimeRunningLow;
		bool shouldNotify = currentState && !_previousTimeRunningLowState;
		_previousTimeRunningLowState = currentState;
		return shouldNotify;
	}

	public void InitializingNewLevel(LevelScriptableObject levelScriptableObject)
	{
		Level level = new(levelScriptableObject);
		CurrentLevel = level;
		Levels.Add(level);

		LastRunTime = -1;
		LastRunMedalStatus = LevelStatus.Unknown;
		LastRunMedalWasNew = false;

		CurrentLevel.Start();

		SeedSplits(level);
	}

	public void ResetRetries()
	{
		Retries = _retries;
	}

	/// <summary>
	///     Seeds splits from the game's replay file - the only reference a hunt has on the first attempt.
	/// </summary>
	private static void SeedSplits(Level level)
	{
		level.TrackSplits(GameStateObserver.ReplaySplits(level.LevelUid));
		level.SealAttempt();
	}
}
