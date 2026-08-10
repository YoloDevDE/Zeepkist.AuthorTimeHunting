using AuthorTimeHunting.States.Ath.States;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthPreparingController : AthPhaseController
{
	public AthPreparingController(AthController ath) : base(ath)
	{
		InitialState = new StateAthWaitingForLevelLoad(this);
		FinalState = new StateAthPhaseFinished(this);
	}

	public override StateBase InitialState { get; }

	public override StateBase FinalState { get; }
}
