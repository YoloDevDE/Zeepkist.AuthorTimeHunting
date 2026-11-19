using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.Util;
using Steamworks.Data;
using Steamworks.Ugc;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Downloads the lobby's next level (ZeepkistLobby.NextPlaylistIndex) while the player still drives.
///     Talks to Steam directly: WorkshopManager's tracking throws on the game's own upload of the
///     same id, and the lobby then skips the level. Never subscribes the player to anything.
/// </summary>
public class WorkshopDownloadService
{
	public static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

	private readonly Dictionary<ulong, Task<bool>> _downloads = new();

	private TickBehaviour _behaviour;
	private string _lastPrefetchedUid;

	public WorkshopDownloadService()
	{
		_behaviour = TickBehaviour.Create(nameof(WorkshopDownloadService), PrefetchNextLevel);
	}

	public void EnsureDownloaded(OnlineZeeplevel level)
	{
		_ = EnsureDownloadedAsync(level);
	}

	public Task<bool> EnsureDownloadedAsync(OnlineZeeplevel level)
	{
		if (level == null || level.WorkshopID == 0)
		{
			return Task.FromResult(true);
		}

		if (_downloads.TryGetValue(level.WorkshopID, out Task<bool> running))
		{
			return running;
		}

		Task<bool> download = DownloadAsync(level.WorkshopID, level.Name);
		_downloads[level.WorkshopID] = download;

		return download;
	}

	public async Task WaitUntilReadyAsync(OnlineZeeplevel level)
	{
		if (level == null || level.WorkshopID == 0)
		{
			return;
		}

		Task<bool> download = EnsureDownloadedAsync(level);

		using CancellationTokenSource timeout = new();

		if (await Task.WhenAny(download, Task.Delay(ReadyTimeout, timeout.Token)) == download)
		{
			timeout.Cancel();

			return;
		}

		Logger.LogWarning(
			$"WorkshopDownloadService: '{level.Name}' ({level.WorkshopID}) is still downloading after "
			+ $"{ReadyTimeout.TotalSeconds:F0}s, going ahead without it.");
	}

	public void PrefetchNextLevel()
	{
		OnlineZeeplevel next = GetNextLevel();

		if (next == null)
		{
			_lastPrefetchedUid = null;
			return;
		}

		if (next.UID == _lastPrefetchedUid)
		{
			return;
		}

		_lastPrefetchedUid = next.UID;
		Logger.LogInfo($"WorkshopDownloadService: Next up is '{next.Name}', making sure it is downloaded.");
		EnsureDownloaded(next);
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

	private static OnlineZeeplevel GetNextLevel()
	{
		ZeepkistLobby lobby = ZeepkistNetwork.CurrentLobby;

		if (lobby?.Playlist == null || lobby.Playlist.Count == 0)
		{
			return null;
		}

		int index = lobby.NextPlaylistIndex;

		if (index < 0 || index >= lobby.Playlist.Count)
		{
			return null;
		}

		return lobby.Playlist[index];
	}

	private async Task<bool> DownloadAsync(ulong workshopId, string levelName)
	{
		try
		{
			Item? item = await Item.GetAsync(new PublishedFileId { Value = workshopId });

			if (item == null)
			{
				_downloads.Remove(workshopId);
				Logger.LogWarning($"WorkshopDownloadService: Steam does not know '{levelName}' ({workshopId}).");

				return false;
			}

			if (item.Value.IsInstalled && !item.Value.NeedsUpdate)
			{
				return true;
			}

			Logger.LogInfo($"WorkshopDownloadService: Pre-downloading '{levelName}' ({workshopId}).");

			if (await item.Value.DownloadAsync())
			{
				Logger.LogInfo($"WorkshopDownloadService: '{levelName}' ({workshopId}) is ready.");

				return true;
			}

			_downloads.Remove(workshopId);
			Logger.LogWarning($"WorkshopDownloadService: Could not pre-download '{levelName}' ({workshopId}).");

			return false;
		}
		catch (Exception e)
		{
			_downloads.Remove(workshopId);
			Logger.LogError(
				$"WorkshopDownloadService: Pre-download of '{levelName}' ({workshopId}) failed: {e.Message}");

			return false;
		}
	}
}
