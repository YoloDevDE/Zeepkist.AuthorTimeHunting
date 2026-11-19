using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     One level of a run: the five list columns and the detail behind them. A class because
///     null doubles as "no level selected".
/// </summary>
public class LevelRow
{
	public LevelRow(int index, string uid, string name, string author, string status, Color32 statusColour,
		string attempts, string duration, string authorTime, string goldTime, string personalBest,
		string authorDelta, string crashes, string wheelsLost)
	{
		Index = index;
		Uid = uid;
		Name = name;
		Status = status;
		StatusColour = statusColour;
		Attempts = attempts;
		Duration = duration;
		AuthorTime = authorTime;
		GoldTime = goldTime;
		Crashes = crashes;
		WheelsLost = wheelsLost;
		Title = $"{name}  ({author})";
		ByAuthor = $"by {author}";
		StatusUpper = status?.ToUpperInvariant();
		BestWithDelta = personalBest == null ? null : $"{personalBest}   ({authorDelta})";
	}

	public int Index { get; private set; }

	public string Title { get; }

	public string ByAuthor { get; }

	public string StatusUpper { get; }

	public string BestWithDelta { get; }

	public string Uid { get; }

	public string Name { get; }
	public string Status { get; }
	public Color32 StatusColour { get; }
	public string Attempts { get; }
	public string Duration { get; }

	public string AuthorTime { get; }
	public string GoldTime { get; }

	public string Crashes { get; }
	public string WheelsLost { get; }

	public LevelRow Renumbered(int index)
	{
		Index = index;

		return this;
	}
}
