using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForNextRun(AthPlayingController phase) : AthState(phase, phase.Ath)
{
	public override void Enter()
	{
		AthController.Ctx.ResetRetries();
	}

	public override void OnRoundEnded()
	{
		phase.Skip();
	}

	public override void OnRoundStarted()
	{
		Controller.TransitionTo(new StateAthWaitingForFinish(phase));
	}

	public override void OnPhotoModeEntered()
	{
		if (!GameStateObserver.IsRacing)
		{
			Logger.LogInfo(
				"StateAthWaitingForNextRun: Photo mode entered outside a running race, keeping the clock paused.");
			return;
		}

		OnRoundStarted();
	}
}
