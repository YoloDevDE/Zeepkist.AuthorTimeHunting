using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The five things a player does to a running hunt: skip, report broken, pause, restart, stop.
///     Drawn into the drawer under <see cref="RunOverlay" />, which decides when and where.
/// </summary>
public class ControlPanel
{
	private const int _buttons = 5;

	private const string _stopLabel = "Stop";

	/// <summary>Barely wider than tall: the caption under the icon sets the width.</summary>
	private const float _tileAspect = 1.05f;

	private const float _tileRows = 1.25f;

	/// <summary>The skip price above the row, shrunk with the tiles so the drawer stays one block.</summary>
	private const float _captionRows = 0.75f;

	private float _height;

	/// <summary>
	///     How far the drawer has to open to show all of this, measured on the last frame drawn.
	///     The guess in rows only stands until the first measurement.
	/// </summary>
	public float Height(ImGui gui)
	{
		float fallback = gui.GetRowHeight() * (_tileRows + _captionRows + 0.4f);

		return (_height > 0f ? _height : fallback) + UiMetrics.Margin(gui);
	}

	public void Draw(ImGui gui, ImRect rect, AthController run, RunHudView view)
	{
		gui.Layout.Push(ImAxis.Vertical, rect);

		try
		{
			DrawControls(gui, run, view);

			_height = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.Layout.Pop();
		}
	}

	/// <summary>
	///     The five buttons, laid into whatever strip they are handed. Shared with the main menu,
	///     which names the stop button differently because it also has a Quit.
	/// </summary>
	public static void Buttons(ImGui gui, ImRect strip, AthController run, RunHudView view, string stopLabel)
	{
		bool racing = GameStateObserver.IsRacing;

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 0), UiIcon.Skip, "Skip", Color.Style.Action.Skip, racing))
		{
			ChatApi.SendMessage("/fs");
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 1), UiIcon.Warning, "Broken", Color.Style.Action.Broken, racing))
		{
			AthRequests.SkipBroken();
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 2), view.Paused ? UiIcon.Play : UiIcon.Pause,
			    view.Paused ? "Resume" : "Pause",
			    view.Paused ? Color.Style.Action.Resume : Color.Style.Action.Pause, true))
		{
			TogglePause(run, view);
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 3), UiIcon.Restart, "Restart", Color.Style.Action.Restart, true))
		{
			AthRequests.Restart();
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 4), UiIcon.Stop, stopLabel, Color.Style.Action.Stop, true))
		{
			AthRequests.Stop();
		}
	}

	/// <summary>Five tiles of the given height, centered in the row.</summary>
	public static ImRect Centred(ImGui gui, ImRect row, float height)
	{
		float side = height * _tileAspect;
		float gap = gui.Style.Layout.InnerSpacing;
		float width = side * _buttons + gap * (_buttons - 1);

		return new ImRect(row.X + (row.W - width) * 0.5f, row.Y, width, height);
	}

	private static void DrawControls(ImGui gui, AthController run, RunHudView view)
	{
		UiText.Centre(gui, view.SkipType, view.SkipColour, UiMetrics.Row(gui, _captionRows),
			gui.Style.Layout.TextSize * 0.8f);

		Buttons(gui, Strip(gui), run, view, _stopLabel);
	}

	private static void TogglePause(AthController run, RunHudView view)
	{
		if (view.Paused)
		{
			run.ResumeRun();

			return;
		}

		run.PauseRun();
	}

	/// <summary>Five tiles in the middle of the drawer rather than stretched across it.</summary>
	private static ImRect Strip(ImGui gui)
	{
		float height = gui.GetRowHeight() * _tileRows;

		return Centred(gui, gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), height), height);
	}

	private static ImRect Tile(ImGui gui, ImRect strip, int index)
	{
		float side = strip.H * _tileAspect;
		float gap = gui.Style.Layout.InnerSpacing;

		return new ImRect(strip.X + index * (side + gap), strip.Y, side, strip.H);
	}
}
