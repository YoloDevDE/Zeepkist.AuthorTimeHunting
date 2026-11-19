namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     The rules of one run, fixed the moment it starts. Each gamemode hands over its own set.
/// </summary>
public class RunSettings
{
	public int DurationMs { get; set; }

	public int PenaltyTimeMs { get; set; }

	public bool RandomPlaylist { get; set; } = true;

	public int FreeSkips { get; set; } = 1;

	public bool RejectDuplicateLevels { get; set; } = true;
}
