using AuthorTimeHunting.Service;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     A state of the run. On top of the lifecycle hooks it inherits, it gets one overridable
///     method per game event ATH listens to.
///     None of these are abstract: a state overrides the handful of moments it cares about
///     and stays silent about the rest. The events themselves are subscribed once, centrally,
///     by <see cref="StateMachine.AthController" /> and forwarded to whichever state is
///     current - a state never registers or removes a handler and therefore cannot leak one.
///     A state that has a sub-machine hands every event it does not claim itself down to the
///     state currently running in there, so a composite does not have to repeat what its
///     children already listen for. A leaf has no sub-machine, so for it the default is what
///     it always was: nothing.
/// </summary>
public abstract class AthState : StateBase
{
	protected AthState(StateMachineBase machine, AthController ath) : base(machine)
	{
		AthController = ath;
	}

	protected AthState(AthController controller) : this(controller, controller)
	{
	}

	public AthController AthController { get; }

	public PlaylistService PlaylistService => AthController.Services.Playlist;

	public RandomLevelService RandomLevels => AthController.RandomLevels;

	private AthState SubState => SubStateMachine?.CurrentState as AthState;

	public virtual void OnRoundStarted()
	{
		SubState?.OnRoundStarted();
	}

	public virtual void OnRoundEnded()
	{
		SubState?.OnRoundEnded();
	}

	public virtual void OnPlayerSpawned()
	{
		SubState?.OnPlayerSpawned();
	}

	public virtual void OnCrossedFinishLine(float time)
	{
		SubState?.OnCrossedFinishLine(time);
	}

	public virtual void OnLevelLoaded()
	{
		SubState?.OnLevelLoaded();
	}

	public virtual void OnPhotoModeEntered()
	{
		SubState?.OnPhotoModeEntered();
	}
}
