namespace AuthorTimeHunting.UI.Hud;

/// <summary>One note in a clip: what pitch, how far into the clip it starts, and how long it rings.</summary>
public class Tone
{
	public Tone(float frequency, float start, float seconds)
	{
		Frequency = frequency;
		Start = start;
		Seconds = seconds;
	}

	public float Frequency { get; }

	/// <summary>Seconds from the beginning of the clip.</summary>
	public float Start { get; }

	public float Seconds { get; }
}
