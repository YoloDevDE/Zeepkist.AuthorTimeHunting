namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The end of a phase, and deliberately empty. <see cref="StateMachineBase.StopGracefully" />
///     switches here when the run is torn down mid-phase, so anything this state did would run
///     during a shutdown as well. The waits announce their own end instead, which leaves this
///     one silent and the abort quiet.
/// </summary>
public class StateAthPhaseFinished(AthPhaseController phase) : AthState(phase, phase.Ath);
