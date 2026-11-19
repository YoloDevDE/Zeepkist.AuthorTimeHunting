using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The hunt as a broadcast bar: the hour on a tile in the middle, the hunt to its left, the
///     level to its right, and the controls in a drawer underneath. Folds to a minimal height
///     while driving - see <see cref="Stage" />.
/// </summary>
public class RunOverlay : IZeepGUIDrawer
{
	private const string _windowTitle = "ATH Bar";

	private const ImWindowFlag _windowFlags =
		ImWindowFlag.NoTitleBar | ImWindowFlag.NoCloseButton | ImWindowFlag.NoMovingAndResizing;

	private const float _widthFraction = 0.58f;
	private const float _minWidth = 640f;
	private const float _maxWidth = 1180f;

	/// <summary>How many rows of content the band holds, the rule along its bottom edge aside.</summary>
	private const float _contentRows = 2.7f;

	/// <summary>The same, with the hunt's own numbers taken out of it - see <see cref="Stage" />.</summary>
	private const float _minimalRows = 1.9f;

	/// <summary>What one side coming open adds to the band. One row, because one row is what it says.</summary>
	private const float _detailRows = 1.1f;

	/// <summary>
	///     How long the full bar stays up once the zeepkists are released, so restarts do not
	///     animate it.
	/// </summary>
	private const float _settleSeconds = 3f;

	/// <summary>
	///     How far through the slide the band's contents change over: halfway, where the height
	///     moves fastest.
	/// </summary>
	private const float _swapAt = 0.5f;

	private const float _ruleFraction = 0.16f;

	/// <summary>How much of the band's height its cut corners take. The bar's whole silhouette.</summary>
	private const float _chamferFraction = 0.45f;

	private const float _badgeAspect = 16f / 9f;

	private const float _pennantWidthFraction = 0.34f;
	private const float _pennantRows = 0.55f;

	/// <summary>How big the hour is on the tile in the middle, as a multiple of the theme's text.</summary>
	private const float _clockSize = 1.6f;

	/// <summary>
	///     How wide each block of a wing actually is, in rows. Each block takes what it needs off
	///     its own edge and leaves the rest empty, so the medal counts stay together.
	/// </summary>
	private const float _medalColumns = 7.4f;

	private const float _skipColumns = 14f;

	private const float _levelTimeColumns = 9.4f;

	private const string _by = " by ";

	private const string _attempts = " attempts";

	private const string _best = "best ";

	private const string _gold = "gold ";

	private const int _left = 0;

	private const int _right = 1;

	/// <summary>Open or shut in a fifth of a second: too fast to wait for, too slow to be a jump cut.</summary>
	private const float _slideSeconds = 0.2f;

	/// <summary>One blink a second, lit for most of it. A recording light, not a turn signal.</summary>
	private const float _blinkOnFraction = 0.6f;

	/// <summary>Pixels the pointer has to travel in a frame before it counts as having been moved.</summary>
	private const float _motionThreshold = 1.5f;

	/// <summary>How long a still pointer stays "just used" before the drawer takes it back.</summary>
	private const float _restSeconds = 2.5f;

	/// <summary>
	///     The corner points of the hand-drawn shapes, written afresh every frame rather than
	///     allocated. Not <c>stackalloc</c>: the IDE cannot resolve the plugin's Span alias.
	/// </summary>
	private static readonly Vector2[] _bandPoints = new Vector2[6];

	private static readonly Vector2[] _pennantPoints = new Vector2[5];

	private static readonly Vector2[] _strokePoints = new Vector2[4];

	private readonly ControlPanel _controls;

	private readonly MedalArt _medals;

	/// <summary>How far each half of the band has come open, 0 shut and 1 out. See <see cref="Side" />.</summary>
	private readonly float[] _sides = new float[2];

	/// <summary>The attempt the bar last saw, so it can tell a respawn from a frame.</summary>
	private int _attempt;

	/// <summary>How much of the bar is showing, 0 minimal and 1 full. Its height and its contents follow.</summary>
	private float _full = 1f;

	private bool _mouseOverWindow;

