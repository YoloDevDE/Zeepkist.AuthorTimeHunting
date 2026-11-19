using AuthorTimeHunting.Util;
using System;
using System.Collections.Generic;
using AuthorTimeHunting.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Logs every key pressed, every screen entered and every change to the lobby's playlist,
///     so a bug report is a timeline rather than a memory.
/// </summary>
public class TraceService
{
	private static readonly KeyCode[] _keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	private TickBehaviour _behaviour;
	private int _lastCount = int.MinValue;
	private bool _lastInGame;

	private int _lastIndex = int.MinValue;
	private ZeepkistLobbyState? _lastLobbyState;
	private string _lastSceneName = "";
	private string _lastUid = "";

	public TraceService()
	{
		_behaviour = TickBehaviour.Create(nameof(TraceService), Tick);

		Logger.LogInfo("TraceService: Tracing keys, screens and the lobby playlist.");
	}

	public void Tick()
	{
		TraceScreen();
		TraceKeys();
		TracePlaylist();
	}

	public void Dispose()
	{
		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}

	/// <summary>Compares the parts rather than the sentence, so no strings are built per frame.</summary>
	private void TraceScreen()
	{
		string scene = SceneManager.GetActiveScene().name;
		bool inGame = PlayerManager.Instance != null && PlayerManager.Instance.currentMaster != null;
		ZeepkistLobbyState? lobby = LobbyState();

		if (scene == _lastSceneName && inGame == _lastInGame && lobby == _lastLobbyState)
		{
			return;
		}

		_lastSceneName = scene;
		_lastInGame = inGame;
		_lastLobbyState = lobby;

		Logger.LogInfo($"Trace: Screen is now {ScreenText(scene, inGame, lobby)}.");
	}

	private static ZeepkistLobbyState? LobbyState()
	{
		if (ZeepkistNetwork.CurrentLobby == null)
		{
			return null;
		}

		return (ZeepkistLobbyState)ZeepkistNetwork.CurrentLobby.GameState;
	}

	private static string Screen()
	{
		return ScreenText(SceneManager.GetActiveScene().name,
			PlayerManager.Instance != null && PlayerManager.Instance.currentMaster != null, LobbyState());
	}

	private static string ScreenText(string scene, bool inGame, ZeepkistLobbyState? lobby)
	{
		return $"'{scene}' [{(inGame ? "gameplay" : "menu")}, {(lobby == null ? "offline" : $"lobby in {lobby}")}]";
	}

	private static void TraceKeys()
	{
		if (!Input.anyKeyDown)
		{
			return;
		}

		try
		{
			LogPressedKeys();
		}
		catch (Exception e)
		{
			Logger.LogWarning($"TraceService: Could not read the keyboard: {e.Message}");
		}
	}

	private static void LogPressedKeys()
	{
		foreach (KeyCode key in _keys)
		{
			if (!Input.GetKeyDown(key))
			{
				continue;
			}

			Logger.LogInfo($"Trace: Key {key} pressed on screen {Screen()}.");
		}
	}

	private void TracePlaylist()
	{
		if (ZeepkistNetwork.CurrentLobby == null)
		{
			return;
		}

		List<OnlineZeeplevel> playlist = ZeepkistNetwork.CurrentLobby.Playlist;
		int index = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
		int count = playlist?.Count ?? 0;
		string uid = Uid(playlist, index);

		if (index == _lastIndex && count == _lastCount && uid == _lastUid)
		{
			return;
		}

		_lastIndex = index;
		_lastCount = count;
		_lastUid = uid;

		Logger.LogInfo($"Trace: Playlist is now {Describe(playlist, index, count)}.");
	}

	private static string Describe(List<OnlineZeeplevel> playlist, int index, int count)
	{
		if (playlist == null || index < 0 || index >= count)
		{
			return $"at index {index} of {count} entries - OUT OF RANGE";
		}

		OnlineZeeplevel level = playlist[index];

		return
			$"at index {index} of {count}: '{level.Name}' by {level.Author} (UID {level.UID}, workshop {level.WorkshopID})";
	}

	private static string Uid(List<OnlineZeeplevel> playlist, int index)
	{
		if (playlist == null || index < 0 || index >= playlist.Count)
		{
			return "";
		}

		return playlist[index].UID ?? "";
	}
}
