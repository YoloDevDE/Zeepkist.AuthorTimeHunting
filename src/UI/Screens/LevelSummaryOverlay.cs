using System;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The screen that comes up between levels: the level just played at the top, the run so far
///     below it, which is what the screen is really for.
///     Painted on the canvas, not a window, so it takes no clicks; drawn at <see cref="UiScreen.Order" />.
/// </summary>
public class LevelSummaryOverlay : IZeepGUIDrawer
{
	private const float _titleSize = 1.6f;
	private const float _scoreSize = 2.2f;
	private const float _lineHeight = 1.8f;

	/// <summary>The line beside the thumbnail, tighter so name, verdict and stats fit its height.</summary>
	private const float _sideLine = 1.4f;

	private const float _columnGap = 0.03f;
	private const float _recentRow = 1.1f;

	/// <summary>How much of its column the thumbnail takes, and the shape the game stores it in.</summary>
	private const float _thumbnailWidth = 0.42f;

	private const float _thumbnailAspect = 9f / 16f;

	private static readonly Color32 _ath = Color.Zeepkist.Medal.Author;

	private static readonly float[] _weights = [0.07f, 0.38f, 0.31f, 0.1f, 0.14f];

	private readonly MedalArt _medals;

	private LevelSummaryView _view;

	public LevelSummaryOverlay(MedalArt medals)
	{
		_medals = medals;
	}

	public bool Visible => _view != null;