	/// <summary>How far out the drawer is, 0 shut and 1 open. Everything about it follows from this.</summary>
	private float _open;

	private Vector3 _pointer;

	/// <summary>The earliest the bar may start folding away, which is what the settle is made of.</summary>
	private float _shrinkAt;

	private float _stirred;

	private bool _wasDriving;

	public RunOverlay(ControlPanel controls, MedalArt medals)
	{
		_controls = controls;
		_medals = medals;
	}

	/// <summary>Starting a hunt arms the bar; the first level is what puts it on screen.</summary>
	public AthController ActiveRun
	{
		get;
		set
		{
			field = value;

			if (value != null)
			{
				Visible = true;
			}
		}
	}

	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		try
		{
			Draw(gui);
		}
		catch (Exception e)
		{
			Logger.LogError($"RunOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	/// <summary>
	///     The drawer is sized from last frame's opening, because the window needs its rect up front.
	///     The pennant always sits inside the window's rect, so the window does not clip it.
	/// </summary>
	private void Draw(ImGui gui)
	{
		AthController run = ActiveRun;
		RunHudView view = RunHudView.ForFrame(run);

		if (view == null)
		{
			return;
		}

		bool stirred = PointerActive();
		float stage = Stage(run, stirred);
		float row = gui.GetRowHeight();
		float pad = UiMetrics.Margin(gui) * 0.5f;
		float rule = Mathf.Max(3f, row * _ruleFraction) * stage;
		float band = row * Mathf.Lerp(_minimalRows, _contentRows, stage) + pad * 2f + rule +
		             row * _detailRows * Mathf.Max(_sides[_left], _sides[_right]);

		float full = _controls.Height(gui);
		float drawer = full * Advance(stirred);
		float below = Mathf.Max(row * _pennantRows, drawer);

		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = UiMetrics.Width(gui, _widthFraction, _minWidth, _maxWidth);
		ImRect rect = new(screen.X + (screen.W - width) * 0.5f, screen.Top - band - below, width, band + below);

		Sides(gui, new ImRect(rect.X, rect.Top - band, rect.W, band));

		ImStyleWindow previous = gui.Style.Window;

		Bare(gui);

		try
		{
			DrawWindow(gui, rect, band, drawer, run, view, stage);
		}
		finally
		{
			gui.Style.Window = previous;
		}
	}

	/// <summary>Takes the window's frame away entirely: the band is a cut-cornered shape drawn by hand.</summary>
	private static void Bare(ImGui gui)
	{
		gui.Style.Window.Box.BackColor = Color.clear;
		gui.Style.Window.Box.BorderThickness = 0f;
		gui.Style.Window.Box.BorderRadius = 0f;
		gui.Style.Window.ContentPadding = 0f;
	}

	/// <summary>
	///     Back to front, which in an immediate mode GUI is call order: the drawer, then the band,
	///     then everything that is words.
	/// </summary>
	private void DrawWindow(ImGui gui, ImRect rect, float band, float drawer, AthController run, RunHudView view,
		float stage)
	{
		bool open = true;

		if (!gui.BeginWindow(_windowTitle, ref open, ref _mouseOverWindow, rect, _windowFlags))
		{
			return;
		}

		try
		{
			ImRect area = gui.AddLayoutRect(gui.GetLayoutWidth(), rect.H);
			ImRect strip = area.TakeTop(band);
			float cut = Chamfer(strip);

			DrawDrawer(gui, new ImRect(area.X, strip.Y - drawer, area.W, drawer), cut, run, view);
			Chamfered(gui, strip, cut, Color.Style.Surface.Panel);
			DrawContent(gui, strip, cut, view, stage);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	/// <summary>
	///     The bar's silhouette: a rectangle with its two bottom corners cut away, as six points
	///     counter-clockwise. The band and the drawer share it.
	/// </summary>
	private static void Chamfered(ImGui gui, ImRect rect, float cut, Color32 colour)
	{
		_bandPoints[0] = new Vector2(rect.X + cut, rect.Y);
		_bandPoints[1] = new Vector2(rect.Right - cut, rect.Y);
		_bandPoints[2] = new Vector2(rect.Right, rect.Y + cut);
		_bandPoints[3] = new Vector2(rect.Right, rect.Top);
		_bandPoints[4] = new Vector2(rect.X, rect.Top);
		_bandPoints[5] = new Vector2(rect.X, rect.Y + cut);

		gui.Canvas.ConvexFill(_bandPoints, colour);
	}

	private static float Chamfer(ImRect rect)
	{
		return Mathf.Min(rect.H * _chamferFraction, rect.W * 0.03f);
	}

	private void DrawContent(ImGui gui, ImRect strip, float cut, RunHudView view, float stage)
	{
		float pad = UiMetrics.Margin(gui) * 0.5f;
		float rule = Mathf.Max(3f, gui.GetRowHeight() * _ruleFraction) * stage;
		bool full = stage >= _swapAt;

		ImRect inner = strip.WithPadding(pad + cut, pad + cut, pad, rule + pad);

		// The tile is the two rows the band always has, never the ones a side has opened.
		float tileHeight = Mathf.Min(inner.H, gui.GetRowHeight() * _minimalRows);
		float tileWidth = tileHeight * _badgeAspect;
		ImRect tile = new(inner.Center.x - tileWidth * 0.5f, inner.Top - tileHeight, tileWidth, tileHeight);
		float wing = (inner.W - tileWidth) * 0.5f - pad;

		DrawPennant(gui, strip, tileWidth * _pennantWidthFraction, gui.GetRowHeight() * _pennantRows);
		DrawRunTime(gui, tile, view);
		DrawHunt(gui, new ImRect(inner.X, inner.Y, wing, inner.H), view);
		DrawLevel(gui, new ImRect(inner.Right - wing, inner.Y, wing, inner.H), view);

		if (!full)
		{
			return;
		}

		UiWidgets.Timeline(gui, new ImRect(strip.X + cut, strip.Y + pad * 0.4f, strip.W - cut * 2f, rule),
			view.Timeline);
	}

	/// <summary>The tab hanging off the middle of the band - what says there is a drawer.</summary>
	private static void DrawPennant(ImGui gui, ImRect strip, float width, float height)
	{
		float half = width * 0.5f;
		float x = strip.Center.x;

		_pennantPoints[0] = new Vector2(x - half, strip.Y);
		_pennantPoints[1] = new Vector2(x - half, strip.Y - height * 0.45f);
		_pennantPoints[2] = new Vector2(x, strip.Y - height);
		_pennantPoints[3] = new Vector2(x + half, strip.Y - height * 0.45f);
		_pennantPoints[4] = new Vector2(x + half, strip.Y);

		gui.Canvas.ConvexFill(_pennantPoints, Color.Zeepkist.Medal.Gold);
	}

	/// <summary>The hour, in the middle, on its tile, with the light that says it is running.</summary>
	private static void DrawRunTime(ImGui gui, ImRect rect, RunHudView view)
	{
		float radius = rect.H * 0.14f;

		gui.Canvas.Rect(rect, Color.Style.Surface.Tile, radius);
		gui.Canvas.RectOutline(rect, Color.Zeepkist.Medal.Gold, Mathf.Max(1f, rect.H * 0.045f), radius);

		ImRect inner = rect.WithPadding(Mathf.Max(2f, rect.H * 0.12f));
		float light = inner.H * 0.32f;
		ImRect column = inner.TakeRight(light, gui.Style.Layout.InnerSpacing, out ImRect clock);

		UiText.Centre(gui, view.TimeLeft, view.TimeColour, clock, gui.Style.Layout.TextSize * _clockSize);
		DrawLive(gui, new ImRect(column.X, column.Center.y - light * 0.5f, light, light), view.Running);
	}

	/// <summary>
	///     The left wing: what the hour has bought, and what the next skip would cost it. A third
	///     row comes down while the pointer is over this half - see <see cref="Sides" />.
	/// </summary>
	private void DrawHunt(ImGui gui, ImRect rect, RunHudView view)
	{
		float row = gui.GetRowHeight();
		ImRect detail = rect.TakeBottom(Detail(gui, _left), out ImRect main);
		ImRect medals = main.TakeTop(main.H * 0.5f, out ImRect price);

		DrawMedals(gui, medals.TakeLeft(Mathf.Min(medals.W, row * _medalColumns)), view);
		DrawSkipPrice(gui, price.TakeLeft(Mathf.Min(price.W, row * _skipColumns)), view);
		DrawRunDetail(gui, detail, view);
	}

	/// <summary>What leaving this level right now would cost, in the colour of what it would cost.</summary>
	private static void DrawSkipPrice(ImGui gui, ImRect rect, RunHudView view)
	{
		float icon = rect.H * 0.7f;
		ImRect glyph = rect.TakeLeft(icon, gui.Style.Layout.InnerSpacing, out ImRect label);

		string text = view.SkipType;
		float size = UiText.Fit(gui, text, gui.Style.Layout.TextSize, label.W);

		UiIcons.Draw(gui, glyph, UiIcon.Skip, view.SkipCostColour);
		UiText.Draw(gui, text, view.SkipCostColour, label, size, 0f);
	}

	/// <summary>The hunt as a whole: levels played, attempts, and how much of the hour went on penalties.</summary>
	private static void DrawRunDetail(ImGui gui, ImRect rect, RunHudView view)
	{
		if (rect.H <= 0f)
		{
			return;
		}

		float size = gui.Style.Layout.TextSize * 0.85f;

		UiText.Draw(gui,
			$"{view.LevelsPlayed} levels   {view.TotalAttempts} attempts   {view.TimeLostToPenalties} lost",
			Color.Style.Text.Muted, rect, size, 0f);
	}

	/// <summary>
	///     The dot a live broadcast puts in the corner: on for most of every second, hard edges both
	///     ways. Unscaled time, so the game slowing down cannot stretch the second.
	/// </summary>
	private static void DrawLive(ImGui gui, ImRect rect, bool running)
	{
		if (!running)
		{
			UiIcons.Draw(gui, rect, UiIcon.Stop, Color.Style.Status.Warning);

			return;
		}

		if (Time.unscaledTime % 1f > _blinkOnFraction)
		{
			return;
		}

		gui.Canvas.Circle(rect.Center, rect.H * 0.25f, Color.Style.Status.Alert);
	}

	private void DrawMedals(ImGui gui, ImRect rect, RunHudView view)
	{
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, rect, 0, 3), _medals.Author, view.AuthorMedals,
			Color.Zeepkist.Medal.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, rect, 1, 3), _medals.Gold, view.GoldMedals,
			Color.Zeepkist.Medal.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, rect, 2, 3), _medals.OhNo, view.Penalties,
			Color.Style.Status.Penalty);
	}

