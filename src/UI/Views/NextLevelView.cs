using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     The level the lobby is about to load, as much of it as is known before it loads.
///     The times come from the run's level pool; a time nobody knows yet is a dash.
/// </summary>
public class NextLevelView
{
	private const string _unknown = "-";

	public string LevelUid { get; private set; }

	public string Name { get; private set; }

	public string Author { get; private set; }

	public string AuthorTime { get; private set; }

	public string GoldTime { get; private set; }

	public static NextLevelView From(AthController run)
	{
		OnlineZeeplevel queued = Queued();

		if (queued == null || string.IsNullOrEmpty(queued.UID))
		{
			return null;
		}

		LevelItem known = run?.RandomLevels.Drawn(queued.UID);

		return new NextLevelView
		{
			LevelUid = queued.UID,
			Name = queued.Name,
			Author = queued.Author,
			AuthorTime = Formatted(known?.ValidationTimeAuthor),
			GoldTime = Formatted(known?.ValidationTimeGold)
		};
	}

	private static OnlineZeeplevel Queued()
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

	private static string Formatted(float? seconds)
	{
		if (seconds == null || seconds <= 0f)
		{
			return _unknown;
		}

		return TimeFormatter.FormatTime(seconds.Value);
	}
}
