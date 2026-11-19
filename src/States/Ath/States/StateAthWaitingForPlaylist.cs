using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     Waits until the server has the playlist the run is supposed to play. A fixed playlist
///     only has to be pushed; a random one has to be drawn first and is retried a few times
///     before the phase gives up.
/// </summary>
public class StateAthWaitingForPlaylist(AthStartingController phase) : AthState(phase, phase.Ath)
{
	private const int _roundTimeSeconds = 86400;
	private const int _pushDelay = 2500;
	private const int _settleDelay = 500;
	private const int _retryDelay = 500;
	private const int _playlistAttempts = 3;

	private CancellationTokenSource _cts;

	public override async void Enter()
	{
		_cts = new CancellationTokenSource();

		try
		{
			await PushPlaylist(_cts.Token);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Logger.LogError($"Enter failed: {ex.Message}\nStack trace: {ex.StackTrace}");
			FrogNotification.Error("Something went wrong while starting the hunt");
			phase.Fail();
		}
	}

	public override void Exit()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private async Task PushPlaylist(CancellationToken stopped)
	{
		ZeepkistNetwork.CurrentLobby.RoundTime = _roundTimeSeconds;

		if (!AthController.Ctx.Settings.RandomPlaylist)
		{
			await Task.Delay(_pushDelay, stopped);
			MultiplayerApi.UpdateServerPlaylist();
			await Task.Delay(_settleDelay, stopped);
			Controller.InvokeFinish();
			return;
		}

		OnlineZeeplevel firstLevel = await DrawFirstLevel(stopped);

		if (firstLevel == null)
		{
			FrogNotification.Error("Failed to start playlist after multiple attempts");
			phase.Fail();
			return;
		}

		Controller.TransitionTo(new StateAthWaitingForFirstLevel(phase, firstLevel));
	}

	private async Task<OnlineZeeplevel> DrawFirstLevel(CancellationToken stopped)
	{
		for (int attempt = 0; attempt < _playlistAttempts; attempt++)
		{
			OnlineZeeplevel level = await TryStartPlaylist(stopped);

			if (level != null)
			{
				return level;
			}
		}

		return null;
	}

	private async Task<OnlineZeeplevel> TryStartPlaylist(CancellationToken stopped)
	{
		try
		{
			OnlineZeeplevel level = await RandomLevels.DrawRandomLevelAsync();
			await PlaylistService.StartNewPlaylist(level);
			return level;
		}
		catch (Exception ex)
		{
			Logger.LogError($"Failed to start playlist: {ex.Message}");
			await Task.Delay(_retryDelay, stopped);
			return null;
		}
	}
}