	/// <summary>
	///     The right wing: the level being driven, always named because the game's own name is hidden,
	///     and what it is costing.
	/// </summary>
	private void DrawLevel(ImGui gui, ImRect rect, RunHudView view)
	{
		ImRect detail = rect.TakeBottom(Detail(gui, _right), out ImRect main);

		ImRect title = main.TakeTop(main.H / 3f, out main);

		DrawTitle(gui, title, view, gui.Style.Layout.TextSize);

		ImRect top = main.TakeTop(main.H * 0.5f, out ImRect bottom);
		ImRect times = top.TakeRight(Mathf.Min(top.W, gui.GetRowHeight() * _levelTimeColumns));

		float size = gui.Style.Layout.TextSize * 0.9f;
		ImRect spent = bottom.TakeRight(UiText.Width(gui, view.LevelTime, size), gui.Style.Layout.InnerSpacing,
			out ImRect attempts);

		DrawTimes(gui, times, view);
		UiText.Draw(gui, view.Attempts + _attempts, Color.Style.Text.Muted, attempts, size, 0f);
		UiText.Right(gui, view.LevelTime, Color.Style.Text.Muted, spent, size);
		DrawLevelDetail(gui, detail, view);
	}

	/// <summary>The level in detail: your best on it, and the gold time.</summary>
	private static void DrawLevelDetail(ImGui gui, ImRect rect, RunHudView view)
	{
		if (rect.H <= 0f)
		{
			return;
		}

		float size = gui.Style.Layout.TextSize * 0.85f;
		string best = _best + view.PersonalBest;
		ImRect right = rect.TakeRight(UiText.Width(gui, best, size), gui.Style.Layout.InnerSpacing, out ImRect left);

		UiText.Draw(gui, _gold + view.GoldTime, Color.Style.Text.Muted, left, size, 0f);
		UiText.Right(gui, best, Color.Style.Text.Muted, right, size);
	}

