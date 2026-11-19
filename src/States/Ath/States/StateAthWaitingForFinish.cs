using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     The level is being driven; the clock runs here and nowhere else. Only a finish with every
///     checkpoint passed ends the state - anything short of that keeps the clock running.
/// </summary>
public class StateAthWaitingForFinish(AthPlayingController phase) : AthState(phase, phase.Ath)
{
	public override void Enter()
	{
		BeginAttempt();

		if (!AthController.Ctx.IsPaused)
		{
			AthController.Ctx.CurrentLevel.ResumeTiming();
		}

		Update();
	}

	public override void Exit()
	{
		AthController.Ctx.CurrentLevel.PauseTiming();
	}

	public override void OnRoundEnded()
	{
		phase.Skip();
	}

	public override void OnCrossedFinishLine(float time)
	{
		if (!GameStateObserver.AllCheckpointsPassed)
		{
			Logger.LogInfo(
				"StateAthWaitingForFinish: Finish crossed without every checkpoint, the level keeps running.");
			FrogNotification.Warn("Checkpoint missed - that run does not count");

			return;
		}

		ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
		PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
		Level currentLevel = AthController.Ctx.CurrentLevel;

		if (currentResult == null)
		{
			Controller.TransitionTo(new StateAthWaitingForNextRun(phase));
			return;
		}

		LevelStatus previousStatus = currentLevel.Status;

		currentLevel.RecordRun(currentResult.Time);
		LevelStatus runMedalStatus = currentLevel.MedalFor(currentResult.Time);

		AthController.Ctx.LastRunTime = currentResult.Time;
		AthController.Ctx.LastRunMedalStatus = runMedalStatus;
		AthController.Ctx.LastRunMedalWasNew = GetMedalRank(runMedalStatus) > GetMedalRank(previousStatus);

		if (currentLevel.Status == LevelStatus.Author)
		{
			Controller.TransitionTo(new StateAthWaitingForRespawn(phase));
			return;
		}

		Controller.TransitionTo(new StateAthWaitingForNextRun(phase));
	}

	public override void OnRoundStarted()
	{
		BeginAttempt();
		Update();
	}

	public override void Update()
	{
		AthController.Ctx.CurrentLevel.TrackSplits(GameStateObserver.CurrentSplits);

		if (AthController.Ctx.IsTimeOver())
		{
			phase.Fail();
			return;
		}

		if (AthController.Ctx.CheckAndNotifyTimeRunningLow())
		{
			FrogNotification.Info("<b>Time is running low!</b><br>A 'Penalty-Skip' will end the run!", 10f);
		}
	}

	/// <summary>
	///     Seals the last attempt's splits into the level's reference before the game's split list
	///     starts belonging to the new attempt.
	/// </summary>
	private void BeginAttempt()
	{
		AthController.Ctx.CurrentLevel.SealAttempt();
		AthController.Ctx.CurrentLevel.Attempt++;
	}

	private static int GetMedalRank(LevelStatus status)
	{
		return status switch
		{
			LevelStatus.Author => 2,
			LevelStatus.Gold => 1,
			_ => 0
		};
	}
}
