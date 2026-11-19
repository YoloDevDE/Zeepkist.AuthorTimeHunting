using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     Waits for the first level to be on disk, then skips to it the moment it can.
/// </summary>
public class StateAthWaitingForFirstLevel(AthStartingController phase, OnlineZeeplevel level)
	: AthState(phase, phase.Ath)
{
	private CancellationTokenSource _cts;

	public override async void Enter()
	{
		_cts = new CancellationTokenSource();

		try
		{
			await WaitForLevel(_cts.Token);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Logger.LogError($"Enter failed: {ex.Message}\nStack trace: {ex.StackTrace}");
			FrogNotification.Error("Something went wrong while starting the hunt");
			phase.Fail();
		}
	}

	public override void Exit()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private async Task WaitForLevel(CancellationToken stopped)
	{
		// The setup screen is still up and its watchdog only counts silence. Downloading a
		// level off the workshop is the longest wait in the whole setup and the only one that
		// arrives without a step of its own, so it says so before it starts.
		AthController.Services.Loading.Step("Downloading the level");

		await AthController.Services.WorkshopDownloads.WaitUntilReadyAsync(level);
		stopped.ThrowIfCancellationRequested();

		SkipToFirstLevel();
		Controller.InvokeFinish();
	}

	private void SkipToFirstLevel()
	{
		try
		{
			PlaylistService.SkipToFirstLevel();
		}
		catch (Exception ex)
		{
			Logger.LogError($"Failed to skip level: {ex.Message}");
			FrogNotification.Error("Error while skipping to the first level");
		}
	}
}
