using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The game's own finish panel, answering the question this mode actually asks.
///     <code>
///     game               ATH
///     00:12.340   ->     [gold] acquired!
///     3 of 3 checkpoints You can now skip without a penalty!
///     </code>
///     The panel already puts two things on screen when a run ends, and only one of them was
///     still worth reading. The checkpoint count says whether the run counted at all, which is
///     as true here as anywhere, so it is left alone. The time does not: it was already read
///     off the ticker while driving, and by the time the panel is up the only open question is
///     whether it was enough. So that is what the line says instead - which medal was taken, and
///     underneath it the one thing that medal changes about the hunt.
///     This is the only place a medal is announced. It used to be told twice, here and in a toast
///     in the opposite corner, and the toast was the copy that could not say what it meant: it
///     arrives while the panel is coming up, in the corner the player is not looking at.
///     A run that crossed the line without every checkpoint never gets here. Nothing is written
///     unless the hunt booked a time for the attempt, and it only books one for a finish it
///     would score, so a dnf keeps the game's own panel.
///     The game writes this label once, at the finish, rather than every frame - so unlike the
///     ticker it does not come back on its own, and the text that was replaced is kept and put
///     back when the hunt lets go of the panel.
/// </summary>
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
	///     Gives the panel back, and only puts the old text back if the verdict is still the thing
	///     on it. Restarting clears this label itself - the game blanks every result field before
	///     the next attempt - and writing a remembered finish time over that clear left the last
	///     verdict on screen through the whole of the next run. If somebody else has written here
	///     since, they meant to, and what was on the label before ATH touched it stopped being the
	///     right answer at that moment.
	///     The sprite asset goes back either way. It is not something anybody else writes over, and
	///     a label left pointing at a sprite asset the mod owns draws garbage once the mod disposes
	///     of it.
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

	/// <summary>
	///     What the run was worth, in two lines. <paramref name="medals" /> is null where the label
	///     could not be given the pictures, and every line then names its medal in words instead.
	/// </summary>
	private static string Verdict(AthCtx ctx, MedalSpriteAsset medals)
	{
		if (ctx.LastRunMedalStatus == LevelStatus.Author)
		{
			return Line(Head(medals, MedalSpriteAsset.Author, _authorWord) + _acquired, _authorNote);
		}

		if (ctx.LastRunMedalStatus == LevelStatus.Gold && ctx.LastRunMedalWasNew)
		{
			return Line(Head(medals, MedalSpriteAsset.Gold, _goldWord) + _acquired, _goldNote);
		}

		return Line(Head(medals, MedalSpriteAsset.OhNo, _missWord) + " " + _keepGoing, Missed(ctx));
	}

	/// <summary>
	///     How far the run was off the author time, which is the only number left worth reading once
	///     the medal has been named. A level whose author time the hunt never learned has none.
	/// </summary>
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

		return $"{verdict}\n<size=60%>{note}</size>";
	}
}
