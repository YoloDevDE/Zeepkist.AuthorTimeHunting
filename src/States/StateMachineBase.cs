using System;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States;

/// <summary>
///     Drives a set of <see cref="StateBase" /> instances: holds the current one, runs the
///     transitions and tears the machine down on request.
///     This used to be an interface whose transition logic sat in C# 8 default interface
///     members. That worked, but an interface cannot hold fields, which forced CurrentState
///     to be publicly settable even though nothing outside ever set it.
/// </summary>
public abstract class StateMachineBase
{
	public StateBase CurrentState { get; private set; }

	/// <summary>
	///     The innermost running state: the current state of the deepest sub-machine that is
	///     active. Anything outside the machine asking "what is the run doing right now" wants
	///     this rather than <see cref="CurrentState" />, which only names the phase.
	/// </summary>
	public StateBase CurrentLeafState => Leaf(CurrentState);

	private static StateBase Leaf(StateBase state)
	{
		StateBase sub = state?.SubStateMachine?.CurrentState;

		if (sub == null)
		{
			return state;
		}

		return Leaf(sub);
	}

	public abstract StateBase InitialState { get; }

	public abstract StateBase FinalState { get; }

	public event Action StateMachineFinished;

	public void Init()
	{
		Logger.LogInfo($"StateMachine: Initializing with initial state {InitialState.GetType().Name}");
		TransitionTo(InitialState);
	}

	public void TransitionTo(StateBase nextState)
	{
		if (nextState == null)
		{
			throw new ArgumentNullException(nameof(nextState));
		}

		Logger.LogInfo(
			$"StateMachine: Transitioning from {(CurrentState == null ? "null" : CurrentState.GetType().Name)} to {nextState.GetType().Name}");

		if (CurrentState != null)
		{
			Logger.LogDebug(
				$"StateMachine: Disposing sub-state machine of {CurrentState.GetType().Name} -> {CurrentState.SubStateMachine?.GetType().Name}[{CurrentState.SubStateMachine?.CurrentState?.GetType().Name}]");
			CurrentState.SubStateMachine?.StopGracefully();

			Logger.LogDebug($"StateMachine: Exiting state {CurrentState.GetType().Name}");
			CurrentState.Exit();
		}

		CurrentState = nextState;

		Logger.LogDebug($"StateMachine: Entering state {CurrentState.GetType().Name}");
		CurrentState.Enter();

		if (CurrentState.SubStateMachine == null)
		{
			return;
		}

		Logger.LogDebug($"StateMachine: Initializing sub-state machine in {CurrentState.GetType().Name}");
		CurrentState.SubStateMachine.Init();
	}

	public virtual void Update()
	{
		if (CurrentState == null)
		{
			return;
		}

		CurrentState.Update();
		CurrentState.SubStateMachine?.Update();
	}

	public void StopGracefully()
	{
		try
		{
			Logger.LogInfo(
				$"StateMachine: Disposing. Current state: {(CurrentState == null ? "null" : CurrentState.GetType().Name)}");

			if (CurrentState == null)
			{
				Logger.LogDebug("StateMachine: No current state to exit during disposal");
				return;
			}

			TransitionToFinalState();

			Logger.LogDebug($"StateMachine: Exiting current state {CurrentState.GetType().Name} during disposal");
			CurrentState.Exit();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "StateMachine.StopGracefully");
		}
		finally
		{
			Logger.LogInfo("StateMachine: Disposal complete");
		}
	}

	private void TransitionToFinalState()
	{
		if (CurrentState == FinalState)
		{
			Logger.LogInfo(
				$"StateMachine: Already in final state ({FinalState.GetType().Name}), skipping transition");

			return;
		}

		Logger.LogInfo($"StateMachine: Transitioning to final state {FinalState.GetType().Name} during disposal");
		TransitionTo(FinalState);
	}

	public void InvokeFinish()
	{
		StateMachineFinished?.Invoke();
	}
}
