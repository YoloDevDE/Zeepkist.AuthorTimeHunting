using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     What the run HUD shows, as data. Built from the run once per frame and shared by the bar,
///     its drawer and the level card, so they cannot drift apart mid-frame.
/// </summary>
public class RunHudView
{
	private const string _noTime = "--:--.---";

	private static int _frame = -1;

	private static AthController _run;
	private static RunHudView _view;

	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	/// <summary>Whether the hour is actually being spent right now, which is what the live dot says.</summary>
	public bool Running { get; private set; }

	public string SkipType { get; private set; }

	public Color32 SkipColour { get; private set; }

	public string TimeLeft { get; private set; }

	public Color32 TimeColour { get; private set; }

	public IReadOnlyList<TimelineSegment> Timeline { get; private set; } = [];

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	public string TimeLostToPenalties { get; private set; }

	public string LevelName { get; private set; }

	/// <summary>The name on its own. The bar writes the "by" itself, in a colour of its own.</summary>
	public string Author { get; private set; }

	public string AuthorTime { get; private set; }
	public string GoldTime { get; private set; }

	/// <summary>Whether the gold time is already in the bag, which is what stops it being a target.</summary>
	public bool GoldOwned { get; private set; }

	/// <summary>How long this level has had of the hour, which is the other clock a hunt is spent by.</summary>
	public string LevelTime { get; private set; }

	public string Attempts { get; private set; }

	public string PersonalBest { get; private set; }

	public Color32 SkipCostColour { get; private set; }

	public string LevelsPlayed { get; private set; }

	public string TotalAttempts { get; private set; }

	public static RunHudView ForFrame(AthController run)
	{
		if (_frame == Time.frameCount && ReferenceEquals(_run, run))
		{
			return _view;
		}

		_view = From(run);
		_frame = Time.frameCount;
		_run = run;

		return _view;
	}

	public static void Clear()
	{
		_frame = -1;
		_run = null;
		_view = null;
	}

	private static RunHudView From(AthController run)
	{
		AthCtx ctx = run?.Ctx;
		Level level = ctx?.CurrentLevel;

		if (level == null)
		{
			return null;
		}

		bool paused = ctx.IsPaused;
		double remaining = ctx.GetRemainingTime().TotalMilliseconds;

		bool running = !paused && level.IsTiming;

		Color32 timeColour = TimeLeftColour(ctx, running);

		return new RunHudView
		{
			Paused = paused,
			Running = running,
			TimeLeft = TimeFormatter.FormatDuration((int)remaining),
			TimeColour = timeColour,
			Timeline = BuildTimeline(ctx, timeColour),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			TimeLostToPenalties = TimeSpan.FromMilliseconds(ctx.GetAccumulatedPenaltyTime()).ToFormattedString(),
			SkipType = SkipTypeLabel(ctx),
			SkipColour = SkipTypeColour(ctx),
			SkipCostColour = SkipTypeColour(ctx),
			LevelName = level.Name,
			Author = level.Author,
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			GoldTime = TimeFormatter.FormatTime(level.GoldTime),
			GoldOwned = level.GoldMedalAcquired,
			LevelTime = TimeFormatter.FormatDuration((int)level.GetPlayDuration().TotalMilliseconds),
			Attempts = UiNumbers.Text(level.Attempt),
			PersonalBest = level.PersonalBestTime < 0f ? _noTime : TimeFormatter.FormatTime(level.PersonalBestTime),
			LevelsPlayed = UiNumbers.Text(ctx.Levels.Count),
			TotalAttempts = UiNumbers.Text(ctx.Levels.Sum(played => played.Attempt))
		};
	}

	/// <summary>
	///     The hour so far, one segment per level in play order, with the penalties on the end.
	///     Broken levels get nothing, because their time is refunded.
	/// </summary>
	private static IReadOnlyList<TimelineSegment> BuildTimeline(AthCtx ctx, Color32 liveColour)
	{
		if (ctx.Duration <= 0)
		{
			return [];
		}

		List<TimelineSegment> segments = [];

		foreach (Level level in ctx.Levels)
		{
			Add(segments, ctx.Duration, level.LevelBroken ? 0d : level.GetPlayDuration().TotalMilliseconds,
				SegmentColour(level, ctx.CurrentLevel, liveColour));
		}

		Add(segments, ctx.Duration, ctx.GetAccumulatedPenaltyTime(), Color.Style.Status.Danger);

		return segments;
	}

	private static void Add(List<TimelineSegment> segments, int duration, double milliseconds, Color32 colour)
	{
		if (milliseconds <= 0d)
		{
			return;
		}

		segments.Add(new TimelineSegment(Mathf.Clamp01((float)(milliseconds / duration)), colour));
	}

	/// <summary>
	///     What a level was worth, except for the one being driven - that one is still worth
	///     whatever the clock says, so it carries the clock's own colour and reddens with it.
	/// </summary>
	private static Color32 SegmentColour(Level level, Level current, Color32 liveColour)
	{
		if (ReferenceEquals(level, current))
		{
			return liveColour;
		}

		return level.Status switch
		{
			LevelStatus.Author => Color.Zeepkist.Medal.Author,
			LevelStatus.Gold => Color.Zeepkist.Medal.Gold,
			LevelStatus.Free => Color.Style.Status.FreeSkip,
			LevelStatus.Failed => Color.Style.Status.Penalty,
			_ => Color.Style.Text.Muted
		};
	}

	private static Color32 TimeLeftColour(AthCtx ctx, bool running)
	{
		if (!running)
		{
			return Color.Style.Text.Muted;
		}

		if (ctx.IsTimeRunningLow)
		{
			return Color.Style.Status.Danger;
		}

		return ctx.IsTimeAfterSkipRunningLow ? Color.Style.Status.Warning : Color.Style.Status.Good;
	}

	/// <summary>What skipping this level right now would do, as one short sentence.</summary>
	private static string SkipTypeLabel(AthCtx ctx)
	{
		if (ctx.CurrentLevel.AuthorTimeAcquired)
		{
			return "Skip: free, author time taken";
		}

		if (ctx.CurrentLevel.GoldMedalAcquired)
		{
			return "Skip: free, gold taken";
		}

		if (ctx.AvaiableFreeSkips == 1)
		{
			return "Skip: free, 1 free skip left";
		}

		if (ctx.AvaiableFreeSkips > 1)
		{
			return $"Skip: free, {ctx.AvaiableFreeSkips} free skips left";
		}

		if (ctx.IsTimeRunningLow)
		{
			return "Skip: ends the run";
		}

		return "Skip: costs " + TimeFormatter.FormatDuration(ctx.PenaltyTimeInMilliseconds);
	}

	private static Color32 SkipTypeColour(AthCtx ctx)
	{
		if (ctx.CurrentLevel.AuthorTimeAcquired)
		{
			return Color.Zeepkist.Medal.Author;
		}

		if (ctx.CurrentLevel.GoldMedalAcquired)
		{
			return Color.Zeepkist.Medal.Gold;
		}

		if (ctx.AvaiableFreeSkips > 0)
		{
			return Color.Style.Status.FreeSkip;
		}

		return ctx.IsTimeRunningLow ? Color.Style.Status.Fatal : Color.Style.Status.Penalty;
	}
}
