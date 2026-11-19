using System;
using AuthorTimeHunting.Service;
using BepInEx.Configuration;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>ATH's dropdown in the game's top bar: opens the menu and flips every window and on/off setting.</summary>
public class AthToolbar : IZeepToolbarDrawer
{
	private readonly ModServices _services;

	public AthToolbar(ModServices services)
	{
		_services = services;
	}

	public string MenuTitle => "ATH";

	public void DrawMenuItems(ImGui gui)
	{
		try
		{
			DrawItems(gui);
		}
		catch (Exception e)
		{
			Logger.LogError($"AthToolbar: Draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	private void DrawItems(ImGui gui)
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		_services.Menu.Visible = Item(gui, "Main Menu", _services.Menu.Visible);
		_services.RunOverlay.Visible = Item(gui, "Run Bar", _services.RunOverlay.Visible);
		_services.Leaderboard.Visible = Item(gui, "Leaderboard", _services.Leaderboard.Visible);
		_services.Splits.Visible = Item(gui, "Splits Window", _services.Splits.Visible);
		_services.Status.Visible = Item(gui, "Status", _services.Status.Visible);
		_services.Welcome.Visible = Item(gui, "Welcome", _services.Welcome.Visible);
		_services.Help.Visible = Item(gui, "Help", _services.Help.Visible);

		Setting(gui, "Random Levels (RTM)", config.RandomPlaylist);
		Setting(gui, "Start Lights", config.StartLights);
		Setting(gui, "Checkpoint Splits", config.Splits);
		Setting(gui, "Medals in the Leaderboard", config.LeaderboardMedals);
		Setting(gui, "Save Playlist on Run End", config.SavePlaylistOnRunEnd);
	}

	private static void Setting(ImGui gui, string label, ConfigEntry<bool> entry)
	{
		entry.Value = Item(gui, label, entry.Value);
	}

	/// <returns>The flipped state when the item was clicked, the given one otherwise.</returns>
	private static bool Item(ImGui gui, string label, bool on)
	{
		string mark = on ? "[x] " : "[  ] ";

		if (gui.Menu((mark + label).AsSpan()))
		{
			return !on;
		}

		return on;
	}
}
