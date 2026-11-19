using AuthorTimeHunting.Util;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.States;
using UnityEngine;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The run's state machine. Its frame loop lives in a <see cref="TickBehaviour" />; it subscribes
///     to the game's events once and forwards them, so states can never leak a handler.
/// </summary>
public class AthController : StateMachineBase
{
	private TickBehaviour _behaviour;
	private bool _eventsSubscribed;

	public AthController(ModServices services, IGamemode gamemode)
	{
		Services = services;
		Gamemode = gamemode;
		Ctx = new AthCtx(gamemode.CreateSettings());
		RandomLevels = services.CreateRandomLevelService();
		InitialState = new StateAthStarting(this);
		FinalState = new StateAthStopping(this);

		_behaviour = TickBehaviour.Create(nameof(AthController), TickWhileRunning);
	}

	public AthCtx Ctx { get; }

	public bool IsTimerRunning { get; private set; }

	public IGamemode Gamemode { get; }

	public ModServices Services { get; }

	public RandomLevelService RandomLevels { get; }

	public override StateBase InitialState { get; }
	public override StateBase FinalState { get; }

	public bool IsBetweenAttempts =>
		Ctx.CurrentLevel != null && CurrentLeafState is StateAthWaitingForNextRun or StateAthWaitingForRespawn;

	private AthState Current => CurrentState as AthState;

	public void PauseRun()
	{
		if (Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = true;
		Ctx.CurrentLevel?.PauseTiming();
		Logger.LogInfo("AthStateMachine: Run paused.");
	}

	public void ResumeRun()
	{
		if (!Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = false;

		if (CurrentLeafState is StateAthWaitingForFinish)
		{
			Ctx.CurrentLevel?.ResumeTiming();
		}

		Logger.LogInfo("AthStateMachine: Run resumed.");
	}

	public void StartTimer()
	{
		if (IsTimerRunning)
		{
			return;
		}

		SubscribeEvents();
		IsTimerRunning = true;
	}

	public void StopTimer()
	{
		if (!IsTimerRunning)
		{
			return;
		}

		UnsubscribeEvents();
		IsTimerRunning = false;
	}

	public void Dispose()
	{
		StopTimer();

		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}

	private void TickWhileRunning()
	{
		if (!IsTimerRunning)
		{
			return;
		}

		Update();
	}

	private void OnRoundStarted()
	{
		Current?.OnRoundStarted();
	}

	private void OnRoundEnded()
	{
		Current?.OnRoundEnded();
	}

	private void OnPlayerSpawned()
	{
		Current?.OnPlayerSpawned();
	}

	private void OnCrossedFinishLine(float time)
	{
		Current?.OnCrossedFinishLine(time);
	}

	private void OnLevelLoaded()
	{
		Current?.OnLevelLoaded();
	}

	private void OnPhotoModeEntered()
	{
		Current?.OnPhotoModeEntered();
	}

	private void OnCrashed(CrashReason reason)
	{
		Ctx.CurrentLevel?.RegisterCrash();
	}

	private void OnWheelBroken()
	{
		Ctx.CurrentLevel?.RegisterWheelLost();
	}

	private void SubscribeEvents()
	{
		if (_eventsSubscribed)
		{
			return;
		}

		RacingApi.RoundStarted += OnRoundStarted;
		RacingApi.RoundEnded += OnRoundEnded;
		RacingApi.PlayerSpawned += OnPlayerSpawned;
		RacingApi.CrossedFinishLine += OnCrossedFinishLine;
		RacingApi.LevelLoaded += OnLevelLoaded;
		RacingApi.Crashed += OnCrashed;
		RacingApi.WheelBroken += OnWheelBroken;
		PhotoModeApi.PhotoModeEntered += OnPhotoModeEntered;

		_eventsSubscribed = true;
	}

	private void UnsubscribeEvents()
	{
		if (!_eventsSubscribed)
		{
			return;
		}

		RacingApi.RoundStarted -= OnRoundStarted;
		RacingApi.RoundEnded -= OnRoundEnded;
		RacingApi.PlayerSpawned -= OnPlayerSpawned;
		RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
		RacingApi.LevelLoaded -= OnLevelLoaded;
		RacingApi.Crashed -= OnCrashed;
		RacingApi.WheelBroken -= OnWheelBroken;
		PhotoModeApi.PhotoModeEntered -= OnPhotoModeEntered;

		_eventsSubscribed = false;
	}
}
