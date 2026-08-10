using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn(AthPlayingController phase) : AthState(phase, phase.Ath)
{
	public override void Enter()
	{
		AthController.Ctx.CurrentLevel.Stop();
		FrogNotification.Author("Author time claimed!<br>[Respawn to continue]");
	}

	public override void OnRoundEnded()
	{
		Controller.InvokeFinish();
	}

	public override void OnPlayerSpawned()
	{
		PlaylistService.SkipLevel();
	}
}
