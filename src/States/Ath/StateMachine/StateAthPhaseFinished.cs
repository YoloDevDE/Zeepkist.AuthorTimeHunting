namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The end of a phase, and deliberately empty: <see cref="StateMachineBase.StopGracefully" />
///     also switches here on teardown, so anything done here would run during a shutdown too.
/// </summary>
public class StateAthPhaseFinished(AthPhaseController phase) : AthState(phase, phase.Ath);