	/// <summary>
	///     The level and whose it is on one line, only the "by" in color. Imui draws one color per
	///     call, so the size is fitted for the whole line first, then each part is drawn from the right.
	/// </summary>
	private static void DrawTitle(ImGui gui, ImRect rect, RunHudView view, float size)
	{
		string line = view.LevelName + _by + view.Author;
		float fitted = UiText.Fit(gui, line, size, rect.W);

		float author = UiText.Width(gui, view.Author, fitted);
		float by = UiText.Width(gui, _by, fitted);

		UiText.Right(gui, view.Author, Color.Style.Surface.White, rect, fitted);
		UiText.Right(gui, _by, Color.Style.Text.Joiner, Ending(rect, author), fitted);
		UiText.Right(gui, view.LevelName, Color.Style.Surface.White, Ending(rect, author + by), fitted);
	}

	/// <summary>The same rect with its right edge pulled in, so the run before it ends where this one starts.</summary>
	private static ImRect Ending(ImRect rect, float taken)
	{
		return new ImRect(rect.X, rect.Y, Mathf.Max(0f, rect.W - taken), rect.H);
	}

	private void DrawTimes(ImGui gui, ImRect rect, RunHudView view)
	{
		DrawMedalTime(gui, UiWidgets.Column(gui, rect, 0, 2), _medals.Author, view.AuthorTime,
			Color.Zeepkist.Medal.Author);

		ImRect gold = UiWidgets.Column(gui, rect, 1, 2);

		if (!view.GoldOwned)
		{
			DrawMedalTime(gui, gold, _medals.Gold, view.GoldTime, Color.Zeepkist.Medal.Gold);

			return;
		}

		DrawMedalDone(gui, gold, _medals.Gold);
	}

