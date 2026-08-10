using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     Nothing here waits on a clock. A run used to sit through five silent seconds before
///     skipping to its first level, which was a guess at how long the playlist takes to reach
///     the server. Instead it waits for the level itself to be on disk, so the skip goes out
///     the moment it can - and the player, who has just watched a ten second countdown on the
///     welcome screen, gets the level they were counting down to.
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
