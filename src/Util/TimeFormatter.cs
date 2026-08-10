using System;

namespace AuthorTimeHunting.Util;

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

		return timeSpan.TotalSeconds >= 60 ?
			$"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}" :
			$"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
	}

	public static string FormatTime(double seconds)
	{
		if (seconds < 0)
		{
			return "none";
		}

		TimeSpan span = TimeSpan.FromSeconds(seconds);

		return span.TotalHours >= 1 ?
			$"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}" :
			$"{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
	}

	/// <summary>
	///     How far apart two times are, written as a signed clock: <c>-00:01.749</c>.
	///     The splits used to write their gaps as bare seconds, on the grounds that a split is
	///     behind by tenths rather than by minutes. It is - but it is read in a column under a run
	///     time, and two columns of numbers that count in different units is a column you have to
	///     stop and parse. The same shape for both is worth the three characters.
	/// </summary>
	public static string FormatDelta(double seconds)
	{
		string sign = seconds <= 0 ? "-" : "+";

		return sign + FormatTime(Math.Abs(seconds));
	}
}
