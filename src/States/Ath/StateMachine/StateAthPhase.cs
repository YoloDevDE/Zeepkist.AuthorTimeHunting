using AuthorTimeHunting.States.Ath.States;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     A state of the run that is made of smaller waits. It does no work of its own - it holds
///     the sub-machine those waits live in and decides where the run goes once they are through.
/// </summary>
public abstract class StateAthPhase : AthState
{
	protected StateAthPhase(AthController controller, AthPhaseController phase) : base(controller)
	{
		Phase = phase;
	}

	public AthPhaseController Phase { get; }

	public override StateMachineBase SubStateMachine => Phase;

	public override void Enter()
	{
		Phase.StateMachineFinished += OnPhaseFinished;
	}

	public override void Exit()
	{
		Phase.StateMachineFinished -= OnPhaseFinished;
	}

	protected abstract StateBase NextState();

	private void OnPhaseFinished()
	{
		if (Phase.HasFailed)
		{
			Controller.TransitionTo(new StateAthStopping(AthController));
			return;
		}

		Controller.TransitionTo(NextState());
	}
}
