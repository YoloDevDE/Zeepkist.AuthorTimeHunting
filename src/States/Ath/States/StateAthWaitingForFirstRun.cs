using System;
using System.Threading.Tasks;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using ZeepSDK.Level;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForFirstRun(AthPlayingController phase) : AthState(phase, phase.Ath)
{
	public override void Enter()
	{
		try
		{
			AthController.Ctx.InitializingNewLevel(LevelApi.CurrentLevel);
		}
		catch (Exception e)
		{
			Logger.LogError(
				$"StateAthWaitingForFirstRun: Failed to start level: {e.Message}\nStack trace: {e.StackTrace}");
			phase.Fail();
		}
	}

	private async Task AddLevelAsync()
	{
		if (!AthController.Ctx.Settings.RandomPlaylist)
		{
			return;
		}

		try
		{
			Logger.LogInfo("Adding Random Level");
			OnlineZeeplevel level = await RandomLevels.DrawRandomLevelAsync();

			await PlaylistService.AddLevelToCurrentPlaylist(level);
		}
		catch (Exception e)
		{
			Logger.LogError($"StateAthWaitingForFirstRun: Could not pre-load the next level: {e.Message}");
			FrogNotification.Warn("Could not load the next level - the playlist may run out");
		}
	}

	public override void OnRoundStarted()
	{
		_ = AddLevelAsync();
		Controller.TransitionTo(new StateAthWaitingForFinish(phase));
	}
}
