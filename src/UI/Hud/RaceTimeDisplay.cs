using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     Rewrites the game's own running-time display into the only number that decides anything: how much time is
///     left before the next medal is gone.
/// </summary>
public class RaceTimeDisplay : IDisposable
{
	private const string _monoSpace = "0.62em";

	private const string _authorLabel = "AT";
	private const string _goldLabel = "G";

	private const int _labelWidth = 3;

	/// <summary>Glyphs in "00:00.000".</summary>
	private const int _timeWidth = 9;

	/// <summary>Label, sign and time, the width every row is padded or centred to.</summary>
	private const int _rowWidth = _labelWidth + 1 + _timeWidth;

	private const string _minusSign = "-";

	/// <summary>Drawn but not seen: the row keeps its place, the player keeps their eye still.</summary>
	private const string _hiddenHex = "#00000000";

	/// <summary>What GameMaster sets its physics clock to when a level restarts.</summary>
	private const float _countdownSeconds = 1.749f;

	/// <summary>
	///     Halfway, which is the whole point: red at the start of the countdown, amber here, green on the release
	///     puts the same gap between each pair.
	/// </summary>
	private const float _amberAt = _countdownSeconds / 2f;

	private const float _greenHoldSeconds = 1f;

	/// <summary>The game's label holds one line and grows downwards, and this one holds four.</summary>
	private const float _linesUp = 1f;

	private const double _shortestLevelWorthLights = 2d;

	/// <summary>Glyphs in "● ● ●", so the row can be centred without measuring the markup.</summary>
	private const int _lampRowWidth = 5;

	private const int _noLights = 0;

	/// <summary>The three the light actually has.</summary>
	private const int _allRed = 1;

	private const int _allAmber = 2;

	private const int _greenStage = 3;

	private const double _closeFraction = 0.75;
	private const double _criticalFraction = 0.92;

	/// <summary>How the chase is going, in the three steps the color already had.</summary>
	private const int _paceSafe = 0;

	private const int _paceClose = 1;

	private const int _paceCritical = 2;

	/// <summary>How often the warning swings from yellow to red and back.</summary>
	private const float _pulsesPerSecond = 1.2f;

	/// <summary>
	///     The lamp, in order of preference, from the big filled circle down to a letter every font on earth has.
	/// </summary>
	private static readonly string[] _lampGlyphs = ["●", "⬤", "◉", "•", "O"];

	private static readonly string _plainHex = Hex(Color.Style.Surface.White);
	private static readonly string _authorHex = Hex(Color.Zeepkist.Medal.Author);
	private static readonly string _goldHex = Hex(Color.Zeepkist.Medal.Gold);

	/// <summary>How the medal being chased is going: comfortable, then tight.</summary>
	private static readonly string _safeHex = Hex(Color.Style.Status.Positive);

	private static readonly string _closeHex = Hex(Color.Style.Status.Close);

	private static readonly string _amberHex = Hex(Color.Style.Status.Close);
	private static readonly string _redHex = Hex(Color.Style.Status.Alert);
	private static readonly string _greenHex = Hex(Color.Style.Status.Positive);

	/// <summary>The medal is in the bag: nothing left to run out of.</summary>
	private static readonly string _claimedHex = Hex(Color.Style.Status.Positive);

	/// <summary>Nothing left to chase. Full red, the one color that cannot be read as anything else.</summary>
	private static readonly string _missedHex = Hex(Color.Style.Status.Alert);

	/// <summary>The place a sign takes up on a time that has none.</summary>
	private static readonly string _noSign = Paint(_minusSign, _hiddenHex);

	/// <summary>A label cell with nothing in it, for the row that is only a clock.</summary>
	private static readonly string _emptyCell = new(' ', _labelWidth);

	/// <summary>What follows a medal, the sprite itself taking the first of the cell's three places.</summary>
	private static readonly string _besideSprite = new(' ', _labelWidth - 1);

	private readonly RaceTimeBehaviour _behaviour;

	private readonly Dictionary<TMP_Text, LabelState> _borrowed = new();

	private readonly MedalSpriteAsset _medals = new();

	private readonly HudSounds _sounds = new();

	private readonly FinishVerdict _verdict = new();

	/// <summary>Whether this level already had gold when this attempt started.</summary>
	private bool _goldOwned;

	/// <summary>
	///     The lamp row as it looks at every stage there is, built once the label has said which shape it can draw.
	/// </summary>
	private string[] _lampRows;

	/// <summary>How the chase last sounded, so each step is played once as it is entered.</summary>
	private int _lastPace = _paceSafe;