	private static void DrawMedalTime(ImGui gui, ImRect cell, Texture2D medal, string time, Color32 colour)
	{
		ImRect icon = cell.TakeLeft(cell.H, gui.Style.Layout.InnerSpacing, out ImRect text);

		UiWidgets.Medal(gui, icon, medal, colour);
		UiText.Draw(gui, time, colour, text, gui.Style.Layout.TextSize * 0.9f, 0f);
	}

	/// <summary>A medal that has been taken, with a tick where its time was.</summary>
	private static void DrawMedalDone(ImGui gui, ImRect cell, Texture2D medal)
	{
		ImRect icon = cell.TakeLeft(cell.H, gui.Style.Layout.InnerSpacing, out ImRect rest);

		UiWidgets.Medal(gui, icon, medal, Color.Zeepkist.Medal.Gold);
		DrawTick(gui, rest.TakeLeft(rest.H), Color.Style.Status.Positive);
	}

	/// <summary>A tick, as its two strokes - neither the icon strip nor the font has one.</summary>
	private static void DrawTick(ImGui gui, ImRect rect, Color32 colour)
	{
		float size = Mathf.Min(rect.W, rect.H);
		float thickness = Mathf.Max(2f, size * 0.16f);

		Vector2 low = new(rect.X + size * 0.42f, rect.Y + size * 0.28f);

		Stroke(gui, new Vector2(rect.X + size * 0.15f, rect.Y + size * 0.55f), low, thickness, colour);
		Stroke(gui, low, new Vector2(rect.X + size * 0.85f, rect.Y + size * 0.8f), thickness, colour);
	}

