using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Enums;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepSDK.Level;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Answers whether a race is actually running, from the moment the mod loads.
///     Polls every frame, because "the level is still loading" is a flag nothing announces.
/// </summary>
public class GameStateObserver
{
	private TickBehaviour _behaviour;
	private bool _lastIsRacing;
	private bool _lastKnownStateValid;

	private ZeepkistLobbyState? _lastLobbyState;

	public GameStateObserver()
	{
		_behaviour = TickBehaviour.Create(nameof(GameStateObserver), Tick);
	}

	public static bool IsInOnlineLobby => ZeepkistNetwork.CurrentLobby != null;

	/// <summary>Whether the lobby is ours to run - everything a hunt does to a lobby is a host power.</summary>
	public static bool IsLobbyHost => IsInOnlineLobby && ZeepkistNetwork.IsMasterClient;

	public static ZeepkistLobbyState? LobbyState
	{
		get
		{
			if (ZeepkistNetwork.CurrentLobby == null)
			{
				return null;
			}

			return (ZeepkistLobbyState)ZeepkistNetwork.CurrentLobby.GameState;
		}
	}

	public static bool IsRacing => IsInOnlineLobby && LobbyState == ZeepkistLobbyState.Racing && IsLevelReady;

	/// <summary>
	///     Whether the local player has every checkpoint. A finish without them does not count,
	///     so the level's clock keeps running. A level with no checkpoints is always satisfied.
	/// </summary>
	public static bool AllCheckpointsPassed
	{
		get
		{
			GameMaster master = PlayerManager.Instance == null ? null : PlayerManager.Instance.currentMaster;

			if (master == null || master.playerResults == null || master.playerResults.Count == 0)
			{
				return false;
			}

			return master.playerResults[0].racepoints >= master.racePoints;
		}
	}

	/// <summary>How many checkpoints the level has. The finish is not one of them.</summary>
	public static int CheckpointCount
	{
		get
		{
			GameMaster master = Master;

			return master == null ? 0 : master.racePoints;
		}
	}

	/// <summary>
	///     The checkpoints the current attempt has already taken, copied into plain numbers so
	///     everything downstream can be tested without the game.
	/// </summary>
	public static SplitSet CurrentSplits
	{
		get
		{
			List<WinCompare.SplitTime> splits = LocalSplits;

			if (splits == null)
			{
				return SplitSet.Empty;
			}

			double[] times = new double[splits.Count];
			double[] speeds = new double[splits.Count];

			for (int i = 0; i < splits.Count; i++)
			{
				times[i] = splits[i].time;
				speeds[i] = splits[i].velocity;
			}

			return new SplitSet(times, speeds);
		}
	}

	/// <summary>How long the attempt being driven has been running, or -1 when none is. The game's own clock.</summary>
	public static double RunTime
	{
		get
		{
			GameMaster master = Master;

			if (master == null || master.currentLevelPhysicsTime < 0f || master.PlayersReady == null ||
			    master.PlayersReady.Count == 0)
			{
				return -1d;
			}

			ReadyToReset player = master.PlayersReady[0];

			return player?.ticker == null ? -1d : player.ticker.GetTicker();
		}
	}

	public static bool IsLevelReady
	{
		get
		{
			if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null)
			{
				return false;
			}

			if (PlayerManager.Instance.currentMaster.loadNewLevel)
			{
				return false;
			}

			return LevelApi.CurrentLevel != null;
		}
	}

	private static GameMaster Master => PlayerManager.Instance == null ? null : PlayerManager.Instance.currentMaster;

	/// <summary>The local player's splits, or null when there is no race to read them off.</summary>
	private static List<WinCompare.SplitTime> LocalSplits
	{
		get
		{
			GameMaster master = Master;

			if (master == null || master.playerResults == null || master.playerResults.Count == 0)
			{
				return null;
			}

			return master.playerResults[0].split_times;
		}
	}

	public event Action<ZeepkistLobbyState?> LobbyStateChanged;

	public event Action BecameRacing;

	public event Action StoppedRacing;

	/// <summary>
	///     The checkpoints of the player's best run on a level, from the game's <c>Replays.json</c>.
	///     Empty where the player has never set a time here.
	/// </summary>
	public static SplitSet ReplaySplits(string levelUid)
	{
		if (string.IsNullOrEmpty(levelUid) || ReplayManager.Instance == null)
		{
			return SplitSet.Empty;
		}

		ReplayManager.ReplayInfo replay = ReplayManager.Instance.GetReplay(levelUid);

		if (replay?.Splits == null)
		{
			return SplitSet.Empty;
		}

		double[] times = new double[replay.Splits.Count];
		double[] speeds = new double[replay.Splits.Count];

		for (int i = 0; i < replay.Splits.Count; i++)
		{
			times[i] = replay.Splits[i];
			speeds[i] = Velocity(replay, i);
		}

		return new SplitSet(times, speeds);
	}

	public void Tick()
	{
		ZeepkistLobbyState? state = LobbyState;

		if (!_lastKnownStateValid || state != _lastLobbyState)
		{
			_lastKnownStateValid = true;
			_lastLobbyState = state;
			Logger.LogInfo(
				$"GameStateObserver: Lobby state is now {(state.HasValue ? state.Value.ToString() : "no lobby")}.");
			Raise(() => LobbyStateChanged?.Invoke(state), nameof(LobbyStateChanged));
		}

		bool racing = IsRacing;

		if (racing == _lastIsRacing)
		{
			return;
		}

		_lastIsRacing = racing;
		Logger.LogInfo($"GameStateObserver: {(racing ? "Race is running" : "Race is not running")}.");
		Raise(racing ? BecameRacing : StoppedRacing, racing ? nameof(BecameRacing) : nameof(StoppedRacing));
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

	/// <summary>Older replays were written without the speeds, so a missing one is an ordinary answer.</summary>
	private static double Velocity(ReplayManager.ReplayInfo replay, int index)
	{
		if (replay.velocities == null || index >= replay.velocities.Count)
		{
			return -1d;
		}

		return replay.velocities[index];
	}

	private static void Raise(Action raise, string eventName)
	{
		try
		{
			raise?.Invoke();
		}
		catch (Exception e)
		{
			Logger.LogError($"GameStateObserver: A subscriber of {eventName} threw: {e.Message}\n{e.StackTrace}");
		}
	}
}
