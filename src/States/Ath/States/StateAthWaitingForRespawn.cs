using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     The author time is in the bag and the level is over; the respawn is what moves the hunt on.
///     Nothing is announced here. The finish panel already says what was taken and what to do
///     about it - see <see cref="UI.Hud.FinishVerdict" /> - and a toast saying the same thing in
///     the opposite corner was the player being told twice.
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
