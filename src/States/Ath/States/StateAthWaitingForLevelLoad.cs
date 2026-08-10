using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForLevelLoad(AthPreparingController phase) : AthState(phase, phase.Ath)
{
	public override void OnLevelLoaded()
	{
		if (IsRunOut())
		{
			phase.Fail();
			return;
		}

		Controller.TransitionTo(new StateAthWaitingForLevelData(phase));
	}

	/// <summary>
	///     Whether the run has nothing left to play: either the clock is up, or the playlist is
	///     a fixed one and every entry in it has been played. A random playlist never runs out,
	///     it just draws another level.
	/// </summary>
	private bool IsRunOut()
	{
		if (AthController.Ctx.IsTimeOver())
		{
			return true;
		}

		if (AthController.Ctx.Settings.RandomPlaylist)
		{
			return false;
		}

		return AthController.Ctx.Levels.Count >= ZeepkistNetwork.CurrentLobby.Playlist.Count;
	}
}
