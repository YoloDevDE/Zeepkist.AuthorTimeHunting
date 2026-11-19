using System;
using System.Collections.Generic;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeepSDK.Racing;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The full-screen cover while the mod is doing something the player must not interrupt:
///     leaving a lobby, reaching the server, opening a new one. Ends on the welcome, which shows
///     the rules of the run about to start and a countdown.
/// </summary>
public class LoadingOverlay : IZeepGUIDrawer, IDisposable
{
	private const string _title = "AUTHOR TIME HUNTING";

	private const float _titleSize = 1.8f;
	private const float _messageSize = 1.05f;

	/// <summary>Smaller than a message so "Starting in 5" stays on one line in the narrow column.</summary>
	private const float _stepSize = 0.8f;

	private const float _lineHeight = 1.9f;
	private const float _titleLines = 1.4f;

	/// <summary>The air between the rules of the run and the list of what the mod is doing about it.</summary>
	private const float _stepsGap = 1.2f;

	/// <summary>How much of a line the box in front of a step takes.</summary>
	private const float _markSize = 0.7f;

	private const float _thumbnailWidthFraction = 0.34f;
	private const float _thumbnailMaxWidth = 640f;
	private const float _thumbnailAspect = 9f / 16f;

	private const float _settingsWidthFraction = 0.3f;
	private const float _settingsMaxWidth = 520f;

	private const float _dotsPerSecond = 2f;

	/// <summary>
	///     Thirty seconds of no progress at all - every finished step resets the clock. Past that
	///     something has gone wrong, and a screen covering the whole game must not stay behind.
	/// </summary>
	private const float _maxSeconds = 30f;

	/// <summary>How long the podium runs for - the one wait with a fixed length, so it is counted down.</summary>
	private const float _podiumSeconds = 8f;

	/// <summary>
	///     Fully opaque, unlike <see cref="ColorExtensions.SurfaceColors.Backdrop" />: the menus
	///     behind this one are clickable, and a player who can see them will click them.
	/// </summary>
	private static readonly Color32 _backdrop = new(8, 9, 12, 255);

	private static readonly string _spacedTitle = UiScreen.Spaced(_title);

	/// <summary>The four dot counts the animation cycles through, so no draw builds a string.</summary>
	private static readonly string[] _dotSteps = ["", ".", "..", "..."];

	private readonly OverlayLayer _layer = new();

	/// <summary>What the mod has done so far, oldest first. The last one is the one it is doing.</summary>
	private readonly List<SetupStep> _steps = [];

	private readonly AthImage _thumbnail;

	private float _countdownEnd;

	private float _hideAt;

	private SetupLine[] _lines = [];

	/// <summary>
	///     Set once the game has started loading the level. Only shown on the checklist - the
	///     screen stays up until the hunt has the level.
	/// </summary>
	private bool _loading;

	/// <summary>The level the lobby is about to load, once there is one to name.</summary>
	private NextLevelView _next;

	/// <summary>
	///     Set when the round the lobby was opened with ends, which is the podium starting. From
	///     here the screen describes the level about to load instead of the run.
	/// </summary>
	private bool _podium;

	private string _tagline = "";

