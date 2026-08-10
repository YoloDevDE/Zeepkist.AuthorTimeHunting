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
///     The hunt as a broadcast bar: a band across the top of the screen with the hour on a tile in
///     the middle of it, the hunt on the left of that tile and the level on the right, and the
///     controls in a drawer underneath.
///     It was a window in the middle of the top of the screen and it kept growing, until it was
///     eleven rows tall and sat exactly where a player looks on a jump. Flattening it to one line
///     fixed the height and broke the shape instead: a strip the full width of the screen is a
///     letterbox bar, and on an ultrawide it put the clock and the medals a monitor's width
///     apart. So the band is a fraction of the screen and centred, which is what a bar over a
///     game looks like everywhere else it is done.
///     The hour is the middle of it because that is what the middle is for: it is the number the
///     whole mode is made of, and everything on either side of it is what that hour has been spent
///     on. The mod's crest had the tile before it, which is a picture that never changes in the one
///     place the eye keeps going back to.
///     Nothing here is drawn before there is a level under the wheels. The bar used to come up
///     with the run, which meant it hung over the whole of the lobby setup with nothing in it but
///     dashes - and over the loading screen that exists to hide exactly that.
///     It has two heights, because it answers two questions and they are not asked at the same
///     time. Between attempts the question is how the hunt is going: the medal tally, the hour
///     spent on the timeline, whose level this is. On the way down a hill none of that is worth a
///     row - the questions are what the clock says, what is being driven, and what leaving would
///     cost - so the bar folds to those and gives the screen back. See <see cref="Stage" /> for
///     when it switches, and why only one of the two directions waits.
///     There is no visible window here, but there is a window. Imui draws text through the window
///     it is inside, and an earlier attempt at drawing a line of it against the bare canvas drew
///     nothing at all - so the frame is styled away rather than done without, the same way
///     <see cref="AthMenu" /> turns a window into a fullscreen screen. The band is then drawn by
///     hand, because a window box is a rectangle and this one has its corners cut off.
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
	///     How long the full bar stays up once the zeepkists are released. A restart is two seconds
	///     of somebody's life and there are a lot of them in a hunt; a bar that folded on every one
	///     of them would be the most animated thing on the screen.
	/// </summary>
	private const float _settleSeconds = 3f;

	/// <summary>
	///     How far through the slide the band's contents change over. Halfway, where the height is
	///     moving fastest and the swap is least likely to be the thing the eye catches.
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
	///     How wide each block of a wing actually is, in rows.
	///     A wing is as wide as the band leaves it, which is far wider than anything standing in
	///     one. Spread to fill, three medal counts land a hand's width apart and stop reading as
	///     three medal counts, and one of them ends up nearer the crest than the row it belongs to.
	///     So every block takes what it needs off its own edge and leaves the rest empty.
	/// </summary>
	private const float _medalColumns = 7.4f;

	private const float _skipColumns = 9.4f;

	private const float _levelTimeColumns = 9.4f;

	private const string _by = " by ";

	private const string _attempts = " attempts";

	private const string _best = "best ";

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
	///     The corner points of the two hand-drawn shapes, written afresh every frame rather than
	///     allocated every frame. Both are filled up to three times a frame.
	///     A <c>stackalloc</c> would say this without a field, and the compiler takes one. The IDE
	///     does not: the plugin hides System.Memory behind an alias so that only the game's own
	///     <c>Span</c> is visible, the IDE resolves references without applying it, and every
	///     <c>stackalloc</c> here then reads as an error. Do not put them back.
	/// </summary>
	private static readonly Vector2[] _bandPoints = new Vector2[6];

	private static readonly Vector2[] _pennantPoints = new Vector2[5];

	private static readonly Vector2[] _strokePoints = new Vector2[4];

	/// <summary>How far each half of the band has come open, 0 shut and 1 out. See <see cref="Side" />.</summary>
	private readonly float[] _sides = new float[2];

	private readonly ControlPanel _controls;

	private readonly MedalArt _medals;

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

	/// <summary>The attempt the bar last saw, so it can tell a respawn from a frame.</summary>
	private int _attempt;

	public RunOverlay(ControlPanel controls, MedalArt medals)
	{
		_controls = controls;
		_medals = medals;
	}

	/// <summary>
	///     Starting a hunt arms the bar; the first level is what puts it on screen. Everything the
	///     bar has to say comes off the level being driven, so between the two there is nothing to
	///     draw, and a band of empty dashes over the setup screen is worse than no band.
	/// </summary>
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
	///     The drawer is sized before it is drawn, from how far open it was last frame, because the
	///     window it lives in has to be given its rect up front.
	///     The pennant is in the window's rect whether the drawer is out or not. It hangs below the
	///     band by design, and a window clips what is drawn in it - a rect that stopped at the band
	///     would cut the point off every time the drawer was shut.
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

	/// <summary>
	///     Takes the window's frame away entirely: the band is a cut-cornered shape drawn by hand,
	///     and anything the window painted underneath it would show at the corners as the rectangle
	///     the band is not.
	/// </summary>
	private static void Bare(ImGui gui)
	{
		gui.Style.Window.Box.BackColor = Color.clear;
		gui.Style.Window.Box.BorderThickness = 0f;
		gui.Style.Window.Box.BorderRadius = 0f;
		gui.Style.Window.ContentPadding = 0f;
	}

	/// <summary>
	///     Back to front, which in an immediate mode GUI is call order: the drawer first so it
	///     comes out from behind the band, then the band, then the crest over the seam between
	///     them, then everything that is words.
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
	///     The bar's silhouette: a rectangle with its two bottom corners cut away. Imui's window
	///     box cannot do it, so it is six points and a convex fill - counter-clockwise, the winding
	///     Imui's own arrows use.
	///     The band and the drawer are both drawn with it, off the same cut, because the drawer is
	///     the band coming open rather than a second thing under it.
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

		// The tile is the two rows the band always has, never the ones a side has opened. Sized off
		// the whole band it grew with every detail row, and what it grew into was the wings.
		float tileHeight = Mathf.Min(inner.H, gui.GetRowHeight() * _minimalRows);
		float tileWidth = tileHeight * _badgeAspect;
		ImRect tile = new(inner.Center.x - tileWidth * 0.5f, inner.Top - tileHeight, tileWidth, tileHeight);
		float wing = (inner.W - tileWidth) * 0.5f - pad;

		DrawPennant(gui, strip, tileWidth * _pennantWidthFraction, gui.GetRowHeight() * _pennantRows);
		DrawRunTime(gui, tile, view);
		DrawHunt(gui, new ImRect(inner.X, inner.Y, wing, inner.H), view);
		DrawLevel(gui, new ImRect(inner.Right - wing, inner.Y, wing, inner.H), view, full);

		if (!full)
		{
			return;
		}

		UiWidgets.Timeline(gui, new ImRect(strip.X + cut, strip.Y + pad * 0.4f, strip.W - cut * 2f, rule),
			view.Timeline);
	}

	/// <summary>
	///     The tab hanging off the middle of the band. It is the only part of the bar that is not
	///     information, and it is worth its pixels twice over: it is what says there is a drawer,
	///     and it is what stops the crest looking like it was dropped on the band by accident.
	/// </summary>
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

	/// <summary>
	///     The hour, in the middle, on the tile the mod's crest used to sit on.
	///     The crest was the right thing there for as long as the bar was being learned and the
	///     wrong thing for every hunt after: it is the one part of the band that never changed, in
	///     the one place the eye goes back to. The hour is the number the whole mode is - everything
	///     else on the bar is what that hour has been spent on - so it takes the middle, and the
	///     light that says it is running goes with it.
	/// </summary>
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
	///     The left wing: what the hour has bought, and what the next skip would cost it.
	///     Both are the hunt rather than the level, which is what puts them on the same side of the
	///     crest. The score is read between attempts and the price while driving, but neither is
	///     worth taking away for the other - they are two numbers, and a wing holds two rows.
	///     A third row comes down when the pointer is over this half: the same hunt in more detail,
	///     for the moments somebody has stopped to look at it. See <see cref="Side" />.
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

		UiIcons.Draw(gui, glyph, UiIcon.Skip, view.SkipCostColour);
		UiText.Draw(gui, view.SkipCost, view.SkipCostColour, label, gui.Style.Layout.TextSize * 1.1f, 0f);
	}

	/// <summary>
	///     The hunt as a whole, for somebody who has stopped to read it: how many levels it has
	///     been through, how many attempts that took, and how much of the hour went on penalties
	///     rather than on driving.
	/// </summary>
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
	///     The dot a live broadcast puts in the corner: on for most of every second, off for the
	///     rest of it, hard edges both ways. It is the only thing on the bar that says the hour is
	///     being spent right now rather than merely standing at some number, and a fade would read
	///     as breathing rather than as recording.
	///     Unscaled time, so the game slowing itself down cannot stretch the second.
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
	///     The right wing: the level being driven and what it is costing.
	///     Whose level it is, and what it is called, is not on it while it is being driven. That was
	///     the last thing on the bar a player could not act on - the name of a level you are looking
	///     at out of the windscreen - and it took the row the level's own clock now has. It comes
	///     back the moment the driving stops, which is when a name is worth reading.
	/// </summary>
	private void DrawLevel(ImGui gui, ImRect rect, RunHudView view, bool full)
	{
		ImRect detail = rect.TakeBottom(Detail(gui, _right), out ImRect main);

		if (full)
		{
			ImRect title = main.TakeTop(main.H / 3f, out main);

			DrawTitle(gui, title, view, gui.Style.Layout.TextSize);
		}

		ImRect top = main.TakeTop(main.H * 0.5f, out ImRect bottom);
		ImRect times = top.TakeRight(Mathf.Min(top.W, gui.GetRowHeight() * _levelTimeColumns), out ImRect skip);

		DrawSkipType(gui, skip, view);
		DrawTimes(gui, times, view);
		UiText.Draw(gui, view.Attempts + _attempts, Color.Style.Text.Muted, bottom,
			gui.Style.Layout.TextSize * 0.9f, 0f);
		UiText.Right(gui, view.LevelTime, Color.Style.Text.Muted, bottom, gui.Style.Layout.TextSize * 0.9f);
		DrawLevelDetail(gui, detail, view);
	}

	/// <summary>Which kind of skip is on offer here, said in words - the price is on the other wing.</summary>
	private static void DrawSkipType(ImGui gui, ImRect rect, RunHudView view)
	{
		UiText.Draw(gui, view.SkipType, view.SkipColour, rect, gui.Style.Layout.TextSize * 0.9f, 0f);
	}

	private static void DrawLevelDetail(ImGui gui, ImRect rect, RunHudView view)
	{
		if (rect.H <= 0f)
		{
			return;
		}

		float size = gui.Style.Layout.TextSize * 0.85f;

		UiText.Draw(gui, view.LevelName + _by + view.Author, Color.Style.Text.Muted, rect, size, 0f);
		UiText.Right(gui, _best + view.PersonalBest, Color.Style.Text.Muted, rect, size);
	}

	/// <summary>
	///     The level and whose it is on one line: both names in plain white, and only the "by"
	///     between them carrying a colour.
	///     It was two lines, the level in blue over the author in gold, which gave the wing two
	///     coloured labels and no plain text - so neither name was the one being read. One line in
	///     one colour makes them a title, and the joining word is the only thing that has to be
	///     told apart from a name.
	///     Imui draws one colour per call, so the line is measured from the right and each run gets
	///     the rect it ends in. A level with a long name loses its tail rather than the author's -
	///     the name is the half you can already see out of the windscreen.
	/// </summary>
	private static void DrawTitle(ImGui gui, ImRect rect, RunHudView view, float size)
	{
		float author = UiText.Width(gui, view.Author, size);
		float by = UiText.Width(gui, _by, size);

		UiText.Right(gui, view.Author, Color.Style.Surface.White, rect, size);
		UiText.Right(gui, _by, Color.Style.Text.Joiner, Ending(rect, author), size);
		UiText.Right(gui, view.LevelName, Color.Style.Surface.White, Ending(rect, author + by), size);
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

	/// <summary>
	///     A medal that has been taken, with a tick where its time was. The time is what you drive
	///     at while it is still a target; once it is won it is a number that cannot change and one
	///     more thing on the bar to read past.
	/// </summary>
	private static void DrawMedalDone(ImGui gui, ImRect cell, Texture2D medal)
	{
		ImRect icon = cell.TakeLeft(cell.H, gui.Style.Layout.InnerSpacing, out ImRect rest);

		UiWidgets.Medal(gui, icon, medal, Color.Zeepkist.Medal.Gold);
		DrawTick(gui, rest.TakeLeft(rest.H), Color.Style.Status.Positive);
	}

	/// <summary>
	///     A tick, as its two strokes. There is no tick in the icon strip and no glyph for one in
	///     the theme's font, and this is two quads - which is less than it would cost to teach
	///     either of them about a shape used in exactly one place.
	/// </summary>
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
	///     The controls, always laid out at their full height against the underside of the band,
	///     and clipped to however far the drawer has come out. That is what makes it read as a
	///     drawer rather than as a fade: the buttons arrive from behind the band with their far
	///     edge still cut off by it.
	///     The clip does the input too. Imui throws away a hover that falls outside the active clip
	///     rect, so a button half out of the slot is live on exactly the half that can be seen.
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
	///     How much of the bar is showing, and the whole of the rule that decides it: the bar folds
	///     away while the level is being driven and comes back the instant the driving stops.
	///     The two directions are deliberately not symmetric. Coming back is immediate, because the
	///     moment a run ends is the moment its result is worth reading. Folding away waits
	///     <see cref="_settleSeconds" />, because a hunt is mostly restarts - and a bar that folded
	///     on every one of them would spend the level animating rather than saying anything.
	///     A hand on the mouse counts as not driving, whatever the run says. It is the same signal
	///     the drawer opens on and it means the same thing here: somebody has stopped steering and
	///     gone looking for the mod, and what they are looking for is the part that folded away.
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

	/// <summary>
	///     How far each half of the band is open, and what opens it: the pointer being over that
	///     half of it.
	///     A hunt is driven with the hands on the keys and the mouse untouched, so a pointer that
	///     has arrived somewhere is somebody looking rather than somebody steering - the same signal
	///     the drawer opens on. Which half it is over is the question being asked: the left of the
	///     crest is the hunt and the right of it is the level, and reaching for one of them is
	///     asking for more of that one.
	/// </summary>
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
	///     Whether a new attempt has begun since the last frame, which is what a respawn is from
	///     out here. A respawn is the one moment in a hunt when the player has decided the run is
	///     over and has not yet started the next one - the one moment the bar's own numbers are
	///     both readable and worth reading - so it comes back up for them, and then folds away
	///     again on its own once the driving resumes.
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
	///     Whether the pointer has been used lately, which is the question the drawer actually
	///     wants answered. Opening on hover meant aiming at a strip of screen to reach the buttons
	///     behind it, and the strip is at the very top edge, which is the one place a pointer
	///     cannot overshoot into - so it was a drawer that only opened for somebody who already
	///     knew where it was.
	///     Moving the mouse at all is the honest signal here: this is a racing game, the pointer
	///     does not steer anything, and a hand that has gone to it has gone to it for the buttons.
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