	private int _lastStage = _noLights;

	/// <summary>Whether this attempt has already been told the author time is behind it.</summary>
	private bool _missedAuthor;

	/// <summary>Whether the label took the medal sprites. If it did not, the two letters stand in.</summary>
	private bool _sprites;

	public RaceTimeDisplay()
	{
		GameObject host = new(nameof(RaceTimeDisplay)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<RaceTimeBehaviour>();
		_behaviour.Bind(this);
	}

	public AthController ActiveRun
	{
		get;
		set
		{
			field = value;

			if (value == null)
			{
				Restore();
			}
		}
	}

	private bool IsActive => ActiveRun?.Ctx.CurrentLevel != null;

	private string AuthorCell => Cell(MedalSpriteAsset.Author, _authorLabel, _authorHex, _plainHex);

	private string GoldCell => Cell(MedalSpriteAsset.Gold, _goldLabel, _goldHex, _plainHex);

	/// <summary>The attempt is over and its time is on the board.</summary>
	private bool Finished
	{
		get
		{
			AthCtx ctx = ActiveRun?.Ctx;

			return ctx?.CurrentLevel != null && !ctx.CurrentLevel.IsTiming && ctx.LastRunTime >= 0;
		}
	}

	public void Dispose()
	{
		Restore();
		_sounds.Dispose();
		_medals.Dispose();

		if (_behaviour != null)
		{
			Object.Destroy(_behaviour.gameObject);
		}
	}

	public void LateUpdate()
	{
		if (!IsActive)
		{
			Restore();
			return;
		}

		try
		{
			Draw();
		}
		catch (Exception e)
		{
			Logger.LogError($"RaceTimeDisplay: Failed, switching it off: {e.Message}\n{e.StackTrace}");
			ActiveRun = null;
		}
	}

	private void Draw()
	{
		PlayerManager manager = PlayerManager.Instance;

		if (manager == null || manager.currentMaster == null)
		{
			return;
		}

		GameMaster master = manager.currentMaster;
		List<ReadyToReset> players = master.PlayersReady;

		if (players == null || players.Count == 0 || master.setupScript == null)
		{
			return;
		}

		LevelScriptableObject level = master.setupScript.GlobalLevel;

		if (level == null)
		{
			return;
		}

		ReadyToReset player = players[0];

		if (player == null || player.ticker == null || player.screenPointer == null)
		{
			return;
		}

		TMP_Text label = player.screenPointer.elapsedTime;

		if (label == null)
		{
			return;
		}

		Borrow(label);

		int stage = Stage(master.currentLevelPhysicsTime, level.TimeAuthor);
		double elapsed = Elapsed(player);

		Announce(stage);
		Rearm(master.currentLevelPhysicsTime);
		Mourn(elapsed, level.TimeAuthor);

		label.text = Compose(stage, master.currentLevelPhysicsTime, elapsed, level.TimeGold, level.TimeAuthor);

		Judge(player.screenPointer.resultTime);
	}

	/// <summary>A new attempt is a new warning, and the countdown running is what says one has begun.</summary>
	private void Rearm(float physicsTime)
	{
		if (physicsTime >= 0f)
		{
			return;
		}

		_verdict.Restore();
		_lastPace = _paceSafe;
		_missedAuthor = false;
		_goldOwned = ActiveRun?.Ctx.CurrentLevel?.GoldMedalAcquired == true;
	}

	/// <summary>The author time going past, said once.</summary>
	private void Mourn(double elapsed, double authorTime)
	{
		if (_missedAuthor || authorTime <= 0d || elapsed < authorTime || Finished)
		{
			return;
		}

		_missedAuthor = true;

		try
		{
			_sounds.Missed();
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the missed author time: {e.Message}");
		}
	}

	private void Judge(TMP_Text label)
	{
		if (!Finished)
		{
			_verdict.Restore();
			return;
		}

		_verdict.Draw(label, ActiveRun.Ctx, _medals);
	}

	private double Elapsed(ReadyToReset player)
	{
		if (Finished)
		{
			return ActiveRun.Ctx.LastRunTime;
		}

		return player.ticker.GetTicker();
	}

	/// <summary>
	///     The whole block, always written in full: the attempt, the lamps, the clock and the medal still in reach.
	/// </summary>
	private string Compose(int stage, float physicsTime, double elapsed, double goldTime, double authorTime)
	{
		bool countingDown = stage != _noLights && physicsTime < 0f;
		double run = Math.Max(0d, elapsed);

		double last = _goldOwned ? authorTime : goldTime;
		bool gone = !countingDown && last > 0d && run >= last;

		return Mono(AttemptRow(countingDown)
		            + "\n" + _lampRows[stage]
		            + "\n" + ClockRow(countingDown, countingDown ? -physicsTime : run, gone)
		            + "\n" + MedalRows(countingDown, Finished, run, goldTime, authorTime));
	}

	private static int Stage(float physicsTime, double authorTime)
	{
		if (!Plugin.Instance.MyConfig.StartLights.Value)
		{
			return _noLights;
		}

		if (physicsTime < 0f)
		{
			return Countdown(_countdownSeconds + physicsTime);
		}

		if (physicsTime < _greenHoldSeconds && authorTime > _shortestLevelWorthLights)
		{
			return _greenStage;
		}

		return _noLights;
	}

	private static int Countdown(float countedDown)
	{
		return countedDown < _amberAt ? _allRed : _allAmber;
	}

	/// <summary>
	///     One click per lamp and a different one on the release, played on the edge so a stage that lasts half a
	///     second does not turn into a drum roll.
	/// </summary>
	private void Announce(int stage)
	{
		if (stage == _lastStage)
		{
			return;
		}

		_lastStage = stage;

		try
		{
			PlayStage(stage);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the start light sound: {e.Message}");
		}
	}

	private void PlayStage(int stage)
	{
		if (stage == _greenStage)
		{
			_sounds.Go();

			return;
		}

		if (stage != _noLights)
		{
			_sounds.Lamp();
		}
	}

	private static string[] BuildLampRows(string glyph)
	{
		string[] rows = new string[_greenStage + 1];

		for (int stage = 0; stage < rows.Length; stage++)
		{
			rows[stage] = LampRow(stage, glyph);
		}

		return rows;
	}

	private static string LampRow(int stage, string glyph)
	{
		string hex = LampHex(stage);

		return Centered(_lampRowWidth) + Paint($"{glyph} {glyph} {glyph}", hex);
	}

	/// <summary>Three red, three amber, three green - the whole light, one color at a time.</summary>
	private static string LampHex(int stage)
	{
		if (stage == _noLights)
		{
			return _hiddenHex;
		}

		if (stage == _greenStage)
		{
			return _greenHex;
		}

		return stage == _allRed ? _redHex : _amberHex;
	}

	private string AttemptRow(bool countingDown)
	{
		Level level = ActiveRun?.Ctx.CurrentLevel;
		string attempt = level == null ? "" : $"Attempt {level.Attempt + 1}";

		return Centered(attempt.Length) + Paint(attempt, countingDown ? _plainHex : _hiddenHex);
	}

	/// <summary>
	///     The clock itself goes red the moment the last medal is out of reach and stays that way, driving or finished.
	/// </summary>
	private static string ClockRow(bool countingDown, double clock, bool gone)
	{
		if (countingDown)
		{
			return Row(_emptyCell, Paint(_minusSign, _amberHex), TimeFormatter.FormatTime(clock), _amberHex);
		}

		return Row(_emptyCell, _noSign, TimeFormatter.FormatTime(clock), gone ? _missedHex : _plainHex);
	}

	/// <summary>The one medal still in reach, and nothing once there is none.</summary>
	private string MedalRows(bool countingDown, bool finished, double elapsed, double goldTime,
		double authorTime)
	{
		if (countingDown || goldTime <= 0d || authorTime <= 0d)
		{
			return HiddenRow();
		}

		if (elapsed < authorTime)
		{
			return Chase(AuthorCell, finished, elapsed, authorTime);
		}

		if (_goldOwned || elapsed >= goldTime)
		{
			return HiddenRow();
		}

		return Chase(GoldCell, finished, elapsed, goldTime);
	}

	/// <summary>A row nobody can see, so the one below it does not climb a line when it appears.</summary>
	private string HiddenRow()
	{
		return Row(Cell(MedalSpriteAsset.Author, _authorLabel, _hiddenHex, _hiddenHex), _noSign,
			TimeFormatter.FormatTime(0d), _hiddenHex);
	}

	/// <summary>
	///     What a row is labelled with: the medal itself where the game's art and the label are both there for it,
	///     and the letters it used to say where they are not.
	/// </summary>
	private string Cell(int medal, string text, string textHex, string spriteHex)
	{
		if (!_sprites)
		{
			return Paint(text.PadRight(_labelWidth), textHex);
		}

		return MedalSpriteAsset.Tag(medal, spriteHex) + _besideSprite;
	}

	private string Chase(string cell, bool finished, double elapsed, double target)
	{
		string paceHex = finished ? _claimedHex : PaceHex(elapsed, target);

		return Row(cell, Paint(_minusSign, paceHex), TimeFormatter.FormatTime(target - elapsed), paceHex);
	}

	private static string Row(string cell, string sign, string time, string timeHex)
	{
		return cell + sign + Paint(time, timeHex);
	}

	private static string Centered(int width)
	{
		return new string(' ', Math.Max(0, (_rowWidth - width) / 2));
	}

	private static string Paint(string text, string hex)
	{
		return $"<color={hex}>{text}</color>";
	}

	private static string Mono(string text)
	{
		return $"<mspace={_monoSpace}>{text}</mspace>";
	}

	/// <summary>The last stretch breathes between the two warning colors rather than sitting on one.</summary>
	private string PaceHex(double elapsed, double target)
	{
		int pace = Pace(elapsed / target);

		Sound(pace);

		if (pace == _paceSafe)
		{
			return _safeHex;
		}

		if (pace == _paceClose)
		{
			return _closeHex;
		}

		return Hex(Color32.Lerp(Color.Style.Status.Close, Color.Style.Status.Alert, Pulse()));
	}

	private static int Pace(double fraction)
	{
		if (fraction < _closeFraction)
		{
			return _paceSafe;
		}

		return fraction < _criticalFraction ? _paceClose : _paceCritical;
	}

	/// <summary>A sine between 0 and 1.</summary>
	private static float Pulse()
	{
		return (Mathf.Sin(Time.unscaledTime * _pulsesPerSecond * 2f * Mathf.PI) + 1f) * 0.5f;
	}

	/// <summary>One tone as each step is entered, and none while it lasts.</summary>
	private void Sound(int pace)
	{
		if (pace == _lastPace)
		{
			return;
		}

		_lastPace = pace;

		try
		{
			PlayPace(pace);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the warning: {e.Message}");
		}
	}

	/// <summary>A single beep as the medal gets tight, two as it is about to be gone.</summary>
	private void PlayPace(int pace)
	{
		if (pace == _paceClose)
		{
			_sounds.Warning();

			return;
		}

		if (pace == _paceCritical)
		{
			_sounds.Alert();
		}
	}

	private static string Hex(Color32 colour)
	{
		return Color.ToHex(colour);
	}

	/// <summary>
	///     Asks for the medals every frame rather than once, and asks on behalf of the label that is about to be
	///     written rather than on behalf of the session.
	/// </summary>
	private void Dress(TMP_Text label)
	{
		_sprites = _medals.Install(label);
	}

	/// <summary>
	///     Taking the label and then dressing it, in that order: what the label had is written down first, or the
	///     sprite asset put on it a moment later is what <see cref="Restore" /> would hand back to the game.
	/// </summary>
	private void Borrow(TMP_Text label)
	{
		Adopt(label);
		Dress(label);
	}

	private void Adopt(TMP_Text label)
	{
		if (_borrowed.ContainsKey(label))
		{
			return;
		}

		_lampRows ??= BuildLampRows(GlyphChoice.First(label, _lampGlyphs));

		RectTransform transform = label.rectTransform;

		_borrowed[label] = new LabelState(label.overflowMode, label.enableAutoSizing, label.fontSize,
			label.enableWordWrapping, transform.anchoredPosition, label.spriteAsset);

		label.overflowMode = TextOverflowModes.Overflow;

		label.enableWordWrapping = false;

		label.enableAutoSizing = false;

		transform.anchoredPosition += new Vector2(0f, label.fontSize * _linesUp);
	}

	/// <summary>
	///     Handing the labels back includes handing back their sprite assets, so the next run has to be dressed
	///     again from scratch.
	/// </summary>
	private void Restore()
	{
		_verdict.Restore();
		_sprites = false;

		foreach (KeyValuePair<TMP_Text, LabelState> entry in _borrowed)
		{
			if (entry.Key == null)
			{
				continue;
			}

			entry.Key.overflowMode = entry.Value.Overflow;
			entry.Key.enableAutoSizing = entry.Value.AutoSizing;
			entry.Key.fontSize = entry.Value.FontSize;
			entry.Key.enableWordWrapping = entry.Value.WordWrapping;
			entry.Key.rectTransform.anchoredPosition = entry.Value.Position;
			entry.Key.spriteAsset = entry.Value.SpriteAsset;
		}

		_borrowed.Clear();
	}
}
