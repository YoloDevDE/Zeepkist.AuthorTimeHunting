using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     The author time is beaten and the level is over; the respawn moves the hunt on.
///     Nothing is announced here - <see cref="UI.Hud.FinishVerdict" /> already says it.
/// </summary>
public class StateAthWaitingForRespawn(AthPlayingController phase) : AthState(phase, phase.Ath)
{
	public override void Enter()
	{
		AthController.Ctx.CurrentLevel.Stop();
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
