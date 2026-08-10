using AuthorTimeHunting.States.Ath.States;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStartingController : AthPhaseController
{
	public AthStartingController(AthController ath) : base(ath)
	{
		InitialState = new StateAthWaitingForPlaylist(this);
		FinalState = new StateAthPhaseFinished(this);
	}

	public override StateBase InitialState { get; }

	public override StateBase FinalState { get; }
}
