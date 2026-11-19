namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     One line of the split list, already written out. A checkpoint not reached yet is still a
///     row with a placeholder time, so the list does not shift while it is being read.
/// </summary>
public readonly struct SplitRow
{
	public SplitRow(string label, string time, string gap, SplitPace pace, string speed, SplitPace speedPace)
	{
		Label = label;
		Time = time;
		Gap = gap;
		Pace = pace;
		Speed = speed;
		SpeedPace = speedPace;
	}

	public string Label { get; }

	public string Time { get; }

	/// <summary>The signed seconds between this attempt and the best one, or empty where there is no answer.</summary>
	public string Gap { get; }

	public SplitPace Pace { get; }

	public string Speed { get; }

	public SplitPace SpeedPace { get; }
}
