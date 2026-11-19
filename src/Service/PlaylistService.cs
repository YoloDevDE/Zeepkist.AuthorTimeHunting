using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.Service;

/// <summary>Edits the lobby playlist and pushes it to the server within its 5 second rate limit.</summary>
public class PlaylistService
{
	private static readonly TimeSpan _minUpdateInterval = TimeSpan.FromSeconds(5);
	private readonly object _queueLock = new();

	private readonly Queue<Action> _updateQueue = new();

	private readonly WorkshopDownloadService _workshopDownloads;
	private DateTime _lastUpdate = DateTime.MinValue;
	private Task _processingTask;

	public PlaylistService(WorkshopDownloadService workshopDownloads)
	{
		_workshopDownloads = workshopDownloads;
	}

	private static List<OnlineZeeplevel> CurrentLobbyPlaylist => ZeepkistNetwork.CurrentLobby.Playlist;
	private static int CurrentPlaylistIndex => ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;

	/// <summary>
	///     The returned task completes once the playlist has actually reached the server, up to
	///     the rate limit later. A skip sent before that lands on the old playlist.
	/// </summary>
	public Task StartNewPlaylist(OnlineZeeplevel initialLevel)
	{
		Logger.LogInfo(
			$"PlaylistService: Starting a new playlist from index {CurrentPlaylistIndex} of {CurrentLobbyPlaylist.Count} entries with {Describe(initialLevel)}.");
		WarnIfUnplayable(initialLevel);

		CurrentLobbyPlaylist.Clear();
		CurrentLobbyPlaylist.Add(initialLevel);

		ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex = 0;

		_workshopDownloads.EnsureDownloaded(initialLevel);

		return QueueServerPlaylistUpdate();
	}

	/// <summary>The returned task carries the same promise as <see cref="StartNewPlaylist" />'s.</summary>
	public Task AddLevelToCurrentPlaylist(OnlineZeeplevel level)
	{
		WarnIfUnplayable(level);
		PlaylistItem playlistItem = new(level.UID, level.WorkshopID, level.Name, level.Author);
		MultiplayerApi.AddLevelToPlaylist(playlistItem, true);
		Logger.LogInfo(
			$"PlaylistService: Added {Describe(level)} to the playlist. Playlist count is now {CurrentLobbyPlaylist.Count}, current index {CurrentPlaylistIndex}.");
		_workshopDownloads.EnsureDownloaded(level);

		return QueueServerPlaylistUpdate();
	}

	/// <summary>The returned task carries the same promise as <see cref="StartNewPlaylist" />'s.</summary>
	public Task ReplaceLevelInCurrentPlaylist(OnlineZeeplevel oldLevel, OnlineZeeplevel newLevel)
	{
		int index = CurrentLobbyPlaylist.FindIndex(old => old.UID == oldLevel.UID);

		if (index == -1)
		{
			Logger.LogWarning($"PlaylistService: Could not find level with UID {oldLevel.UID} in playlist");

			return Task.CompletedTask;
		}

		WarnIfUnplayable(newLevel);
		CurrentLobbyPlaylist[index] = newLevel;
		Logger.LogInfo(
			$"PlaylistService: Replaced '{oldLevel.Name}' with {Describe(newLevel)} at index {index}, current index {CurrentPlaylistIndex}.");
		_workshopDownloads.EnsureDownloaded(newLevel);

		return QueueServerPlaylistUpdate();
	}

	public void SkipLevel()
	{
		SkipCommand();
	}

	public void SkipToNextLevel()
	{
		SkipCommand("next");
	}

	public void SkipToPrevLevel()
	{
		SkipCommand("prev");
	}

	public void SkipToLastLevel()
	{
		SkipCommand(CurrentLobbyPlaylist.Count - 1);
	}

	public void SkipToFirstLevel()
	{
		SkipCommand(0);
	}

	public void RestartCurrentLevel()
	{
		SkipCommand("restart");
	}

	private static string Describe(OnlineZeeplevel level)
	{
		if (level == null)
		{
			return "no level at all";
		}

		return $"'{level.Name}' by {level.Author} (UID {level.UID}, workshop {level.WorkshopID})";
	}

	/// <summary>A level with no workshop id cannot be served, and the lobby skips it on the spot.</summary>
	private static void WarnIfUnplayable(OnlineZeeplevel level)
	{
		if (level != null && level.WorkshopID != 0)
		{
			return;
		}

		Logger.LogError(
			$"PlaylistService: {Describe(level)} has no workshop id - the server cannot serve it and the lobby will skip it.");
	}

	private Task QueueServerPlaylistUpdate()
	{
		return Enqueue(MultiplayerApi.UpdateServerPlaylist);
	}

	private Task Enqueue(Action update)
	{
		lock (_queueLock)
		{
			_updateQueue.Enqueue(update);
			Logger.LogInfo(
				$"PlaylistService: Enqueued server playlist update. Queue length is now {_updateQueue.Count}.");

			if (_processingTask != null && !_processingTask.IsCompleted)
			{
				Logger.LogInfo("PlaylistService: Queue processing task already running, update appended.");

				return _processingTask;
			}

			Logger.LogInfo("PlaylistService: Starting queue processing task.");
			_processingTask = ProcessQueueAsync();

			return _processingTask;
		}
	}

	private async Task ProcessQueueAsync()
	{
		await Task.Yield();

		Logger.LogInfo("PlaylistService: Queue processing loop started.");

		while (true)
		{
			Action update;

			lock (_queueLock)
			{
				if (_updateQueue.Count == 0)
				{
					Logger.LogInfo("PlaylistService: Queue empty, stopping processing loop.");
					_processingTask = null;
					return;
				}

				update = _updateQueue.Dequeue();
				Logger.LogInfo(
					$"PlaylistService: Dequeued update. {_updateQueue.Count} update(s) remaining in queue.");
			}

			TimeSpan sinceLastUpdate = DateTime.UtcNow - _lastUpdate;

			if (sinceLastUpdate < _minUpdateInterval)
			{
				TimeSpan waitTime = _minUpdateInterval - sinceLastUpdate;
				Logger.LogInfo(
					$"PlaylistService: Rate limit active, waiting {waitTime.TotalSeconds:F1}s before applying update.");
				await Task.Delay(waitTime);
			}

			try
			{
				Logger.LogInfo("PlaylistService: Applying server playlist update now.");
				update();
				Logger.LogInfo("PlaylistService: Server playlist update applied successfully.");
			}
			catch (Exception ex)
			{
				Logger.LogError($"PlaylistService: Server playlist update failed: {ex.Message}");
			}

			_lastUpdate = DateTime.UtcNow;
		}
	}

	private static void SkipCommand(int command)
	{
		Send($"/fs {command}");
	}

	private static void SkipCommand(string command = null)
	{
		if (command == null)
		{
			Send("/fs");
			return;
		}

		Send($"/fs {command}");
	}

	private static void Send(string command)
	{
		Logger.LogInfo(
			$"PlaylistService: Sending '{command}' at index {CurrentPlaylistIndex} of {CurrentLobbyPlaylist.Count} entries.");
		ChatApi.SendMessage(command);
	}
}
