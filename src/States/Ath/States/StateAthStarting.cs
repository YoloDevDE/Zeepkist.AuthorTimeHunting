using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     The run's first state. Getting a hunt started is two waits in a row - one on the server
///     for the playlist, one on the workshop for the first level - and those live in
///     <see cref="AthStartingController" /> underneath.
/// </summary>
public class StateAthStarting(AthController controller)
	: StateAthPhase(controller, new AthStartingController(controller))
{
	public override void OnRoundEnded()
	{
		Controller.TransitionTo(new StateAthPreparingLevel(AthController));
	}

	protected override StateBase NextState()
	{
		return new StateAthPreparingLevel(AthController);
	}
}