	/// <summary>One straight stroke of a given thickness, as the quad it is.</summary>
	private static void Stroke(ImGui gui, Vector2 from, Vector2 to, float thickness, Color32 colour)
	{
		Vector2 along = (to - from).normalized;
		Vector2 across = new Vector2(-along.y, along.x) * (thickness * 0.5f);

		_strokePoints[0] = from - across;
		_strokePoints[1] = to - across;
		_strokePoints[2] = to + across;
		_strokePoints[3] = from + across;

		gui.Canvas.ConvexFill(_strokePoints, colour);
	}

	/// <summary>
	///     The controls, laid out at full height under the band and clipped to however far the
	///     drawer is out. The clip also limits input to the visible half.
	/// </summary>
	private void DrawDrawer(ImGui gui, ImRect rect, float cut, AthController run, RunHudView view)
	{
		if (rect.H <= 0f)
		{
			return;
		}

		float full = _controls.Height(gui);
		float pad = UiMetrics.Margin(gui);

		gui.Canvas.PushClipRect(rect);

		try
		{
			Chamfered(gui, rect, Mathf.Min(cut, rect.H), Color.Style.Surface.Panel);
			_controls.Draw(gui, new ImRect(rect.X + pad, rect.Top - full, rect.W - pad * 2f, full), run, view);
		}
		finally
		{
			gui.Canvas.PopClipRect();
		}
	}

	/// <summary>
	///     How much of the bar is showing: it folds after <see cref="_settleSeconds" /> of driving and
	///     comes back the instant driving stops. A moving mouse counts as not driving.
	/// </summary>
	private float Stage(AthController run, bool stirred)
	{
		bool driving = !stirred && run?.CurrentLeafState is StateAthWaitingForFinish;

		if (driving != _wasDriving || Respawned(run))
		{
			_wasDriving = driving;
			_shrinkAt = Time.unscaledTime + _settleSeconds;
		}

		float step = Time.unscaledDeltaTime / _slideSeconds;

		if (!driving)
		{
			_full = Mathf.Clamp01(_full + step);

			return _full;
		}

		if (Time.unscaledTime < _shrinkAt)
		{
			return _full;
		}

		_full = Mathf.Clamp01(_full - step);

		return _full;
	}

	/// <summary>Opens whichever half of the band the pointer is over: left is the hunt, right is the level.</summary>
	private void Sides(ImGui gui, ImRect band)
	{
		Vector2 pointer = gui.Input.MousePosition;
		bool inside = band.Contains(pointer);

		Open(_left, inside && pointer.x < band.Center.x);
		Open(_right, inside && pointer.x >= band.Center.x);
	}

	private void Open(int side, bool wanted)
	{
		float step = Time.unscaledDeltaTime / _slideSeconds;

		_sides[side] = Mathf.Clamp01(_sides[side] + (wanted ? step : -step));
	}

	/// <summary>How much room this side's extra row has right now, which is none until it opens.</summary>
	private float Detail(ImGui gui, int side)
	{
		return gui.GetRowHeight() * _detailRows * _sides[side];
	}

	/// <summary>
	///     Whether a new attempt has begun since the last frame. A respawn brings the full bar back
	///     up until driving resumes.
	/// </summary>
	private bool Respawned(AthController run)
	{
		int attempt = run?.Ctx.CurrentLevel?.Attempt ?? 0;

		if (attempt == _attempt)
		{
			return false;
		}

		_attempt = attempt;
		_full = 1f;

		return true;
	}

	private float Advance(bool stirred)
	{
		float step = Time.unscaledDeltaTime / _slideSeconds;

		_open = Mathf.Clamp01(_open + (stirred ? step : -step));

		return _open;
	}

	/// <summary>
	///     Whether the pointer has been used lately - the drawer's signal. The pointer steers
	///     nothing in a racing game, so a hand on it is reaching for the buttons.
	/// </summary>
	private bool PointerActive()
	{
		Vector3 now = Input.mousePosition;
		bool moved = (now - _pointer).sqrMagnitude > _motionThreshold * _motionThreshold;

		_pointer = now;

		if (moved)
		{
			_stirred = Time.unscaledTime;
		}

		return Time.unscaledTime - _stirred < _restSeconds;
	}
}
