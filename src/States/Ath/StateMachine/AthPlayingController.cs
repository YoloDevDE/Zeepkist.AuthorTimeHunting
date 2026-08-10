using AuthorTimeHunting.States.Ath.States;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     Playing a level is the one phase with two good endings: the level was driven to its
///     author time and the run walks away with a medal, or it was given up on and the run
///     walks away with a skip. <see cref="WasSkipped" /> is which of the two happened.
/// </summary>
public class AthPlayingController : AthPhaseController
{
	public AthPlayingController(AthController ath) : base(ath)
	{
		InitialState = new StateAthWaitingForFirstRun(this);
		FinalState = new StateAthPhaseFinished(this);
	}

	public bool WasSkipped { get; private set; }

	public void Skip()
	{
		WasSkipped = true;
		InvokeFinish();
	}

	public override StateBase InitialState { get; }

	public override StateBase FinalState { get; }
}
