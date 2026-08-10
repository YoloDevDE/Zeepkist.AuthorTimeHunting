namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The sub-machine of one phase of a run. It walks the waits that phase is made of and then
///     finishes; the phase's parent state listens for that and moves the run on.
///     A sub-state cannot transition into the outer machine, so a phase that cannot go on says
///     so through <see cref="HasFailed" /> and finishes like any other. Where the run goes from
///     there is the parent's decision, and only the parent's.
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
	///     Ends the phase on the note that it could not go on. Failing and finishing are one
	///     call because a phase that raises the flag without finishing leaves the run standing
	///     still, and nothing outside would ever notice.
	/// </summary>
	public void Fail()
	{
		HasFailed = true;
		InvokeFinish();
	}
}
