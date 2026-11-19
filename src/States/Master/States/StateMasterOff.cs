using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOff : StateBase
{
	private IGamemode _pendingGamemode;

	public StateMasterOff(AthMasterController controller, IGamemode pendingGamemode = null) : base(controller)
	{
		_pendingGamemode = pendingGamemode;
	}

	private AthMasterController AthMaster => (AthMasterController)Controller;

	public override void Enter()
	{
		AthRequests.StopRequested += StopChallenge;
		AthRequests.StartRequested += StartChallenge;
		AthRequests.RestartRequested += StartChallenge;
		AthMaster.Services.GameState.BecameRacing += OnBecameRacing;

		StartPendingRun();
	}

	public override void Exit()
	{
		AthRequests.StopRequested -= StopChallenge;
		AthRequests.StartRequested -= StartChallenge;
		AthRequests.RestartRequested -= StartChallenge;
		AthMaster.Services.GameState.BecameRacing -= OnBecameRacing;
		_pendingGamemode = null;
	}

	/// <summary>
	///     A pending run checks the race state itself, because BecameRacing already fired while the
	///     lobby was built. It waits a frame so it does not transition from inside Enter.
	/// </summary>
	private async void StartPendingRun()
	{
		if (_pendingGamemode == null)
		{
			return;
		}

		await Task.Yield();

		if (_pendingGamemode == null || !GameStateObserver.IsRacing)
		{
			return;
		}

		try
		{
			OnBecameRacing();
		}
		catch (Exception e)
		{
			Logger.LogError($"StateMasterOff: Could not start the pending run: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	///     Every start builds the hunt its own lobby, even for a player already hosting one - a
	///     lobby ATH opened itself is the only one it knows the state of.
	/// </summary>
	private void StartChallenge()
	{
		Controller.TransitionTo(new StateMasterConnectingToServer(AthMaster, AthMaster.Services.Gamemodes.Selected));
	}

	private void OnBecameRacing()
	{
		if (_pendingGamemode == null)
		{
			return;
		}

		IGamemode gamemode = _pendingGamemode;
		_pendingGamemode = null;
		Logger.LogInfo($"StateMasterOff: Race is running, starting the pending {gamemode.Id} run.");
		BeginRun(gamemode);
	}

	private void BeginRun(IGamemode gamemode)
	{
		if (!IsHudReady())
		{
			return;
		}

		Controller.TransitionTo(new StateMasterOn(AthMaster, gamemode));
	}

	private bool IsHudReady()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			FrogNotification.Warn("Online HUD not ready yet, try again in a moment");
			return false;
		}

		return true;
	}

	private void StopChallenge()
	{
		if (_pendingGamemode != null)
		{
			_pendingGamemode = null;
			FrogNotification.Info("Pending start cancelled");
			return;
		}

		FrogNotification.Warn("ATH is not running");
	}
}
