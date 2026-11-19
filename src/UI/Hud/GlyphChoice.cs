using System.Linq;
using TMPro;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>Picks a character the label's font can draw.</summary>
public static class GlyphChoice
{
	/// <summary>The first candidate the font can draw. If it can draw none, the last candidate.</summary>
	public static string First(TMP_Text label, string[] candidates)
	{
		string last = candidates.Last();

		if (label.font == null)
		{
			return last;
		}

		foreach (string candidate in candidates)
		{
			if (label.font.HasCharacters(candidate))
			{
				return candidate;
			}
		}

		return last;
	}
}
