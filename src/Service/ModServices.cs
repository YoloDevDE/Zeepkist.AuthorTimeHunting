using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Screens;
using AuthorTimeHunting.UI.Views;

namespace AuthorTimeHunting.Service;

/// <summary>
///     The mod's composition root: session-scoped services, built once in Plugin.Awake.
///     Run-scoped state does not belong here.
/// </summary>
public class ModServices
{
	public ModServices()
	{
		Playlist = new PlaylistService(WorkshopDownloads);
		Toolbar = new AthToolbar(this);
		DebugToolbar = new AthDebugToolbar(this);
		Health = new HealthService(GraphQL);
		RunOverlay = new RunOverlay(Control, Medals);
		Results = new ResultsScreen(Medals);
		LevelSummary = new LevelSummaryOverlay(Medals);
		Loading = new LoadingOverlay(Thumbnail);
		Menu = new AthMenu(MenuBackground);
	}

	public AthToolbar Toolbar { get; }

	public AthDebugToolbar DebugToolbar { get; }

	public GamemodeRegistry Gamemodes { get; } = new();

	public LocalLevelCacheService LocalLevelCache { get; } = new();
	public GraphQLService GraphQL { get; } = new();

	public HealthService Health { get; }

	public WorkshopDownloadService WorkshopDownloads { get; } = new();

	public PlaylistService Playlist { get; }

	public GameStateObserver GameState { get; } = new();

	public TraceService Trace { get; } = new();

	public AthMenu Menu { get; }

	public StatusWindow Status { get; } = new();

	/// <summary>Not a drawer of its own: <see cref="RunOverlay" /> draws it.</summary>
	public ControlPanel Control { get; } = new();

	public RunOverlay RunOverlay { get; }

	public DebugPanel Debug { get; } = new();

	public LevelSummaryOverlay LevelSummary { get; }

	public RaceTimeDisplay RaceTime { get; } = new();

	public LeaderboardOverlay Leaderboard { get; } = new();

	public SplitsOverlay Splits { get; } = new();

	public ResultsScreen Results { get; }

	public WelcomeWindow Welcome { get; } = new();

	public HelpWindow Help { get; } = new();

	public LoadingOverlay Loading { get; }

	/// <summary>The mod's logo, shared by the loading screen and the run bar.</summary>
	public AthImage Thumbnail { get; } = new("AuthorTimeHunting.Thumbnail.png");

	/// <summary>What the menu puts behind itself instead of a flat black screen.</summary>
	public AthImage MenuBackground { get; } = new("AuthorTimeHunting.MenuBackground.jpg");

	/// <summary>The game's medals, copied once and shared by everything that draws one.</summary>
	public MedalArt Medals { get; } = new();

	public LobbySilence Silence { get; } = new();

	public PlayMenuButton PlayMenu { get; } = new();

	public MatchHistoryService History { get; } = new();

	public void PublishRun(AthController run)
	{
		RunHudView.Clear();

		RunOverlay.ActiveRun = run;
		Debug.ActiveRun = run;
		Status.ActiveRun = run;
		RaceTime.ActiveRun = run;
		Leaderboard.ActiveRun = run;
		Splits.ActiveRun = run;
		Loading.ActiveRun = run;
	}

	/// <summary>
	///     Closes the windows a player opened before a loading screen. The run's overlays stay,
	///     because nothing switches them back on.
	/// </summary>
	public void CloseMenus()
	{
		Menu.Visible = false;
		Status.Visible = false;
		Welcome.Visible = false;
		Help.Visible = false;
		Debug.Visible = false;
		Results.Close();
	}

	public RandomLevelService CreateRandomLevelService()
	{
		return new RandomLevelService(GraphQL, LocalLevelCache);
	}
}
