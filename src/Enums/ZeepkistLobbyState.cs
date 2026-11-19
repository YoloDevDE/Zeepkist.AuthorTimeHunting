namespace AuthorTimeHunting.Enums;

/// <summary>
///     The lobby's game state, stored by the game as a bare int on ZeepkistLobby.GameState.
///     CAREFUL: 0 is also an uninitialized int, so never read <see cref="Racing" /> on its own
///     as "the race is running" - use GameStateObserver.IsRacing.
/// </summary>
public enum ZeepkistLobbyState
{
	Racing = 0,

	Ending = 1,

	Podium = 2
}
