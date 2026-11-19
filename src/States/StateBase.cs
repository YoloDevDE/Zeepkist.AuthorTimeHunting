namespace AuthorTimeHunting.States;

/// <summary>
///     A single state of a <see cref="StateMachineBase" />. Every hook is virtual and does nothing
///     by default, so a state only overrides the moments it cares about.
/// </summary>
public abstract class StateBase
{
	protected StateBase(StateMachineBase controller)
	{
		Controller = controller;
	}

	public StateMachineBase Controller { get; }

	public virtual StateMachineBase SubStateMachine => null;

	public virtual void Enter()
	{
	}

	public virtual void Update()
	{
	}

	public virtual void Exit()
	{
	}
}
