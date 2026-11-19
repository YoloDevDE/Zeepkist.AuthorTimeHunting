namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The sub-machine of one phase of a run. It walks its waits and finishes; the parent state
///     decides where the run goes, reading <see cref="HasFailed" /> when the phase could not go on.
/// </summary>
public abstract class AthPhaseController : StateMachineBase
{
	protected AthPhaseController(AthController ath)
	{
		Ath = ath;
	}

	public AthController Ath { get; }

	public bool HasFailed { get; private set; }

	/// <summary>
	///     Ends the phase on the note that it could not go on. Failing and finishing are one call,
	///     so the flag can never be raised without the run moving on.
	/// </summary>
	public void Fail()
	{
		HasFailed = true;
		InvokeFinish();
	}
}
