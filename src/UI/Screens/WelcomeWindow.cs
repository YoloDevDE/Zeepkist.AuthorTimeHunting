using System;
using System.Linq;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The screen a player meets before they ever press Start: what ATH is, what the gamemode
///     asks of them, and the one button in the mod that can be abused.
/// </summary>
public class WelcomeWindow : IZeepGUIDrawer
{
	private const string _windowTitle = "Welcome to ATH";

	private const float _widthFraction = 0.6f;
	private const float _heightFraction = 0.8f;
	private const float _minWidth = 700f;
	private const float _minHeight = 560f;

	private const float _titleSize = 2.1f;

	private const ImWindowFlag _windowFlags = ImWindowFlag.NoResizing;

	private WelcomeMode[] _modes = [];

	private bool _mouseOverWindow;

	public bool Visible
	{
		get;
		set
		{
			bool wasVisible = field;
			field = value;

			if (value && !wasVisible)
			{
				_modes = BuildModes();
			}
		}
	}

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
			Logger.LogError($"WelcomeWindow: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	private void Draw(ImGui gui)
	{
		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = Mathf.Min(Mathf.Max(screen.W * _widthFraction, _minWidth), screen.W);
		float height = Mathf.Min(Mathf.Max(screen.H * _heightFraction, _minHeight), screen.H);

		ImRect rect = new(screen.Left + (screen.W - width) * 0.5f,
			screen.Bottom + (screen.H - height) * 0.5f,
			width,
			height);

		bool open = true;

		if (!gui.BeginWindow(_windowTitle, ref open, ref _mouseOverWindow, rect, _windowFlags))
		{
			return;
		}

		try
		{
			DrawHeadline(gui);
			DrawBody(gui);
			DrawFooter(gui);
		}
		finally
		{
			gui.EndWindow();
		}

		if (!open)
		{
			Visible = false;
		}
	}

	private static void DrawHeadline(ImGui gui)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, "WELCOME TO", UiText.Muted(gui), UiMetrics.Row(gui, 1f), text * 0.9f);
		UiText.Centre(gui, "Author Time Hunting", UiText.Default(gui), UiMetrics.Row(gui, _titleSize * 1.2f),
			text * _titleSize);
		UiText.Centre(gui, "Beat the author's time. Then do it again, until the hour is gone.",
			Color.Zeepkist.Medal.Author,
			UiMetrics.Row(gui, 1.3f), text * 1.05f);

		gui.AddSpacing();
	}

	private void DrawBody(ImGui gui)
	{
		float footer = UiMetrics.ButtonHeight(gui) + gui.GetRowHeight() + gui.Style.Layout.Spacing * 3f;
		ImRect body = gui.AddLayoutRect(gui.GetLayoutWidth(),
			Mathf.Max(gui.GetLayoutHeight() - footer, gui.GetRowHeight()));

		gui.Layout.Push(ImAxis.Vertical, body);

		try
		{
			DrawScrollable(gui);
		}
		finally
		{
			gui.Layout.Pop();
		}
	}

	private void DrawScrollable(ImGui gui)
	{
		gui.BeginScrollable();

		try
		{
			DrawGamemodes(gui);
			DrawBrokenLevels(gui);
		}
		finally
		{
			gui.EndScrollable();
		}
	}

	private void DrawGamemodes(ImGui gui)
	{
		float text = gui.Style.Layout.TextSize;

		UiWidgets.Heading(gui, UiMetrics.Row(gui, 1f), "THE GAMEMODE");

		foreach (WelcomeMode mode in _modes)
		{
			DrawGamemode(gui, mode, text);
		}

		UiText.Paragraph(gui,
			"More modes are on the way. Classic is the one that exists today, and it is the one every "
			+ "other mode will be measured against.", UiText.Muted(gui));

		gui.AddSpacing();
	}

	private static void DrawGamemode(ImGui gui, WelcomeMode mode, float text)
	{
		UiText.Draw(gui, mode.Name, Color.Zeepkist.Medal.Author, UiMetrics.Row(gui, 1.3f), text * 1.15f, 0f);
		UiText.Paragraph(gui, mode.Description, UiText.Default(gui));
		gui.AddSpacing();

		foreach (string rule in mode.Rules)
		{
			UiText.Paragraph(gui, rule, UiText.Default(gui));
		}

		gui.AddSpacing();
	}

	private static void DrawBrokenLevels(ImGui gui)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 1f), "WHEN A LEVEL IS BROKEN");

		UiText.Paragraph(gui,
			"The workshop has levels that simply cannot be finished - a checkpoint that never arms, a jump "
			+ "that does not exist any more, an author time nobody including the author has ever driven. "
			+ "'Level is Broken' throws that level out, refunds every second you spent on it, and draws "
			+ "another one.", UiText.Default(gui));

		gui.AddSpacing();

		UiText.Paragraph(gui,
			"Which is exactly why it must not be used on a level that is merely hard. A run where the hard "
			+ "ones were declared broken is not a run - there is nothing left in it to be proud of, and "
			+ "nothing in it worth comparing to anybody else's. Hard levels get skipped, at the price the "
			+ "gamemode asks. Broken ones get reported.", Color.Style.Status.Alert);

		gui.AddSpacing();
	}

	private void DrawFooter(ImGui gui)
	{
		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), UiIcon.Info, "How do I use this?",
			    Color.Style.Action.Restart))
		{
			Plugin.Instance.Services.Help.Visible = true;
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.Play, "Let's hunt",
			    Color.Style.Action.Resume))
		{
			Visible = false;
		}
	}

	private static WelcomeMode[] BuildModes()
	{
		return Plugin.Instance.Services.Gamemodes.All.Select(mode => new WelcomeMode(mode)).ToArray();
	}
}