	public LoadingOverlay(AthImage thumbnail)
	{
		_thumbnail = thumbnail;
		RacingApi.RoundEnded += OnRoundEnded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public string Message { get; private set; } = "";

	public bool Visible { get; private set; }

	/// <summary>
	///     The hunt this screen is waiting for. It goes away once that hunt has a level under the
	///     wheels, after the game's own loading screen.
	/// </summary>
	public AthController ActiveRun { get; set; }

	public void Dispose()
	{
		RacingApi.RoundEnded -= OnRoundEnded;
		SceneManager.sceneLoaded -= OnSceneLoaded;
		_layer.Drop();
	}

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		if (Done())
		{
			Hide();

			return;
		}

		try
		{
			Draw(gui);
		}
		catch (Exception e)
		{
			Logger.LogError($"LoadingOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Show(string message)
	{
		Message = message;
		_podium = false;
		_loading = false;
		_next = null;
		_lines = [];
		_steps.Clear();
		_tagline = "";
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + _maxSeconds;
		Visible = true;
		_layer.Raise();
		Logger.LogInfo($"LoadingOverlay: {message}");
	}

	/// <summary>
	///     Ticks off whatever the mod was doing and says what it is doing now. A step never puts
	///     the screen back up, so Escape stays dismissed.
	/// </summary>
	public void Step(string label)
	{
		if (!Visible)
		{
			return;
		}

		Finish();

		_steps.Add(new SetupStep(label));
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + _maxSeconds;

		Logger.LogInfo($"LoadingOverlay: {label}");
	}

	/// <summary>
	///     The last step counts down instead of waiting. Used once the mod knows how long is left -
	///     the podium runs for a fixed time, and that time is the run about to start.
	/// </summary>
	public void CountdownTo(float seconds)
	{
		_countdownEnd = Time.unscaledTime + seconds;
	}

	public void Finish()
	{
		if (_steps.Count == 0)
		{
			return;
		}

		_steps[_steps.Count - 1].Done = true;
	}

	/// <summary>The rules of the run about to start, put up beside the checklist rather than instead of it.</summary>
	public void ShowWelcome(IGamemode gamemode)
	{
		_tagline = gamemode.Description;
		_lines = Describe(gamemode);
	}

	public void Hide()
	{
		Visible = false;
		_layer.Drop();
		_lines = [];
		_steps.Clear();
		_tagline = "";
		_next = null;
	}

	/// <summary>
	///     Three ways this screen ends: the hunt got its level, the player pressed Escape, or
	///     nothing has happened for long enough that nothing is going to.
	/// </summary>
	private bool Done()
	{
		if (ActiveRun?.Ctx.CurrentLevel != null)
		{
			return true;
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Logger.LogInfo("LoadingOverlay: Dismissed with Escape.");

			return true;
		}

		return TimedOut();
	}

	private bool TimedOut()
	{
		if (Time.unscaledTime <= _hideAt)
		{
			return false;
		}

		Logger.LogWarning($"LoadingOverlay: '{Message}' has been up for {_maxSeconds:0} seconds. Standing down.");

		return true;
	}

	/// <summary>
	///     The scene load after the podium is the level itself starting to load - the last step,
	///     and the last thing that resets the watchdog.
	/// </summary>
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!Visible || !_podium || _loading)
		{
			return;
		}

		_loading = true;
		Step("Loading the level");
	}

	/// <summary>
	///     The round ending is the podium starting: the screen counts down from here, and the
	///     lobby's playlist has settled on which level is next.
	/// </summary>
	private void OnRoundEnded()
	{
		if (!Visible)
		{
			return;
		}

		_podium = true;
		Step("Starting in");
		CountdownTo(_podiumSeconds);
	}

	private static SetupLine[] Describe(IGamemode gamemode)
	{
		RunSettings settings = gamemode.CreateSettings();

		return
		[
			new SetupLine("Gamemode", gamemode.DisplayName),
			new SetupLine("You have", TimeFormatter.FormatDuration(settings.DurationMs)),
			new SetupLine("A skip costs", TimeFormatter.FormatDuration(settings.PenaltyTimeMs)),
			new SetupLine("Free skips", settings.FreeSkips.ToString()),
			new SetupLine("Levels", settings.RandomPlaylist ? "drawn from the workshop" : "the lobby playlist")
		];
	}

	/// <summary>
	///     Once the podium is running, shows which level is coming in place of the rules. Asked
	///     every frame until the lobby answers, then kept.
	/// </summary>
	private void Preview()
	{
		if (!_podium || _next != null)
		{
			return;
		}

		NextLevelView next = NextLevelView.From(ActiveRun);

		if (next == null)
		{
			return;
		}

		_next = next;
		_tagline = next.Name;
		_lines =
		[
			new SetupLine("Author", next.Author),
			new SetupLine("Author Time", next.AuthorTime),
			new SetupLine("Gold Time", next.GoldTime)
		];
	}

	private void Draw(ImGui gui)
	{
		Preview();

		ImRect screen = gui.Canvas.ScreenRect;

		gui.Canvas.Rect(screen, _backdrop);

		float size = gui.Style.Layout.TextSize;
		float line = size * _lineHeight;

		float picture = Mathf.Min(screen.W * _thumbnailWidthFraction, _thumbnailMaxWidth) * _thumbnailAspect;
		float tagline = _lines.Length == 0 ? 0f : line;
		float rows = _lines.Length * line;
		float steps = Mathf.Max(1, _steps.Count) * line;
		float height = picture + line * (_titleLines + _stepsGap) + tagline + rows + steps;

		float top = screen.Y + (screen.H + height) * 0.5f;
		float underPicture = top - picture;
		float underTitle = underPicture - line * _titleLines;
		float underTagline = underTitle - tagline;
		float underRows = underTagline - rows;

		DrawThumbnail(gui, new ImRect(screen.X, underPicture, screen.W, picture));

		gui.Canvas.Text(_spacedTitle.AsSpan(), Color.Zeepkist.Medal.Author,
			new ImRect(screen.X, underTitle, screen.W, line * _titleLines), size * _titleSize);

		gui.Canvas.Text(_tagline.AsSpan(), Color.Style.Surface.White,
			new ImRect(screen.X, underTagline, screen.W, tagline), size * _messageSize);

		DrawSettings(gui, screen, underTagline, line, size);
		DrawSteps(gui, screen, underRows - line * _stepsGap, line, size);
	}

	private void DrawSteps(ImGui gui, ImRect screen, float top, float line, float size)
	{
		if (_steps.Count == 0)
		{
			gui.Canvas.Text((Message + Dots()).AsSpan(), Color.Style.Text.Muted,
				new ImRect(screen.X, top - line, screen.W, line), size * _messageSize);

			return;
		}

		float width = Mathf.Min(screen.W * _settingsWidthFraction, _settingsMaxWidth);
		float left = screen.X + (screen.W - width) * 0.5f;

		for (int i = 0; i < _steps.Count; i++)
		{
			DrawStep(gui, new ImRect(left, top - line * (i + 1), width, line), _steps[i], size,
				i == _steps.Count - 1);
		}
	}

	/// <summary>
	///     A box that fills in when the step is done. Imui draws everything in one font and a tick
	///     is a glyph that font may not have, so the mark is drawn rather than written.
	/// </summary>
	private void DrawStep(ImGui gui, ImRect row, SetupStep step, float size, bool last)
	{
		float box = size * _markSize;
		ImRect mark = new(row.X, row.Y + (row.H - box) * 0.5f, box, box);

		gui.Canvas.Rect(mark, step.Done ? Color.Style.Status.Positive : Color.Style.Surface.Track, box * 0.25f);

		ImRect text = new(row.X + box * 2f, row.Y, row.W - box * 2f, row.H);

		gui.Canvas.Text(StepLabel(step, last).AsSpan(),
			step.Done ? Color.Style.Text.Muted : Color.Style.Surface.White, text, size * _stepSize, 0f);
	}

	/// <summary>
	///     The step being worked on says so, with dots while the wait is open ended and with the
	///     seconds left once the mod knows how many there are.
	/// </summary>
	private string StepLabel(SetupStep step, bool last)
	{
		if (step.Done || !last)
		{
			return step.Label;
		}

		int left = Mathf.CeilToInt(_countdownEnd - Time.unscaledTime);

		if (_countdownEnd > 0f && left > 0)
		{
			return $"{step.Label} {left}";
		}

		return step.Label + Dots();
	}

	private void DrawSettings(ImGui gui, ImRect screen, float top, float line, float size)
	{
		float width = Mathf.Min(screen.W * _settingsWidthFraction, _settingsMaxWidth);
		float left = screen.X + (screen.W - width) * 0.5f;

		for (int i = 0; i < _lines.Length; i++)
		{
			ImRect row = new(left, top - line * (i + 1), width, line);

			gui.Canvas.Text(_lines[i].Label.AsSpan(), Color.Style.Text.Muted, row, size, 0f);
			gui.Canvas.Text(_lines[i].Value.AsSpan(), Color.Style.Text.Default, row, size, 1f);
		}
	}

	private void DrawThumbnail(ImGui gui, ImRect rect)
	{
		Texture2D picture = Picture();

		if (picture == null)
		{
			return;
		}

		gui.Image(picture, rect, true);
	}

	/// <summary>The level's own picture once the game has loaded one, the mod's logo until then.</summary>
	private Texture2D Picture()
	{
		Texture2D level = _next == null ? null : LevelThumbnails.Get(_next.LevelUid);

		return level == null ? _thumbnail.Texture : level;
	}

	private static string Dots()
	{
		return _dotSteps[(int)(Time.unscaledTime * _dotsPerSecond) % _dotSteps.Length];
	}
}
