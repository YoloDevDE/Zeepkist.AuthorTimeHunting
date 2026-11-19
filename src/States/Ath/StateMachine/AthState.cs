using AuthorTimeHunting.Service;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     A state of the run with one overridable method per game event, forwarded by
///     <see cref="StateMachine.AthController" />. By default an event is handed down to the
///     current sub-state, if any.
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
