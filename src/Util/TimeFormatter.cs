using System;

namespace AuthorTimeHunting.Util;

/// <summary>Clock-style text for durations and times.</summary>
public abstract class TimeFormatter
{
	public static string FormatDuration(int durationInMilliseconds)
	{
		if (durationInMilliseconds < 0)
		{
			return "none";
		}

		TimeSpan timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);

		if (timeSpan.TotalHours >= 1)
		{
			return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
		}

		if (timeSpan.TotalSeconds >= 60)
		{
			return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
		}

		return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
	}

	public static string FormatTime(double seconds)
	{
		if (seconds < 0)
		{
			return "none";
		}

		TimeSpan span = TimeSpan.FromSeconds(seconds);

		if (span.TotalHours >= 1)
		{
			return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
		}

		return $"{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
	}

	/// <summary>
	///     How far apart two times are, written as a signed clock: <c>-00:01.749</c>.
	/// </summary>
	public static string FormatDelta(double seconds)
	{
		string sign = seconds <= 0 ? "-" : "+";

		return sign + FormatTime(Math.Abs(seconds));
	}
}
