using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     The level is being driven. Everything the player does to it - the first run, every
///     attempt after it, the pause between two attempts, the respawn that ends a level once
///     the author time is beaten - happens in <see cref="AthPlayingController" /> underneath.
/// </summary>
public class StateAthPlayingLevel(AthController controller)
	: StateAthPhase(controller, new AthPlayingController(controller))
{
	private AthPlayingController Playing => (AthPlayingController)Phase;

	protected override StateBase NextState()
	{
		if (Playing.WasSkipped)
		{
			return new StateAthSkippingLevel(AthController);
		}

		return new StateAthLevelSummary(AthController);
	}
}
