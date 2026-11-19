using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>One stretch of the hour, and what it was spent on.</summary>
public class TimelineSegment
{
	public TimelineSegment(float fraction, Color32 colour)
	{
		Fraction = fraction;
		Colour = colour;
	}

	/// <summary>How much of the whole budget this stretch took, between 0 and 1.</summary>
	public float Fraction { get; }

	public Color32 Colour { get; }
}