	public void OnZeepGUI(ImGui gui)
	{
		LevelSummaryView view = _view;

		if (view == null)
		{
			return;
		}

		try
		{
			Draw(gui, view);
		}
		catch (Exception e)
		{
			Logger.LogError($"LevelSummaryOverlay: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Hide();
		}
	}

	public void Show(AthCtx ctx)
	{
		_view = LevelSummaryView.From(ctx);
	}

	public void Hide()
	{
		_view = null;
	}

	private void Draw(ImGui gui, LevelSummaryView view)
	{
		gui.Canvas.PushOrder(UiScreen.Order);

		try
		{
			Paint(gui, view);
		}
		finally
		{
			gui.Canvas.PopOrder();
		}
	}

	private void Paint(ImGui gui, LevelSummaryView view)
	{
		ImRect screen = UiScreen.Full(gui);
		float text = gui.Style.Layout.TextSize;

		gui.Canvas.Rect(screen, Color.Style.Surface.Shade);

		UiColumn body = new(UiScreen.Column(screen), text * _lineHeight);

		body.Space(0.6f);
		DrawLevel(gui, body, view, text);
		body.Space(0.4f);
		UiScreen.Rule(gui, body.Row(0.6f), _ath);
		DrawScore(gui, body.Row(_scoreSize * 1.2f), view, text);
		body.Space(0.3f);
		DrawRecentHeader(gui, body.Row(1f), text);
		DrawRecent(gui, body, view, text);
	}

	/// <summary>The level as a picture on the left, its name and how it went beside it.</summary>
	private static void DrawLevel(ImGui gui, UiColumn body, LevelSummaryView view, float text)
	{
		float width = body.Rest().W * _thumbnailWidth;
		float height = width * _thumbnailAspect;
		ImRect block = body.Row(height / (text * _lineHeight));
		ImRect picture = new(block.X, block.Y, width, height);
		float gap = block.W * _columnGap;

		DrawThumbnail(gui, picture, view, text);

		UiColumn side = new(new ImRect(picture.Right + gap, block.Y, block.W - width - gap, block.H),
			text * _sideLine);

		Left(gui, view.Name, Color.Style.Text.LevelName, side.Row(_titleSize), text * _titleSize);
		Left(gui, view.ByAuthor, Color.Style.Text.AuthorName, side.Row(0.9f), text * 0.9f);
		Left(gui, view.StatusUpper, view.StatusColour, side.Row(_titleSize), text * _titleSize);
		side.Space(0.3f);

		Stat(gui, side.Row(1f), "Author Time", view.AuthorTime, Color.Zeepkist.Medal.Author, text);
		Stat(gui, side.Row(1f), "Your Time", view.Best ?? "never finished", Color.Style.Text.Default, text);
		DrawDelta(gui, side.Row(1f), view, text);
		Stat(gui, side.Row(1f), "Attempts / Crashes", view.Attempts + " / " + view.Crashes, Color.Style.Text.Muted,
			text);
		Stat(gui, side.Row(1f), "Time Here", view.TimeHere, Color.Style.Text.Muted, text);
	}

	private static void DrawDelta(ImGui gui, ImRect row, LevelSummaryView view, float text)
	{
		if (view.AuthorDelta == null)
		{
			return;
		}

		Stat(gui, row, view.AuthorDeltaLabel, view.AuthorDelta, view.AuthorDeltaColour, text);
	}

	private static void DrawThumbnail(ImGui gui, ImRect box, LevelSummaryView view, float text)
	{
		Texture2D thumbnail = LevelThumbnails.Get(view.Uid);

		if (thumbnail == null)
		{
			gui.Canvas.Rect(box, Color.Style.Surface.Track, box.H * 0.05f);
			gui.Canvas.Text("no thumbnail".AsSpan(), Color.Style.Text.Muted, box, text);

			return;
		}

		gui.Image(thumbnail, box);
	}

	/// <summary>The run so far, big: the medals on the left, the time left on the right.</summary>
	private void DrawScore(ImGui gui, ImRect row, LevelSummaryView view, float text)
	{
		ImRect medals = new(row.X, row.Y, row.W * 0.55f, row.H);

		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 0, 3), _medals.Author, view.AuthorMedals,
			Color.Zeepkist.Medal.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 1, 3), _medals.Gold, view.GoldMedals,
			Color.Zeepkist.Medal.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 2, 3), _medals.OhNo, view.Penalties,
			Color.Style.Status.Penalty);

		gui.Canvas.Text(view.TimeLeft.AsSpan(), Color.Style.Status.Positive, row, text * _scoreSize, 1f);
	}

	/// <summary>
	///     As much of the run as the column still has room for. A row drawn past the bottom of the
	///     screen is not clipped by anything - there is no window here to clip it.
	/// </summary>
	private static void DrawRecent(ImGui gui, UiColumn column, LevelSummaryView view, float text)
	{
		float line = text * _lineHeight * _recentRow;

		foreach (LevelRow level in view.Recent)
		{
			if (column.Rest().H < line)
			{
				continue;
			}

			DrawRecentRow(gui, column.Row(_recentRow), level, text);
		}
	}

	private static void DrawRecentHeader(ImGui gui, ImRect row, float text)
	{
		float size = text * 0.8f;

		Cell(gui, row, 0, "#", Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 1, "LEVEL", Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 2, "RESULT", Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 3, "TRIES", Color.Style.Text.Muted, size, 1f);
		Cell(gui, row, 4, "TIME", Color.Style.Text.Muted, size, 1f);
	}

	private static void DrawRecentRow(ImGui gui, ImRect row, LevelRow level, float text)
	{
		float size = text * 0.9f;

		Cell(gui, row, 0, UiNumbers.Text(level.Index), Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 1, level.Name, Color.Style.Text.LevelName, size, 0f);
		Cell(gui, row, 2, level.Status, level.StatusColour, size, 0f);
		Cell(gui, row, 3, level.Attempts, Color.Style.Text.Muted, size, 1f);
		Cell(gui, row, 4, level.Duration, Color.Style.Text.Muted, size, 1f);
	}

	private static void Cell(ImGui gui, ImRect row, int column, string value, Color32 colour, float size, float alignX)
	{
		ImRect rect = UiWidgets.Cell(row, _weights, column);

		gui.Canvas.PushClipRect(rect);

		try
		{
			gui.Canvas.Text(value.AsSpan(), colour, rect, UiText.Fit(gui, value, size, rect.W), alignX);
		}
		finally
		{
			gui.Canvas.PopClipRect();
		}
	}


	private static void Stat(ImGui gui, ImRect row, string label, string value, Color32 colour, float size)
	{
		gui.Canvas.Text(label.AsSpan(), Color.Style.Text.Muted, row, size, 0f);
		gui.Canvas.Text(value.AsSpan(), colour, row, size, 1f);
	}

	private static void Left(ImGui gui, string value, Color32 colour, ImRect row, float size)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		gui.Canvas.Text(value.AsSpan(), colour, row, UiText.Fit(gui, value, size, row.W), 0f);
	}
}
