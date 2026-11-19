using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using Newtonsoft.Json;
using ZeepkistNetworking;
using ZeepSDK.Playlist;
using Random = UnityEngine.Random;

namespace AuthorTimeHunting.Service;

/// <summary>The levels of the player's local playlists, scanned once and drawn from at random.</summary>
public class LocalLevelCacheService
{
	private bool _initialized;

	private int _skippedLocalLevels;

	public int LevelCount => CachedLevelItems.Count;

	private List<LevelItem> CachedLevelItems { get; } = new();

	public void EnsureInitialized()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		ScanLocalPlaylists();
	}

	public List<LevelItem> GetRandomLevelItems(int count, IEnumerable<string> excludedUids = null)
	{
		EnsureInitialized();

		if (CachedLevelItems.Count == 0)
		{
			Logger.LogError("LocalLevelCacheService: No levels in cache.");
			return new List<LevelItem>();
		}

		HashSet<string> excluded = new(StringComparer.OrdinalIgnoreCase);

		if (excludedUids != null)
		{
			excluded.UnionWith(excludedUids);
		}

		List<LevelItem> available = CachedLevelItems.Where(l => !excluded.Contains(l.FileUid)).ToList();

		if (available.Count == 0)
		{
			Logger.LogWarning("LocalLevelCacheService: All levels have been played. Playlist exhausted.");
			return new List<LevelItem>();
		}

		return available.OrderBy(_ => Random.value).Take(count).ToList();
	}

	private void ScanLocalPlaylists()
	{
		Logger.LogInfo("LocalLevelCacheService: Scanning local playlists via PlaylistApi...");

		bool apiSuccess = TryScanViaApi();

		if (!apiSuccess || CachedLevelItems.Count == 0)
		{
			Logger.LogWarning(
				"LocalLevelCacheService: PlaylistApi scan failed or empty, falling back to manual file scan.");
			TryScanViaFileSystem();
		}

		if (CachedLevelItems.Count == 0)
		{
			Logger.LogWarning("LocalLevelCacheService: No levels found in any local playlist.");
			TryNotifyWarning("ATH: No levels found in local playlists. Add playlists to use ATH.");

			return;
		}

		Logger.LogInfo(
			$"LocalLevelCacheService: Cached {CachedLevelItems.Count} unique levels, dropped {_skippedLocalLevels} without a workshop id.");
		TryNotifySuccess($"ATH: Loaded {CachedLevelItems.Count} levels from local playlists.");
	}

	private bool TryScanViaApi()
	{
		try
		{
			IReadOnlyList<PlaylistSaveJSON> playlists = PlaylistApi.GetPlaylists();

			if (playlists == null || playlists.Count == 0)
			{
				Logger.LogWarning("LocalLevelCacheService: PlaylistApi returned no playlists.");
				return false;
			}

			HashSet<string> seenUids = new(StringComparer.OrdinalIgnoreCase);

			foreach (PlaylistSaveJSON playlist in playlists)
			{
				AddPlaylist(seenUids, playlist);
			}

			Logger.LogInfo(
				$"LocalLevelCacheService: PlaylistApi scan complete, {CachedLevelItems.Count} levels from {playlists.Count} playlists.");
			return true;
		}
		catch (Exception ex)
		{
			Logger.LogError($"LocalLevelCacheService: PlaylistApi scan failed: {ex.Message}");
			return false;
		}
	}

	private void TryScanViaFileSystem()
	{
		try
		{
			string playlistDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Zeepkist", "Playlists");

			if (!Directory.Exists(playlistDir))
			{
				Logger.LogWarning($"LocalLevelCacheService: Playlist directory not found: {playlistDir}");
				return;
			}

			string[] files = Directory.GetFiles(playlistDir, "*.zeeplist", SearchOption.TopDirectoryOnly);

			if (files.Length == 0)
			{
				Logger.LogWarning($"LocalLevelCacheService: No .zeeplist files found in {playlistDir}");
				return;
			}

			HashSet<string> seenUids = new(StringComparer.OrdinalIgnoreCase);

			foreach (LevelItem existing in CachedLevelItems)
			{
				seenUids.Add(existing.FileUid);
			}

			int fileCount = 0;

			foreach (string file in files)
			{
				if (!TryAddZeeplist(seenUids, file))
				{
					continue;
				}

				fileCount++;
			}

			Logger.LogInfo(
				$"LocalLevelCacheService: File system scan complete, parsed {fileCount}/{files.Length} files, {CachedLevelItems.Count} total levels.");
		}
		catch (Exception ex)
		{
			Logger.LogError($"LocalLevelCacheService: File system scan failed: {ex.Message}");
		}
	}

	private void AddPlaylist(HashSet<string> seenUids, PlaylistSaveJSON playlist)
	{
		if (playlist?.levels == null)
		{
			return;
		}

		foreach (OnlineZeeplevel level in playlist.levels)
		{
			AddLevel(seenUids, level.UID, level.WorkshopID, level.Name, level.Author);
		}
	}

	/// <summary>Returns whether the file held a level list.</summary>
	private bool TryAddZeeplist(HashSet<string> seenUids, string file)
	{
		try
		{
			string json = File.ReadAllText(file);
			ZeeplistFile parsed = JsonConvert.DeserializeObject<ZeeplistFile>(json);

			if (parsed?.Levels == null)
			{
				return false;
			}

			foreach (ZeeplistLevel level in parsed.Levels)
			{
				ulong.TryParse(level.WorkshopID, out ulong workshopId);
				AddLevel(seenUids, level.UID, workshopId, level.Name, level.Author);
			}

			return true;
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"LocalLevelCacheService: Failed to parse {Path.GetFileName(file)}: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	///     Drops levels without a workshop id: the server cannot fetch them, and the run would
	///     loop on "restart the level" forever.
	/// </summary>
	private void AddLevel(HashSet<string> seenUids, string uid, ulong workshopId, string name, string author)
	{
		if (string.IsNullOrEmpty(uid))
		{
			return;
		}

		if (workshopId == 0)
		{
			_skippedLocalLevels++;
			Logger.LogDebug($"LocalLevelCacheService: Dropped '{name}' (UID {uid}) - no workshop id.");
			return;
		}

		if (!seenUids.Add(uid))
		{
			return;
		}

		CachedLevelItems.Add(new LevelItem { FileUid = uid, WorkshopId = workshopId, Name = name, FileAuthor = author });
	}

	private static void TryNotifySuccess(string message)
	{
		try
		{
			FrogNotification.Success(message);
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"LocalLevelCacheService: Could not send success notification: {ex.Message}");
		}
	}

	private static void TryNotifyWarning(string message)
	{
		try
		{
			FrogNotification.Warn(message);
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"LocalLevelCacheService: Could not send warning notification: {ex.Message}");
		}
	}
}
