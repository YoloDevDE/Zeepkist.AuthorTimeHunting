using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     Getting the next level ready to be driven: waiting for it to load, reading what it is,
///     and - if it turns out to be broken or one the run has already played - swapping it for
///     another and waiting again. The phase ends when a level the run accepts is on screen.
/// </summary>
public class StateAthPreparingLevel(AthController controller)
	: StateAthPhase(controller, new AthPreparingController(controller))
{
	protected override StateBase NextState()
	{
		return new StateAthPlayingLevel(AthController);
	}
}
