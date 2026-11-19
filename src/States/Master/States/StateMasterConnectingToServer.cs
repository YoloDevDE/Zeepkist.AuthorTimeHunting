using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine.SceneManagement;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Master.States;

/// <summary>
///     Gets the client onto the lobby server so the hunt can host its own lobby. Any existing
///     connection is dropped first, because only a fresh connect raises an event to wait for.
/// </summary>
public class StateMasterConnectingToServer : StateBase
{
	private const string _onlineLobbyScene = "Online Lobby";

	private const string _reason = "Author Time Hunting is opening its own lobby";

	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

	private readonly CancellationTokenSource _cts = new();
	private readonly IGamemode _gamemode;

	private bool _connected;
	private bool _left;

	public StateMasterConnectingToServer(AthMasterController controller, IGamemode gamemode) : base(controller)
	{
		_gamemode = gamemode;
	}

	private AthMasterController AthMaster => (AthMasterController)Controller;

	public override async void Enter()
	{
		AthRequests.StopRequested += Cancel;
		ZeepkistNetwork.ConnectedToMasterServer += OnConnected;

		AthMaster.Services.CloseMenus();
		AthMaster.Services.Loading.Show("Leaving the lobby");

		try
		{
			Finish(await Connect());
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception e)
		{
			Logger.LogError($"StateMasterConnectingToServer: {e.Message}\n{e.StackTrace}");
			Finish(false);
		}
	}

	public override void Exit()
	{
		_left = true;
		AthRequests.StopRequested -= Cancel;
		ZeepkistNetwork.ConnectedToMasterServer -= OnConnected;
		_cts.Cancel();
		_cts.Dispose();
	}

	/// <summary>
	///     Connects through the network manager from any scene; loading the online menu is the
	///     fallback when the manager does not exist yet.
	/// </summary>
	private static void StartConnecting()
	{
		if (NetworkClientManager.Instance != null)
		{
			NetworkClientManager.Instance.ConnectToMasterServer();
			return;
		}

		Logger.LogWarning("StateMasterConnectingToServer: No network manager, going through the online menu.");
		SceneManager.LoadScene(_onlineLobbyScene);
	}

	private async Task<bool> Connect()
	{
		ZeepkistNetwork.Disconnect(_reason);

		if (!await Wait.UntilAsync(() => !ZeepkistNetwork.IsConnected, _timeout, _cts.Token))
		{
			return false;
		}

		AthMaster.Services.Loading.Show("Connecting to the lobby server");
		StartConnecting();

		return await Wait.UntilAsync(() => _connected, _timeout, _cts.Token);
	}

	private void OnConnected()
	{
		_connected = true;
	}

	private void Finish(bool connected)
	{
		if (_left)
		{
			return;
		}

		if (!connected)
		{
			AthMaster.Services.Loading.Hide();
			FrogNotification.Error("Could not reach the lobby server, the hunt did not start");
			Controller.TransitionTo(new StateMasterOff(AthMaster));

			return;
		}

		Controller.TransitionTo(new StateMasterCreatingLobby(AthMaster, _gamemode));
	}

	private void Cancel()
	{
		AthMaster.Services.Loading.Hide();
		FrogNotification.Info("Start cancelled");
		Controller.TransitionTo(new StateMasterOff(AthMaster));
	}
}
