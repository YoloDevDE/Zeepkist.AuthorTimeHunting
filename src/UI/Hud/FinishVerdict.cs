using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>The game's own finish panel, answering the question this mode actually asks.</summary>
public class FinishVerdict
{
	private const string _authorWord = "AUTHOR TIME";

	private const string _goldWord = "GOLD";

	private const string _missWord = "NO MEDAL";

	/// <summary>What a medal changes, in the smaller type a caption is read in.</summary>
	private const string _authorNote = "Respawn to continue";

	private const string _goldNote = "You can now skip without a penalty!";

	private const string _keepGoing = "Keep going!";

	private const string _acquired = " acquired!";

	private static readonly string _white = Color.ToHex(Color.Style.Surface.White);

	private static readonly string _gap = Color.ToHex(Color.Zeepkist.Medal.Gold);

	private TMP_Text _label;

	private string _replaced;

	/// <summary>What the label wrote its sprites against, so it can be given back the way it was.</summary>
	private TMP_SpriteAsset _replacedSprites;

	/// <summary>The last thing written here, so a restore can tell its own text from the game's.</summary>
	private string _written;

	public void Draw(TMP_Text label, AthCtx ctx, MedalSpriteAsset medals)
	{
		if (label == null)
		{
			return;
		}

		if (_label != label)
		{
			Restore();
		}

		if (_label == null)
		{
			_label = label;
			_replaced = label.text;
			_replacedSprites = label.spriteAsset;
		}

		_written = Verdict(ctx, medals.Install(label) ? medals : null);
		label.text = _written;
	}

	/// <summary>
	///     Gives the panel back, and only puts the old text back if the verdict is still the thing on it.
	/// </summary>
	public void Restore()
	{
		if (_label == null)
		{
			return;
		}

		if (_label.text == _written)
		{
			_label.text = _replaced;
		}

		_label.spriteAsset = _replacedSprites;

		_label = null;
		_replaced = null;
		_replacedSprites = null;
		_written = null;
	}

	/// <summary>What the run was worth, in two lines.</summary>
	private static string Verdict(AthCtx ctx, MedalSpriteAsset medals)
	{
		switch (ctx.LastRunMedalStatus)
		{
			case LevelStatus.Author:
				return Line(Head(medals, MedalSpriteAsset.Author, _authorWord) + _acquired, _authorNote);

			case LevelStatus.Gold when ctx.LastRunMedalWasNew:
				return Line(Head(medals, MedalSpriteAsset.Gold, _goldWord) + _acquired, _goldNote);

			default:
				return Line(Head(medals, MedalSpriteAsset.OhNo, _missWord) + " " + _keepGoing, Missed(ctx));
		}
	}

	/// <summary>How far the run was off the author time, which is the only number left worth reading once the medal has
	/// been named.</summary>
	private static string Missed(AthCtx ctx)
	{
		double authorTime = ctx.CurrentLevel?.AuthorTime ?? 0d;

		if (authorTime <= 0d)
		{
			return "";
		}

		return $"AT missed by <color={_gap}>{TimeFormatter.FormatDelta(ctx.LastRunTime - authorTime)}</color>";
	}

	/// <summary>The medal itself where the label can draw one, and its name where it cannot.</summary>
	private static string Head(MedalSpriteAsset medals, int medal, string word)
	{
		if (medals == null || !medals.Has(medal))
		{
			return word;
		}

		return MedalSpriteAsset.Tag(medal, _white);
	}

	/// <summary>The verdict over its caption, the caption in the smaller type captions are read in.</summary>
	private static string Line(string verdict, string note)
	{
		if (string.IsNullOrEmpty(note))
		{
			return verdict;
		}

		return $"{verdict}<br><br><size=60%>{note}</size>";
	}
}
